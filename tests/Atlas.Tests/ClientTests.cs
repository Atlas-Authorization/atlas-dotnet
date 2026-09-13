using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    public class ClientTests
    {
        private static AtlasClient Client(MockHttpMessageHandler handler) =>
            new AtlasClient(new AtlasClientOptions
            {
                SecretKey = "sk_test_123",
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
            });

        // ---------------- Users ----------------

        [Fact]
        public async Task Users_List_ParsesCursorPage_AndSendsBearer()
        {
            var handler = MockHttpMessageHandler.Json(
                "{\"data\":[{\"object\":\"user\",\"id\":\"user_1\",\"first_name\":\"Ada\"}],\"has_more\":true,\"next_cursor\":\"user_1\"}");
            using var client = Client(handler);

            var page = await client.Users.ListAsync(new ListUsersParams { Limit = 10 });

            Assert.Single(page.Data);
            Assert.Equal("user_1", page.Data[0].Id);
            Assert.Equal("Ada", page.Data[0].FirstName);
            Assert.True(page.HasMore);
            Assert.Equal("user_1", page.NextCursor);

            var req = Assert.Single(handler.Requests);
            Assert.Equal("GET", req.Method);
            Assert.Equal("/v1/users", req.Path);
            Assert.Contains("limit=10", req.Query);
            Assert.Equal("Bearer sk_test_123", req.Authorization);
        }

        [Fact]
        public async Task Users_Get_EncodesId_AndParsesUser()
        {
            var handler = MockHttpMessageHandler.Json(
                "{\"object\":\"user\",\"id\":\"user_42\",\"mfa_enabled\":true,\"banned\":false}");
            using var client = Client(handler);

            var user = await client.Users.GetAsync("user_42");

            Assert.Equal("user_42", user.Id);
            Assert.True(user.MfaEnabled);
            Assert.Equal("/v1/users/user_42", handler.Requests[0].Path);
        }

        [Fact]
        public async Task Users_Create_SerializesSnakeCaseBody_AndPassesIdempotencyKey()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"user\",\"id\":\"user_new\"}");
            using var client = Client(handler);

            var user = await client.Users.CreateAsync(
                new CreateUserBody { EmailAddress = "ada@example.com", Password = "hunter2", EmailVerified = true },
                idempotencyKey: "idem-1");

            Assert.Equal("user_new", user.Id);
            var req = handler.Requests[0];
            Assert.Equal("POST", req.Method);
            Assert.Contains("\"email_address\":\"ada@example.com\"", req.Body);
            Assert.Contains("\"email_verified\":true", req.Body);
            Assert.Equal("idem-1", req.IdempotencyKey);
        }

        [Fact]
        public async Task Users_Lock_OmitsNullOptionalBodyField()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"user\",\"id\":\"user_1\",\"locked\":true}");
            using var client = Client(handler);

            await client.Users.LockAsync("user_1");

            // duration_in_seconds was null and must be dropped from the body.
            Assert.DoesNotContain("duration_in_seconds", handler.Requests[0].Body);
        }

        // ---------------- Sessions ----------------

        [Fact]
        public async Task Sessions_Create_ParsesMintedSession()
        {
            var handler = MockHttpMessageHandler.Json(
                "{\"object\":\"session\",\"id\":\"sess_1\",\"user_id\":\"user_1\",\"jwt\":\"ey.j.wt\",\"refresh_token\":\"rt_1\",\"expires_in\":60}");
            using var client = Client(handler);

            var minted = await client.Sessions.CreateAsync(new CreateSessionBody { UserId = "user_1" });

            Assert.Equal("sess_1", minted.Id);
            Assert.Equal("ey.j.wt", minted.Jwt);
            Assert.Equal("rt_1", minted.RefreshToken);
            Assert.Equal(60, minted.ExpiresIn);
            Assert.Equal("/v1/sessions", handler.Requests[0].Path);
        }

        [Fact]
        public async Task Sessions_List_RequiresUserIdQuery()
        {
            var handler = MockHttpMessageHandler.Json("{\"object\":\"list\",\"data\":[]}");
            using var client = Client(handler);

            await client.Sessions.ListAsync("user_7");

            Assert.Contains("user_id=user_7", handler.Requests[0].Query);
        }

        // ---------------- Organizations ----------------

        [Fact]
        public async Task Organizations_List_ParsesCursorPage()
        {
            var handler = MockHttpMessageHandler.Json(
                "{\"data\":[{\"object\":\"organization\",\"id\":\"org_1\",\"name\":\"Acme\",\"slug\":\"acme\"}],\"has_more\":false,\"next_cursor\":null}");
            using var client = Client(handler);

            var page = await client.Organizations.ListAsync();

            Assert.Single(page.Data);
            Assert.Equal("org_1", page.Data[0].Id);
            Assert.Equal("acme", page.Data[0].Slug);
        }

        [Fact]
        public async Task Organizations_Memberships_Add_PostsUserIdAndRole()
        {
            var handler = MockHttpMessageHandler.Json(
                "{\"object\":\"organization_membership\",\"organization_id\":\"org_1\",\"user_id\":\"user_1\",\"role\":\"admin\"}");
            using var client = Client(handler);

            var result = await client.Organizations.Memberships.AddAsync("org_1", "user_1", "admin");

            Assert.Equal("admin", result.Role);
            var req = handler.Requests[0];
            Assert.Equal("/v1/organizations/org_1/memberships", req.Path);
            Assert.Contains("\"user_id\":\"user_1\"", req.Body);
            Assert.Contains("\"role\":\"admin\"", req.Body);
        }

        // ---------------- Roles ----------------

        [Fact]
        public async Task Roles_SetPermissions_PutsPermissionArray()
        {
            var handler = MockHttpMessageHandler.Json(
                "{\"object\":\"role\",\"id\":\"role_1\",\"key\":\"editor\",\"permissions\":[\"posts:read\",\"posts:write\"],\"ignored\":[]}");
            using var client = Client(handler);

            var result = await client.Roles.SetPermissionsAsync("role_1", new[] { "posts:read", "posts:write" });

            Assert.Equal(2, result.Permissions.Count);
            var req = handler.Requests[0];
            Assert.Equal("PUT", req.Method);
            Assert.Equal("/v1/roles/role_1/permissions", req.Path);
            Assert.Contains("posts:write", req.Body);
        }

        [Fact]
        public async Task Roles_Delete_PassesReassignToQuery()
        {
            var handler = MockHttpMessageHandler.Json(
                "{\"object\":\"role\",\"id\":\"role_1\",\"deleted\":true,\"members_reassigned\":3}");
            using var client = Client(handler);

            var result = await client.Roles.DeleteAsync("role_1", reassignTo: "role_2");

            Assert.True(result.Deleted);
            Assert.Equal(3, result.MembersReassigned);
            Assert.Contains("reassign_to=role_2", handler.Requests[0].Query);
        }

        // ---------------- Error handling ----------------

        [Fact]
        public async Task Error_404_ThrowsNotFound_WithParsedCode()
        {
            var handler = MockHttpMessageHandler.Json(
                "{\"errors\":[{\"code\":\"NOT_FOUND\",\"message\":\"No such user\"}]}", HttpStatusCode.NotFound);
            using var client = Client(handler);

            var ex = await Assert.ThrowsAsync<AtlasNotFoundException>(() => client.Users.GetAsync("user_missing"));

            Assert.Equal(404, ex.Status);
            Assert.Equal("NOT_FOUND", ex.Code);
            Assert.True(ex.HasCode("NOT_FOUND"));
            Assert.Equal("No such user", ex.Message);
        }

        [Fact]
        public async Task Error_401_ThrowsAuthentication()
        {
            var handler = MockHttpMessageHandler.Json(
                "{\"errors\":[{\"code\":\"UNAUTHENTICATED\",\"message\":\"bad key\"}]}", HttpStatusCode.Unauthorized);
            using var client = Client(handler);

            await Assert.ThrowsAsync<AtlasAuthenticationException>(() => client.Users.ListAsync());
        }

        [Fact]
        public async Task Error_429_ExposesRetryAfterFromMeta()
        {
            var handler = MockHttpMessageHandler.Json(
                "{\"errors\":[{\"code\":\"RATE_LIMITED\",\"message\":\"slow down\",\"meta\":{\"retry_after\":12}}]}",
                (HttpStatusCode)429);
            using var client = Client(handler);

            var ex = await Assert.ThrowsAsync<AtlasRateLimitException>(() => client.Users.ListAsync());

            Assert.Equal(429, ex.Status);
            Assert.Equal(12d, ex.RetryAfter);
        }

        [Fact]
        public async Task Error_NonJsonBody_KeptAsMessage()
        {
            var handler = MockHttpMessageHandler.Json("upstream proxy exploded", HttpStatusCode.BadGateway);
            using var client = Client(handler);

            var ex = await Assert.ThrowsAsync<AtlasServerException>(() => client.Users.ListAsync());

            Assert.Equal(502, ex.Status);
            Assert.Contains("upstream proxy exploded", ex.Message);
        }
    }
}
