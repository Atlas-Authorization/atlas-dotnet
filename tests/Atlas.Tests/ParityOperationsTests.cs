using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>Request shaping for the operations added in the parity release: verb, exact URL (encoded path parameters, query), JSON body and idempotency key.</summary>
    public class ParityOperationsTests
    {
        private static async Task Row(string method, string pathAndQuery, string? body, bool idem, Func<AtlasClient, Task> call)
        {
            var h = MockHttpMessageHandler.Json("{}");
            using var c = new AtlasClient(new AtlasClientOptions
            {
                SecretKey = "sk_test_123",
                PlatformKey = "psk_test_456",
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(h),
            });
            await call(c);
            Assert.Single(h.Requests);
            var r = h.Requests[0];
            Assert.Equal(method, r.Method);
            Assert.Equal(pathAndQuery, r.Url.OriginalString.Substring("https://api.test.example".Length));
            if (body == null) Assert.True(string.IsNullOrEmpty(r.Body), $"{method} {pathAndQuery} should send no body");
            else Assert.Equal(body, r.Body);
            if (idem) Assert.Equal("idem-1", r.IdempotencyKey);
            else Assert.Null(r.IdempotencyKey);
        }

        [Fact]
        public async Task AppCatalog_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/app_catalog?limit=1&starting_after=q1", null, false, c => c.AppCatalog.PresetsAsync(limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/app_catalog/installed?limit=1&starting_after=q1", null, false, c => c.AppCatalog.InstalledAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/app_catalog/a%2Fb/install?dry_run=true", "{\"installed_id\":\"s\",\"name\":\"s\"}", true, c => c.AppCatalog.InstallAsync("a/b", body: new AppCatalogInstallBody { InstalledId = "s", Name = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("PATCH", "/v1/app_catalog/installed/a%2Fb?dry_run=true", "{\"name\":\"s\",\"allowed_email_domains\":[\"a\"]}", false, c => c.AppCatalog.UpdateAsync("a/b", body: new AppCatalogUpdateBody { Name = "s", AllowedEmailDomains = new List<string> { "a" } }, dryRun: true));
            await Row("DELETE", "/v1/app_catalog/installed/a%2Fb?dry_run=true", null, false, c => c.AppCatalog.UninstallAsync("a/b", dryRun: true));
        }

        [Fact]
        public async Task LdapServerClients_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/ldap_server_clients?limit=1&starting_after=q1", null, false, c => c.LdapServerClients.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/ldap_server_clients?dry_run=true", "{\"name\":\"s\",\"base_dn\":\"s\"}", true, c => c.LdapServerClients.CreateAsync(body: new LdapServerClientsCreateBody { Name = "s", BaseDn = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/ldap_server_clients/a%2Fb", null, false, c => c.LdapServerClients.GetAsync("a/b"));
            await Row("PATCH", "/v1/ldap_server_clients/a%2Fb?dry_run=true", "{\"name\":\"s\",\"base_dn\":\"s\"}", false, c => c.LdapServerClients.UpdateAsync("a/b", body: new LdapServerClientsUpdateBody { Name = "s", BaseDn = "s" }, dryRun: true));
            await Row("DELETE", "/v1/ldap_server_clients/a%2Fb?dry_run=true", null, false, c => c.LdapServerClients.DeleteAsync("a/b", dryRun: true));
            await Row("POST", "/v1/ldap_server_clients/a%2Fb/rotate_secret?dry_run=true", null, true, c => c.LdapServerClients.RotateSecretAsync("a/b", dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task Users_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/users/a%2Fb/history?limit=1&starting_after=q1&action=q1", null, false, c => c.Users.HistoryAsync("a/b", limit: 1, startingAfter: "q1", action: "q1"));
            await Row("GET", "/v1/users/a%2Fb/minor_status", null, false, c => c.Users.MinorStatusAsync("a/b"));
            await Row("POST", "/v1/users/a%2Fb/minor_status/assess?dry_run=true", null, true, c => c.Users.AssessMinorAsync("a/b", dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/users/a%2Fb/links?dry_run=true", "{\"type\":\"s\",\"email_address_id\":\"s\"}", true, c => c.Users.CreateLinkAsync("a/b", body: new UsersCreateLinkBody { Type = "s", EmailAddressId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/users/a%2Fb/device_keys?limit=1&starting_after=q1", null, false, c => c.Users.ListDeviceKeysAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("DELETE", "/v1/users/a%2Fb/device_keys/p2?dry_run=true", null, false, c => c.Users.DeleteDeviceKeyAsync("a/b", "p2", dryRun: true));
            await Row("POST", "/v1/users/a%2Fb/required_actions?dry_run=true", "{\"action\":\"s\",\"url\":\"s\"}", true, c => c.Users.RequiredActions.RequestAsync("a/b", body: new UsersRequiredActionsRequestBody { Action = "s", Url = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/users/a%2Fb/required_actions?limit=1&starting_after=q1", null, false, c => c.Users.RequiredActions.ListAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("DELETE", "/v1/users/a%2Fb/required_actions/p2?dry_run=true", null, false, c => c.Users.RequiredActions.ClearAsync("a/b", "p2", dryRun: true));
            await Row("POST", "/v1/required_actions/bulk?dry_run=true", "{\"action\":\"s\",\"url\":\"s\"}", true, c => c.Users.RequiredActions.BulkAsync(body: new UsersRequiredActionsBulkBody { Action = "s", Url = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/users/a%2Fb/notes?limit=1&starting_after=q1", null, false, c => c.Users.Notes.ListAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/users/a%2Fb/notes?dry_run=true", "{\"body\":\"s\",\"pinned\":true}", true, c => c.Users.Notes.CreateAsync("a/b", body: new UsersNotesCreateBody { Body = "s", Pinned = true }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("PATCH", "/v1/users/a%2Fb/notes/p2?dry_run=true", "{\"body\":\"s\",\"pinned\":true}", false, c => c.Users.Notes.UpdateAsync("a/b", "p2", body: new UsersNotesUpdateBody { Body = "s", Pinned = true }, dryRun: true));
            await Row("DELETE", "/v1/users/a%2Fb/notes/p2?dry_run=true", null, false, c => c.Users.Notes.DeleteAsync("a/b", "p2", dryRun: true));
            await Row("POST", "/v1/users/a%2Fb/guardians?dry_run=true", "{\"email\":\"s\",\"guardian_user_id\":\"s\"}", true, c => c.Users.Guardians.RequestAsync("a/b", body: new UsersGuardiansRequestBody { Email = "s", GuardianUserId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/users/a%2Fb/guardians/p2/approve?dry_run=true", "{\"note\":\"s\"}", true, c => c.Users.Guardians.ApproveAsync("a/b", "p2", body: new UsersGuardiansApproveBody { Note = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("DELETE", "/v1/users/a%2Fb/guardians/p2?dry_run=true", null, false, c => c.Users.Guardians.RevokeAsync("a/b", "p2", dryRun: true));
            await Row("POST", "/v1/users/search?dry_run=true", "{\"query\":\"s\",\"order_by\":\"s\"}", true, c => c.Users.Search.QueryAsync(body: new UsersSearchQueryBody { Query = "s", OrderBy = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/users/search/export?dry_run=true", "{\"query\":\"s\",\"order_by\":\"s\"}", true, c => c.Users.Search.ExportAsync(body: new UsersSearchExportBody { Query = "s", OrderBy = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/users/search/views?limit=1&starting_after=q1", null, false, c => c.Users.Search.Views.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/users/search/views?dry_run=true", "{\"name\":\"s\",\"order_by\":\"s\"}", true, c => c.Users.Search.Views.CreateAsync(body: new UsersSearchViewsCreateBody { Name = "s", OrderBy = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("PATCH", "/v1/users/search/views/a%2Fb?dry_run=true", "{\"name\":\"s\",\"order_by\":\"s\"}", false, c => c.Users.Search.Views.UpdateAsync("a/b", body: new UsersSearchViewsUpdateBody { Name = "s", OrderBy = "s" }, dryRun: true));
            await Row("DELETE", "/v1/users/search/views/a%2Fb?dry_run=true", null, false, c => c.Users.Search.Views.DeleteAsync("a/b", dryRun: true));
        }

        [Fact]
        public async Task Journeys_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/flows?limit=1&starting_after=q1", null, false, c => c.Journeys.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/flows?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", true, c => c.Journeys.CreateAsync(body: new JourneysCreateBody { Name = "s", Description = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/flows/a%2Fb", null, false, c => c.Journeys.GetAsync("a/b"));
            await Row("PATCH", "/v1/flows/a%2Fb?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", false, c => c.Journeys.UpdateAsync("a/b", body: new JourneysUpdateBody { Name = "s", Description = "s" }, dryRun: true));
            await Row("DELETE", "/v1/flows/a%2Fb?dry_run=true", null, false, c => c.Journeys.DeleteAsync("a/b", dryRun: true));
            await Row("GET", "/v1/flows/a%2Fb/versions?limit=1&starting_after=q1", null, false, c => c.Journeys.VersionsAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/flows/a%2Fb/activate?dry_run=true", "{\"version\":7}", true, c => c.Journeys.ActivateAsync("a/b", body: new JourneysActivateBody { Version = 7 }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/flows/a%2Fb/simulate?dry_run=true", "{\"user_id\":\"s\",\"method\":\"s\"}", true, c => c.Journeys.SimulateAsync("a/b", body: new JourneysSimulateBody { UserId = "s", Method = "s" }, dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task Groups_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/groups?limit=1&starting_after=q1", null, false, c => c.Groups.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/groups?dry_run=true", "{\"slug\":\"s\",\"name\":\"s\"}", true, c => c.Groups.CreateAsync(body: new GroupsCreateBody { Slug = "s", Name = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/groups/a%2Fb", null, false, c => c.Groups.GetAsync("a/b"));
            await Row("PATCH", "/v1/groups/a%2Fb?dry_run=true", "{\"slug\":\"s\",\"name\":\"s\"}", false, c => c.Groups.UpdateAsync("a/b", body: new GroupsUpdateBody { Slug = "s", Name = "s" }, dryRun: true));
            await Row("DELETE", "/v1/groups/a%2Fb?dry_run=true", null, false, c => c.Groups.DeleteAsync("a/b", dryRun: true));
            await Row("GET", "/v1/groups/a%2Fb/members?limit=1&starting_after=q1", null, false, c => c.Groups.ListMembersAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/groups/a%2Fb/members?dry_run=true", "{\"user_ids\":[\"a\"]}", true, c => c.Groups.AddMembersAsync("a/b", body: new GroupsAddMembersBody { UserIds = new List<string> { "a" } }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/groups/a%2Fb/members/remove?dry_run=true", "{\"user_ids\":[\"a\"]}", true, c => c.Groups.RemoveMembersAsync("a/b", body: new GroupsRemoveMembersBody { UserIds = new List<string> { "a" } }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/groups/a%2Fb/reapply?dry_run=true", null, true, c => c.Groups.ReapplyAsync("a/b", dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/users/a%2Fb/groups?limit=1&starting_after=q1", null, false, c => c.Groups.ForUserAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/group_rules?limit=1&starting_after=q1", null, false, c => c.Groups.Rules.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/group_rules?dry_run=true", "{\"name\":\"s\",\"enabled\":true}", true, c => c.Groups.Rules.CreateAsync(body: new GroupsRulesCreateBody { Name = "s", Enabled = true }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/group_rules/apply?dry_run=true", null, true, c => c.Groups.Rules.ApplyAllAsync(dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/group_rules/a%2Fb", null, false, c => c.Groups.Rules.GetAsync("a/b"));
            await Row("PATCH", "/v1/group_rules/a%2Fb?dry_run=true", "{\"name\":\"s\",\"enabled\":true}", false, c => c.Groups.Rules.UpdateAsync("a/b", body: new GroupsRulesUpdateBody { Name = "s", Enabled = true }, dryRun: true));
            await Row("DELETE", "/v1/group_rules/a%2Fb?dry_run=true", null, false, c => c.Groups.Rules.DeleteAsync("a/b", dryRun: true));
            await Row("POST", "/v1/group_rules/a%2Fb/apply?dry_run=true", null, true, c => c.Groups.Rules.ApplyAsync("a/b", dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task Migrations_Operations_HitTheirRoutes()
        {
            await Row("POST", "/v1/migrations/a%2Fb/exchange?dry_run=true", "{\"token\":\"s\",\"remember\":true}", true, c => c.Migrations.ExchangeAsync("a/b", body: new MigrationsExchangeBody { Token = "s", Remember = true }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/migrations/sources?limit=1&starting_after=q1", null, false, c => c.Migrations.SourcesAsync(limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/migrations/a%2Fb/bridge", null, false, c => c.Migrations.GetBridgeAsync("a/b"));
            await Row("PUT", "/v1/migrations/a%2Fb/bridge?dry_run=true", "{\"enabled\":true,\"mode\":\"s\"}", false, c => c.Migrations.SetBridgeAsync("a/b", body: new MigrationsSetBridgeBody { Enabled = true, Mode = "s" }, dryRun: true));
            await Row("DELETE", "/v1/migrations/a%2Fb/bridge?dry_run=true", null, false, c => c.Migrations.DeleteBridgeAsync("a/b", dryRun: true));
            await Row("GET", "/v1/migrations/a%2Fb/imports/public_key", null, false, c => c.Migrations.Imports.GetPublicKeyAsync("a/b"));
            await Row("POST", "/v1/migrations/a%2Fb/imports/public_key/rotate?dry_run=true", null, true, c => c.Migrations.Imports.RotatePublicKeyAsync("a/b", dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/migrations/a%2Fb/imports?dry_run=true", "{}", true, c => c.Migrations.Imports.CreateAsync("a/b", body: new MigrationsImportsCreateBody(), dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/migrations/a%2Fb/imports?limit=1&starting_after=q1", null, false, c => c.Migrations.Imports.ListAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/migrations/a%2Fb/imports/p2", null, false, c => c.Migrations.Imports.GetAsync("a/b", "p2"));
            await Row("DELETE", "/v1/migrations/a%2Fb/imports/p2?dry_run=true", null, false, c => c.Migrations.Imports.DeleteAsync("a/b", "p2", dryRun: true));
            await Row("PUT", "/v1/migrations/a%2Fb/imports/p2/parts/p3?dry_run=true", "{\"data\":\"s\"}", false, c => c.Migrations.Imports.PutPartAsync("a/b", "p2", "p3", body: new MigrationsImportsPutPartBody { Data = "s" }, dryRun: true));
            await Row("POST", "/v1/migrations/a%2Fb/imports/p2/finalize?dry_run=true", null, true, c => c.Migrations.Imports.FinalizeAsync("a/b", "p2", dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/migrations/a%2Fb/imports/p2/dry_run?dry_run=true", "{}", true, c => c.Migrations.Imports.DryRunAsync("a/b", "p2", body: new MigrationsImportsDryRunBody(), dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/migrations/a%2Fb/imports/p2/apply?dry_run=true", "{\"confirm_conflicts\":true}", true, c => c.Migrations.Imports.ApplyAsync("a/b", "p2", body: new MigrationsImportsApplyBody { ConfirmConflicts = true }, dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task ManagedGrants_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/grant_management/grants?user_id=q1&client_id=q1&status=q1&limit=1&starting_after=q1", null, false, c => c.ManagedGrants.ListAsync(userId: "q1", clientId: "q1", status: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/grant_management/grants/a%2Fb", null, false, c => c.ManagedGrants.GetAsync("a/b"));
            await Row("PATCH", "/v1/grant_management/grants/a%2Fb?dry_run=true", "{\"scopes\":[\"a\"]}", false, c => c.ManagedGrants.NarrowAsync("a/b", body: new ManagedGrantsNarrowBody { Scopes = new List<string> { "a" } }, dryRun: true));
            await Row("DELETE", "/v1/grant_management/grants/a%2Fb?dry_run=true", null, false, c => c.ManagedGrants.RevokeAsync("a/b", dryRun: true));
        }

        [Fact]
        public async Task LeastPrivilege_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/least_privilege/report?days=1&subject_type=q1&subject_id=q1&only_unused=true&limit=1&format=q1&starting_after=q1", null, false, c => c.LeastPrivilege.ReportAsync(days: 1, subjectType: "q1", subjectId: "q1", onlyUnused: true, limit: 1, format: "q1", startingAfter: "q1"));
            await Row("GET", "/v1/least_privilege/report/a%2Fb/p2?days=1", null, false, c => c.LeastPrivilege.GetAsync("a/b", "p2", days: 1));
            await Row("POST", "/v1/least_privilege/apply?dry_run=true", "{\"subject_type\":\"s\",\"subject_id\":\"s\"}", true, c => c.LeastPrivilege.ApplyAsync(body: new LeastPrivilegeApplyBody { SubjectType = "s", SubjectId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task DeliveryHealth_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/delivery_health/a%2Fb/p2", null, false, c => c.DeliveryHealth.GetAsync("a/b", "p2"));
        }

        [Fact]
        public async Task Flags_Operations_HitTheirRoutes()
        {
            await Row("PUT", "/v1/organizations/a%2Fb/entitlements?dry_run=true", "{\"plan\":\"s\",\"features\":[\"a\"]}", false, c => c.Flags.AssignOrganizationEntitlementsAsync("a/b", body: new FlagsAssignOrganizationEntitlementsBody { Plan = "s", Features = new List<string> { "a" } }, dryRun: true));
            await Row("POST", "/v1/feature_flags?dry_run=true", "{\"key\":\"s\",\"type\":\"s\"}", true, c => c.Flags.CreateAsync(body: new FlagsCreateBody { Key = "s", Type = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/feature_flags?starting_after=q1&limit=1", null, false, c => c.Flags.ListAsync(startingAfter: "q1", limit: 1));
            await Row("GET", "/v1/feature_flags/a%2Fb", null, false, c => c.Flags.RetrieveAsync("a/b"));
            await Row("PATCH", "/v1/feature_flags/a%2Fb?dry_run=true", "{\"description\":\"s\",\"enabled\":true}", false, c => c.Flags.UpdateAsync("a/b", body: new FlagsUpdateBody { Description = "s", Enabled = true }, dryRun: true));
            await Row("DELETE", "/v1/feature_flags/a%2Fb?dry_run=true", null, false, c => c.Flags.DeleteAsync("a/b", dryRun: true));
            await Row("POST", "/v1/feature_flags/evaluate?dry_run=true", "{\"user_id\":\"s\",\"org_id\":\"s\"}", true, c => c.Flags.EvaluateAsync(body: new FlagsEvaluateBody { UserId = "s", OrgId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("PUT", "/v1/users/a%2Fb/entitlements?dry_run=true", "{\"plan\":\"s\",\"features\":[\"a\"]}", false, c => c.Flags.AssignUserEntitlementsAsync("a/b", body: new FlagsAssignUserEntitlementsBody { Plan = "s", Features = new List<string> { "a" } }, dryRun: true));
        }

        [Fact]
        public async Task MfaFactors_Operations_HitTheirRoutes()
        {
            await Row("PUT", "/v1/instance/yubico_secret?dry_run=true", "{\"secret\":\"s\"}", false, c => c.MfaFactors.SetYubicoSecretAsync(body: new MfaFactorsSetYubicoSecretBody { Secret = "s" }, dryRun: true));
            await Row("DELETE", "/v1/instance/yubico_secret?dry_run=true", null, false, c => c.MfaFactors.RemoveYubicoSecretAsync(dryRun: true));
            await Row("PUT", "/v1/instance/duo_secret?dry_run=true", "{\"secret\":\"s\"}", false, c => c.MfaFactors.SetDuoSecretAsync(body: new MfaFactorsSetDuoSecretBody { Secret = "s" }, dryRun: true));
            await Row("DELETE", "/v1/instance/duo_secret?dry_run=true", null, false, c => c.MfaFactors.RemoveDuoSecretAsync(dryRun: true));
            await Row("PUT", "/v1/instance/smart_card_secret?dry_run=true", "{\"secret\":\"s\"}", false, c => c.MfaFactors.SetSmartCardSecretAsync(body: new MfaFactorsSetSmartCardSecretBody { Secret = "s" }, dryRun: true));
            await Row("DELETE", "/v1/instance/smart_card_secret?dry_run=true", null, false, c => c.MfaFactors.RemoveSmartCardSecretAsync(dryRun: true));
            await Row("POST", "/v1/instance/duo_health_check?dry_run=true", null, true, c => c.MfaFactors.DuoHealthCheckAsync(dryRun: true, idempotencyKey: "idem-1"));
            await Row("DELETE", "/v1/users/a%2Fb/security_questions?dry_run=true", null, false, c => c.MfaFactors.ResetSecurityQuestionsAsync("a/b", dryRun: true));
            await Row("GET", "/v1/yubikeys?limit=1&starting_after=q1", null, false, c => c.MfaFactors.Yubikeys.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/yubikeys?dry_run=true", "{\"public_id\":\"s\",\"aes_key\":\"s\"}", true, c => c.MfaFactors.Yubikeys.RegisterAsync(body: new MfaFactorsYubikeysRegisterBody { PublicId = "s", AesKey = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("DELETE", "/v1/yubikeys/a%2Fb?dry_run=true", null, false, c => c.MfaFactors.Yubikeys.RemoveAsync("a/b", dryRun: true));
            await Row("GET", "/v1/users/a%2Fb/smartcard_mappings?limit=1&starting_after=q1", null, false, c => c.MfaFactors.SmartCardMappings.ListAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/users/a%2Fb/smartcard_mappings?dry_run=true", "{\"kind\":\"s\",\"value\":\"s\"}", true, c => c.MfaFactors.SmartCardMappings.AddAsync("a/b", body: new MfaFactorsSmartCardMappingsAddBody { Kind = "s", Value = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("DELETE", "/v1/users/a%2Fb/smartcard_mappings/p2?dry_run=true", null, false, c => c.MfaFactors.SmartCardMappings.RemoveAsync("a/b", "p2", dryRun: true));
        }

        [Fact]
        public async Task Analytics_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/analytics/funnel?kind=q1&from=v1&to=v1&days=1&granularity=q1&organization_id=q1&format=q1&limit=1&starting_after=q1", null, false, c => c.Analytics.FunnelAsync(kind: "q1", from: "v1", to: "v1", days: 1, granularity: "q1", organizationId: "q1", format: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/analytics/journey_funnel?journey_id=q1&from=v1&to=v1&days=1&granularity=q1&organization_id=q1&format=q1&limit=1&starting_after=q1", null, false, c => c.Analytics.JourneyFunnelAsync(journeyId: "q1", from: "v1", to: "v1", days: 1, granularity: "q1", organizationId: "q1", format: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/analytics/failures?from=v1&to=v1&days=1&granularity=q1&organization_id=q1&format=q1&limit=1&starting_after=q1", null, false, c => c.Analytics.FailuresAsync(from: "v1", to: "v1", days: 1, granularity: "q1", organizationId: "q1", format: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/analytics/breakdown?dimension=q1&from=v1&to=v1&days=1&granularity=q1&organization_id=q1&format=q1&limit=1&starting_after=q1", null, false, c => c.Analytics.BreakdownAsync(dimension: "q1", from: "v1", to: "v1", days: 1, granularity: "q1", organizationId: "q1", format: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/analytics/active_users?from=v1&to=v1&days=1&granularity=q1&organization_id=q1&format=q1&limit=1&starting_after=q1", null, false, c => c.Analytics.ActiveUsersAsync(from: "v1", to: "v1", days: 1, granularity: "q1", organizationId: "q1", format: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/analytics/passkey_adoption?from=v1&to=v1&days=1&granularity=q1&organization_id=q1&format=q1&limit=1&starting_after=q1", null, false, c => c.Analytics.PasskeyAdoptionAsync(from: "v1", to: "v1", days: 1, granularity: "q1", organizationId: "q1", format: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/analytics/api_usage?subject_type=q1&subject_id=q1&window_minutes=1&limit=1&starting_after=q1", null, false, c => c.Analytics.ApiUsageAsync(subjectType: "q1", subjectId: "q1", windowMinutes: 1, limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/analytics/reports/inactive_users?inactive_days=1&limit=1&starting_after=q1&from=v1&to=v1&days=1&granularity=q1&organization_id=q1&format=q1", null, false, c => c.Analytics.Reports.InactiveUsersAsync(inactiveDays: 1, limit: 1, startingAfter: "q1", from: "v1", to: "v1", days: 1, granularity: "q1", organizationId: "q1", format: "q1"));
            await Row("GET", "/v1/analytics/reports/mfa_coverage?from=v1&to=v1&days=1&granularity=q1&organization_id=q1&format=q1&limit=1&starting_after=q1", null, false, c => c.Analytics.Reports.MfaCoverageAsync(from: "v1", to: "v1", days: 1, granularity: "q1", organizationId: "q1", format: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/analytics/reports/access_by_app?from=v1&to=v1&days=1&granularity=q1&organization_id=q1&format=q1&limit=1&starting_after=q1", null, false, c => c.Analytics.Reports.AccessByAppAsync(from: "v1", to: "v1", days: 1, granularity: "q1", organizationId: "q1", format: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/analytics/alert_rules?limit=1&starting_after=q1", null, false, c => c.Analytics.AlertRules.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/analytics/alert_rules?dry_run=true", "{\"name\":\"s\",\"metric\":\"s\"}", true, c => c.Analytics.AlertRules.CreateAsync(body: new AnalyticsAlertRulesCreateBody { Name = "s", Metric = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/analytics/alert_rules/a%2Fb", null, false, c => c.Analytics.AlertRules.GetAsync("a/b"));
            await Row("PATCH", "/v1/analytics/alert_rules/a%2Fb?dry_run=true", "{\"name\":\"s\",\"metric\":\"s\"}", false, c => c.Analytics.AlertRules.UpdateAsync("a/b", body: new AnalyticsAlertRulesUpdateBody { Name = "s", Metric = "s" }, dryRun: true));
            await Row("DELETE", "/v1/analytics/alert_rules/a%2Fb?dry_run=true", null, false, c => c.Analytics.AlertRules.DeleteAsync("a/b", dryRun: true));
            await Row("POST", "/v1/analytics/alert_rules/a%2Fb/test?dry_run=true", "{\"notify\":true}", true, c => c.Analytics.AlertRules.TestAsync("a/b", body: new AnalyticsAlertRulesTestBody { Notify = true }, dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task Marketing_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/marketing_settings", null, false, c => c.Marketing.GetSettingsAsync());
            await Row("PUT", "/v1/marketing_settings?dry_run=true", "{}", false, c => c.Marketing.UpdateSettingsAsync(body: new MarketingUpdateSettingsBody(), dryRun: true));
            await Row("GET", "/v1/leads?limit=1&starting_after=q1&form=q1&format=q1", null, false, c => c.Marketing.ListLeadsAsync(limit: 1, startingAfter: "q1", form: "q1", format: "q1"));
            await Row("DELETE", "/v1/leads/a%2Fb?dry_run=true", null, false, c => c.Marketing.DeleteLeadAsync("a/b", dryRun: true));
        }

        [Fact]
        public async Task AuditEventSchemas_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/audit_event_schemas?limit=1&starting_after=q1", null, false, c => c.AuditEventSchemas.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/audit_event_schemas?dry_run=true", "{\"action\":\"s\",\"version\":7}", true, c => c.AuditEventSchemas.RegisterAsync(body: new AuditEventSchemasRegisterBody { Action = "s", Version = 7 }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/audit_event_schemas/a%2Fb", null, false, c => c.AuditEventSchemas.GetAsync("a/b"));
            await Row("PATCH", "/v1/audit_event_schemas/a%2Fb?dry_run=true", "{\"description\":\"s\",\"status\":\"s\"}", false, c => c.AuditEventSchemas.UpdateAsync("a/b", body: new AuditEventSchemasUpdateBody { Description = "s", Status = "s" }, dryRun: true));
            await Row("DELETE", "/v1/audit_event_schemas/a%2Fb?dry_run=true", null, false, c => c.AuditEventSchemas.DeleteAsync("a/b", dryRun: true));
            await Row("POST", "/v1/audit_events/a%2Fb/redact?dry_run=true", "{\"fields\":[\"a\"],\"reason\":\"s\"}", true, c => c.AuditEventSchemas.RedactAsync("a/b", body: new AuditEventSchemasRedactBody { Fields = new List<string> { "a" }, Reason = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/audit_events/a%2Fb/redactions?limit=1&starting_after=q1", null, false, c => c.AuditEventSchemas.RedactionsAsync("a/b", limit: 1, startingAfter: "q1"));
        }

        [Fact]
        public async Task DirectoryPullSources_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/directory_pull_sources?organization_id=q1&limit=1&starting_after=q1", null, false, c => c.DirectoryPullSources.ListAsync(organizationId: "q1", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/directory_pull_sources?dry_run=true", "{\"organization_id\":\"s\",\"name\":\"s\"}", true, c => c.DirectoryPullSources.CreateAsync(body: new DirectoryPullSourcesCreateBody { OrganizationId = "s", Name = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/directory_pull_sources/a%2Fb", null, false, c => c.DirectoryPullSources.GetAsync("a/b"));
            await Row("PATCH", "/v1/directory_pull_sources/a%2Fb?dry_run=true", "{\"organization_id\":\"s\",\"name\":\"s\"}", false, c => c.DirectoryPullSources.UpdateAsync("a/b", body: new DirectoryPullSourcesUpdateBody { OrganizationId = "s", Name = "s" }, dryRun: true));
            await Row("DELETE", "/v1/directory_pull_sources/a%2Fb?dry_run=true", null, false, c => c.DirectoryPullSources.DeleteAsync("a/b", dryRun: true));
            await Row("PUT", "/v1/directory_pull_sources/a%2Fb/csv?dry_run=true", "{\"csv\":\"s\"}", false, c => c.DirectoryPullSources.SetCsvAsync("a/b", body: new DirectoryPullSourcesSetCsvBody { Csv = "s" }, dryRun: true));
            await Row("POST", "/v1/directory_pull_sources/a%2Fb/sync_runs?dry_run=true", null, true, c => c.DirectoryPullSources.SyncRuns.CreateAsync("a/b", dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/directory_pull_sources/a%2Fb/sync_runs?limit=1&starting_after=q1", null, false, c => c.DirectoryPullSources.SyncRuns.ListAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/directory_pull_sources/a%2Fb/sync_runs/p2", null, false, c => c.DirectoryPullSources.SyncRuns.GetAsync("a/b", "p2"));
        }

        [Fact]
        public async Task OrgLdapConnections_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/org_ldap_connections/a%2Fb/sync", null, false, c => c.OrgLdapConnections.GetSyncAsync("a/b"));
            await Row("PUT", "/v1/org_ldap_connections/a%2Fb/sync?dry_run=true", "{\"enabled\":true,\"idAttribute\":\"s\"}", false, c => c.OrgLdapConnections.SetSyncAsync("a/b", body: new OrgLdapConnectionsSetSyncBody { Enabled = true, IdAttribute = "s" }, dryRun: true));
            await Row("GET", "/v1/org_ldap_connections/a%2Fb/relay", null, false, c => c.OrgLdapConnections.GetRelayAsync("a/b"));
            await Row("PUT", "/v1/org_ldap_connections/a%2Fb/relay?dry_run=true", "{\"agent_id\":\"s\"}", false, c => c.OrgLdapConnections.SetRelayAsync("a/b", body: new OrgLdapConnectionsSetRelayBody { AgentId = "s" }, dryRun: true));
            await Row("POST", "/v1/org_ldap_connections/a%2Fb/sync_runs?dry_run=true", "{\"kind\":\"s\"}", true, c => c.OrgLdapConnections.SyncRuns.CreateAsync("a/b", body: new OrgLdapConnectionsSyncRunsCreateBody { Kind = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/org_ldap_connections/a%2Fb/sync_runs?limit=1&starting_after=q1", null, false, c => c.OrgLdapConnections.SyncRuns.ListAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/org_ldap_connections/a%2Fb/sync_runs/p2", null, false, c => c.OrgLdapConnections.SyncRuns.GetAsync("a/b", "p2"));
        }

        [Fact]
        public async Task RelayAgents_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/relay_agents?limit=1&starting_after=q1", null, false, c => c.RelayAgents.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/relay_agents?dry_run=true", "{\"name\":\"s\",\"organization_id\":\"s\"}", true, c => c.RelayAgents.CreateAsync(body: new RelayAgentsCreateBody { Name = "s", OrganizationId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/relay_agents/a%2Fb", null, false, c => c.RelayAgents.GetAsync("a/b"));
            await Row("PATCH", "/v1/relay_agents/a%2Fb?dry_run=true", "{\"name\":\"s\",\"organization_id\":\"s\"}", false, c => c.RelayAgents.UpdateAsync("a/b", body: new RelayAgentsUpdateBody { Name = "s", OrganizationId = "s" }, dryRun: true));
            await Row("DELETE", "/v1/relay_agents/a%2Fb?dry_run=true", null, false, c => c.RelayAgents.DeleteAsync("a/b", dryRun: true));
            await Row("POST", "/v1/relay_agents/a%2Fb/rotate_secret?dry_run=true", null, true, c => c.RelayAgents.RotateSecretAsync("a/b", dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task OAuthClients_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/oauth_clients/a%2Fb/exchange_policy", null, false, c => c.OAuthClients.GetExchangePolicyAsync("a/b"));
            await Row("PUT", "/v1/oauth_clients/a%2Fb/exchange_policy?dry_run=true", "{\"allowed_audiences\":[\"a\"],\"allowed_scopes\":[\"a\"]}", false, c => c.OAuthClients.SetExchangePolicyAsync("a/b", body: new OAuthClientsSetExchangePolicyBody { AllowedAudiences = new List<string> { "a" }, AllowedScopes = new List<string> { "a" } }, dryRun: true));
            await Row("DELETE", "/v1/oauth_clients/a%2Fb/exchange_policy?dry_run=true", null, false, c => c.OAuthClients.DeleteExchangePolicyAsync("a/b", dryRun: true));
            await Row("PUT", "/v1/oauth_clients/a%2Fb/profile?dry_run=true", "{\"profile\":\"s\"}", false, c => c.OAuthClients.SetProfileAsync("a/b", body: new OAuthClientsSetProfileBody { Profile = "s" }, dryRun: true));
            await Row("PUT", "/v1/oauth_clients/a%2Fb/delegated_ui?dry_run=true", "{\"login_url\":\"s\",\"consent_url\":\"s\"}", false, c => c.OAuthClients.SetDelegatedUiAsync("a/b", body: new OAuthClientsSetDelegatedUiBody { LoginUrl = "s", ConsentUrl = "s" }, dryRun: true));
            await Row("GET", "/v1/oauth_clients/a%2Fb/app_settings", null, false, c => c.OAuthClients.AppSettings.GetAsync("a/b"));
            await Row("PATCH", "/v1/oauth_clients/a%2Fb/app_settings?dry_run=true", "{\"allowed_sign_in_methods\":[\"a\"],\"claims_allowlist\":[\"a\"]}", false, c => c.OAuthClients.AppSettings.UpdateAsync("a/b", body: new OAuthClientsAppSettingsUpdateBody { AllowedSignInMethods = new List<string> { "a" }, ClaimsAllowlist = new List<string> { "a" } }, dryRun: true));
            await Row("GET", "/v1/workload_identity/presets?limit=1&starting_after=q1", null, false, c => c.OAuthClients.WorkloadIdentity.PresetsAsync(limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/workload_identity/cloud_trust?provider=q1&audience=q1&subject=q1&tenant_id=q1", null, false, c => c.OAuthClients.WorkloadIdentity.CloudTrustAsync(provider: "q1", audience: "q1", subject: "q1", tenantId: "q1"));
            await Row("GET", "/v1/oauth_clients/a%2Fb/trusted_issuers?limit=1&starting_after=q1", null, false, c => c.OAuthClients.TrustedIssuers.ListAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/oauth_clients/a%2Fb/trusted_issuers?dry_run=true", "{\"issuer\":\"s\",\"jwks_uri\":\"s\"}", true, c => c.OAuthClients.TrustedIssuers.CreateAsync("a/b", body: new OAuthClientsTrustedIssuersCreateBody { Issuer = "s", JwksUri = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("DELETE", "/v1/oauth_clients/a%2Fb/trusted_issuers/p2?dry_run=true", null, false, c => c.OAuthClients.TrustedIssuers.DeleteAsync("a/b", "p2", dryRun: true));
            await Row("GET", "/v1/oauth_login_requests?login_challenge=q1", null, false, c => c.OAuthClients.LoginRequests.GetAsync(loginChallenge: "q1"));
            await Row("PUT", "/v1/oauth_login_requests/accept?dry_run=true&login_challenge=q1", "{\"subject\":\"s\",\"remember\":true}", false, c => c.OAuthClients.LoginRequests.AcceptAsync(loginChallenge: "q1", body: new OAuthClientsLoginRequestsAcceptBody { Subject = "s", Remember = true }, dryRun: true));
            await Row("PUT", "/v1/oauth_login_requests/reject?dry_run=true&login_challenge=q1", "{\"error\":\"s\",\"error_description\":\"s\"}", false, c => c.OAuthClients.LoginRequests.RejectAsync(loginChallenge: "q1", body: new OAuthClientsLoginRequestsRejectBody { Error = "s", ErrorDescription = "s" }, dryRun: true));
            await Row("GET", "/v1/oauth_consent_requests?consent_challenge=q1", null, false, c => c.OAuthClients.ConsentRequests.GetAsync(consentChallenge: "q1"));
            await Row("PUT", "/v1/oauth_consent_requests/accept?dry_run=true&consent_challenge=q1", "{\"grant_scope\":[\"a\"],\"remember\":true}", false, c => c.OAuthClients.ConsentRequests.AcceptAsync(consentChallenge: "q1", body: new OAuthClientsConsentRequestsAcceptBody { GrantScope = new List<string> { "a" }, Remember = true }, dryRun: true));
            await Row("PUT", "/v1/oauth_consent_requests/reject?dry_run=true&consent_challenge=q1", "{\"error\":\"s\",\"error_description\":\"s\"}", false, c => c.OAuthClients.ConsentRequests.RejectAsync(consentChallenge: "q1", body: new OAuthClientsConsentRequestsRejectBody { Error = "s", ErrorDescription = "s" }, dryRun: true));
        }

        [Fact]
        public async Task Organizations_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/organizations/a%2Fb/connected_apps?limit=1&starting_after=q1", null, false, c => c.Organizations.ListConnectedAppsAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("DELETE", "/v1/organizations/a%2Fb/connected_apps/p2?dry_run=true", null, false, c => c.Organizations.RevokeConnectedAppAsync("a/b", "p2", dryRun: true));
            await Row("GET", "/v1/organizations/a%2Fb/connections/p2/secret?version=q1", null, false, c => c.Organizations.GetConnectionSecretAsync("a/b", "p2", version: "q1"));
            await Row("GET", "/v1/organizations/a%2Fb/webhook_endpoints?limit=1&starting_after=q1", null, false, c => c.Organizations.WebhookEndpoints.ListAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/organizations/a%2Fb/webhook_endpoints?dry_run=true", "{\"url\":\"s\",\"enabled_events\":[\"a\"]}", true, c => c.Organizations.WebhookEndpoints.CreateAsync("a/b", body: new OrganizationsWebhookEndpointsCreateBody { Url = "s", EnabledEvents = new List<string> { "a" } }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/organizations/a%2Fb/webhook_endpoints/p2", null, false, c => c.Organizations.WebhookEndpoints.GetAsync("a/b", "p2"));
            await Row("PATCH", "/v1/organizations/a%2Fb/webhook_endpoints/p2?dry_run=true", "{\"url\":\"s\",\"enabled_events\":[\"a\"]}", false, c => c.Organizations.WebhookEndpoints.UpdateAsync("a/b", "p2", body: new OrganizationsWebhookEndpointsUpdateBody { Url = "s", EnabledEvents = new List<string> { "a" } }, dryRun: true));
            await Row("DELETE", "/v1/organizations/a%2Fb/webhook_endpoints/p2?dry_run=true", null, false, c => c.Organizations.WebhookEndpoints.DeleteAsync("a/b", "p2", dryRun: true));
            await Row("POST", "/v1/organizations/a%2Fb/webhook_endpoints/p2/rotate_secret?dry_run=true", null, true, c => c.Organizations.WebhookEndpoints.RotateSecretAsync("a/b", "p2", dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/organizations/a%2Fb/webhook_endpoints/p2/deliveries?limit=1&starting_after=q1&status=q1&event_type=q1&since=q1&until=q1&trace=q1", null, false, c => c.Organizations.WebhookEndpoints.DeliveriesAsync("a/b", "p2", limit: 1, startingAfter: "q1", status: "q1", eventType: "q1", since: "q1", until: "q1", trace: "q1"));
        }

        [Fact]
        public async Task OAuthClientProfiles_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/oauth_client_profiles?limit=1&starting_after=q1", null, false, c => c.OAuthClientProfiles.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("PUT", "/v1/oauth_client_profiles/a%2Fb?dry_run=true", "{\"description\":\"s\",\"require_pkce\":true}", false, c => c.OAuthClientProfiles.SetAsync("a/b", body: new OAuthClientProfilesSetBody { Description = "s", RequirePkce = true }, dryRun: true));
            await Row("DELETE", "/v1/oauth_client_profiles/a%2Fb?dry_run=true", null, false, c => c.OAuthClientProfiles.DeleteAsync("a/b", dryRun: true));
        }

        [Fact]
        public async Task PasskeyApi_Operations_HitTheirRoutes()
        {
            await Row("POST", "/v1/passkey_api/registration/init?dry_run=true", "{\"user_id\":\"s\",\"username\":\"s\"}", true, c => c.PasskeyApi.RegistrationInitAsync(body: new PasskeyApiRegistrationInitBody { UserId = "s", Username = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/passkey_api/registration/finalize?dry_run=true", "{\"user_id\":\"s\",\"challenge\":\"s\"}", true, c => c.PasskeyApi.RegistrationFinalizeAsync(body: new PasskeyApiRegistrationFinalizeBody { UserId = "s", Challenge = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/passkey_api/login/init?dry_run=true", "{\"user_id\":\"s\"}", true, c => c.PasskeyApi.LoginInitAsync(body: new PasskeyApiLoginInitBody { UserId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/passkey_api/login/finalize?dry_run=true", "{\"handle\":\"s\",\"challenge\":\"s\"}", true, c => c.PasskeyApi.LoginFinalizeAsync(body: new PasskeyApiLoginFinalizeBody { Handle = "s", Challenge = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/passkey_api/users/a%2Fb/passkeys?limit=1&starting_after=q1", null, false, c => c.PasskeyApi.ListAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("DELETE", "/v1/passkey_api/users/a%2Fb/passkeys/p2?dry_run=true", null, false, c => c.PasskeyApi.DeleteAsync("a/b", "p2", dryRun: true));
            await Row("GET", "/v1/passkey_api/usage", null, false, c => c.PasskeyApi.UsageAsync());
        }

        [Fact]
        public async Task Vc_Operations_HitTheirRoutes()
        {
            await Row("POST", "/v1/vc/offers?dry_run=true", "{\"credential_type_id\":\"s\",\"user_id\":\"s\"}", true, c => c.Vc.CreateOfferAsync(body: new VcCreateOfferBody { CredentialTypeId = "s", UserId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/vc/credential_types?limit=1&starting_after=q1", null, false, c => c.Vc.CredentialTypes.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/vc/credential_types?dry_run=true", "{\"vct\":\"s\",\"format\":\"s\"}", true, c => c.Vc.CredentialTypes.CreateAsync(body: new VcCredentialTypesCreateBody { Vct = "s", Format = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/vc/credential_types/a%2Fb", null, false, c => c.Vc.CredentialTypes.GetAsync("a/b"));
            await Row("DELETE", "/v1/vc/credential_types/a%2Fb?dry_run=true", null, false, c => c.Vc.CredentialTypes.DeleteAsync("a/b", dryRun: true));
            await Row("GET", "/v1/vc/credentials?user_id=q1&limit=1&starting_after=q1", null, false, c => c.Vc.Credentials.ListAsync(userId: "q1", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/vc/credentials/a%2Fb/revoke?dry_run=true", "{}", true, c => c.Vc.Credentials.RevokeAsync("a/b", body: new VcCredentialsRevokeBody(), dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/vc/presentation_requests?dry_run=true", "{\"vct\":\"s\",\"format\":\"s\"}", true, c => c.Vc.PresentationRequests.CreateAsync(body: new VcPresentationRequestsCreateBody { Vct = "s", Format = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/vc/presentation_requests/a%2Fb", null, false, c => c.Vc.PresentationRequests.GetAsync("a/b"));
        }

        [Fact]
        public async Task Listen_Operations_HitTheirRoutes()
        {
            await Row("POST", "/v1/listen/sessions?dry_run=true", "{\"events\":[\"a\"],\"ttl_seconds\":7}", true, c => c.Listen.CreateSessionAsync(body: new ListenCreateSessionBody { Events = new List<string> { "a" }, TtlSeconds = 7 }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/listen/sessions/a%2Fb/stream?events=q1&after=q1&max_events=1&poll_ms=1&max_wait_seconds=1", null, false, c => c.Listen.StreamSessionAsync("a/b", events: "q1", after: "q1", maxEvents: 1, pollMs: 1, maxWaitSeconds: 1));
        }

        [Fact]
        public async Task AgentRegistration_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/agent_registration", null, false, c => c.AgentRegistration.GetAsync());
            await Row("PUT", "/v1/agent_registration?dry_run=true", "{\"enabled\":true,\"allow_anonymous\":true}", false, c => c.AgentRegistration.ReplaceAsync(body: new AgentRegistrationReplaceBody { Enabled = true, AllowAnonymous = true }, dryRun: true));
        }

        [Fact]
        public async Task Agents_Operations_HitTheirRoutes()
        {
            await Row("POST", "/v1/agents/a%2Fb/claim?dry_run=true", "{\"user_id\":\"s\"}", true, c => c.Agents.ClaimAsync("a/b", body: new AgentsClaimBody { UserId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/agents?owner_user_id=q1&tier=q1&limit=1&starting_after=q1", null, false, c => c.Agents.ListAsync(ownerUserId: "q1", tier: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/agents/a%2Fb", null, false, c => c.Agents.GetAsync("a/b"));
            await Row("PUT", "/v1/agents/a%2Fb/owner?dry_run=true", "{\"user_id\":\"s\"}", false, c => c.Agents.SetOwnerAsync("a/b", body: new AgentsSetOwnerBody { UserId = "s" }, dryRun: true));
            await Row("POST", "/v1/agents/a%2Fb/revoke_access?dry_run=true", "{\"disable\":true}", true, c => c.Agents.RevokeAccessAsync("a/b", body: new AgentsRevokeAccessBody { Disable = true }, dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task AgentAccessRequests_Operations_HitTheirRoutes()
        {
            await Row("POST", "/v1/agent_access_requests/a%2Fb/approve?dry_run=true", "{\"expires_in_seconds\":7,\"decided_by\":\"s\"}", true, c => c.AgentAccessRequests.ApproveAsync("a/b", body: new AgentAccessRequestsApproveBody { ExpiresInSeconds = 7, DecidedBy = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/agent_access_requests/a%2Fb/deny?dry_run=true", "{\"decided_by\":\"s\"}", true, c => c.AgentAccessRequests.DenyAsync("a/b", body: new AgentAccessRequestsDenyBody { DecidedBy = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/agent_access_requests?status=q1&agent_id=q1&limit=1&starting_after=q1", null, false, c => c.AgentAccessRequests.ListAsync(status: "q1", agentId: "q1", limit: 1, startingAfter: "q1"));
        }

        [Fact]
        public async Task Actions_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/actions/a%2Fb/calls?limit=1&status=q1&test=true&starting_after=q1", null, false, c => c.Actions.CallsAsync("a/b", limit: 1, status: "q1", test: true, startingAfter: "q1"));
        }

        [Fact]
        public async Task Workflows_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/workflows?limit=1&starting_after=q1", null, false, c => c.Workflows.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/workflows?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", true, c => c.Workflows.CreateAsync(body: new WorkflowsCreateBody { Name = "s", Description = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/workflows/a%2Fb", null, false, c => c.Workflows.GetAsync("a/b"));
            await Row("PATCH", "/v1/workflows/a%2Fb?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", false, c => c.Workflows.UpdateAsync("a/b", body: new WorkflowsUpdateBody { Name = "s", Description = "s" }, dryRun: true));
            await Row("DELETE", "/v1/workflows/a%2Fb?dry_run=true", null, false, c => c.Workflows.DeleteAsync("a/b", dryRun: true));
            await Row("POST", "/v1/workflows/a%2Fb/dry_run?dry_run=true", "{\"user_id\":\"s\",\"organization_id\":\"s\"}", true, c => c.Workflows.DryRunAsync("a/b", body: new WorkflowsDryRunBody { UserId = "s", OrganizationId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/workflows/a%2Fb/run?dry_run=true", "{\"user_id\":\"s\",\"organization_id\":\"s\"}", true, c => c.Workflows.RunAsync("a/b", body: new WorkflowsRunBody { UserId = "s", OrganizationId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/workflows/a%2Fb/runs?limit=1&starting_after=q1", null, false, c => c.Workflows.RunsAsync("a/b", limit: 1, startingAfter: "q1"));
        }

        [Fact]
        public async Task PageTemplates_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/instance/page_templates?limit=1&starting_after=q1", null, false, c => c.PageTemplates.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/instance/page_templates/a%2Fb", null, false, c => c.PageTemplates.GetAsync("a/b"));
            await Row("PUT", "/v1/instance/page_templates/a%2Fb?dry_run=true", "{}", false, c => c.PageTemplates.SaveDraftAsync("a/b", body: new PageTemplatesSaveDraftBody(), dryRun: true));
            await Row("DELETE", "/v1/instance/page_templates/a%2Fb?dry_run=true&status=q1", null, false, c => c.PageTemplates.DiscardAsync("a/b", dryRun: true, status: "q1"));
            await Row("POST", "/v1/instance/page_templates/a%2Fb/preview?dry_run=true", null, true, c => c.PageTemplates.PreviewAsync("a/b", dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/instance/page_templates/a%2Fb/promote?dry_run=true", null, true, c => c.PageTemplates.PromoteAsync("a/b", dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task ServiceConnectors_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/service_connector_catalog?limit=1&starting_after=q1", null, false, c => c.ServiceConnectors.CatalogAsync(limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/service_connectors?limit=1&starting_after=q1", null, false, c => c.ServiceConnectors.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/service_connectors?dry_run=true", "{\"name\":\"s\",\"kind\":\"s\"}", true, c => c.ServiceConnectors.CreateAsync(body: new ServiceConnectorsCreateBody { Name = "s", Kind = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/service_connectors/a%2Fb", null, false, c => c.ServiceConnectors.GetAsync("a/b"));
            await Row("PATCH", "/v1/service_connectors/a%2Fb?dry_run=true", "{\"name\":\"s\",\"enabled\":true}", false, c => c.ServiceConnectors.UpdateAsync("a/b", body: new ServiceConnectorsUpdateBody { Name = "s", Enabled = true }, dryRun: true));
            await Row("DELETE", "/v1/service_connectors/a%2Fb?dry_run=true", null, false, c => c.ServiceConnectors.DeleteAsync("a/b", dryRun: true));
            await Row("POST", "/v1/service_connectors/a%2Fb/invoke?dry_run=true", "{\"operation\":\"s\"}", true, c => c.ServiceConnectors.InvokeAsync("a/b", body: new ServiceConnectorsInvokeBody { Operation = "s" }, dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task DesktopMfa_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/desktop_mfa/policy", null, false, c => c.DesktopMfa.GetPolicyAsync());
            await Row("PUT", "/v1/desktop_mfa/policy?dry_run=true", "{\"enabled\":true,\"applies_to\":\"s\"}", false, c => c.DesktopMfa.UpdatePolicyAsync(body: new DesktopMfaUpdatePolicyBody { Enabled = true, AppliesTo = "s" }, dryRun: true));
            await Row("POST", "/v1/desktop_mfa/enrolment_codes?dry_run=true", "{\"user_id\":\"s\"}", true, c => c.DesktopMfa.CreateEnrolmentCodeAsync(body: new DesktopMfaCreateEnrolmentCodeBody { UserId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/desktop_mfa/devices?user_id=q1&limit=1&starting_after=q1", null, false, c => c.DesktopMfa.ListDevicesAsync(userId: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/desktop_mfa/devices/a%2Fb", null, false, c => c.DesktopMfa.GetDeviceAsync("a/b"));
            await Row("DELETE", "/v1/desktop_mfa/devices/a%2Fb?dry_run=true", null, false, c => c.DesktopMfa.DeleteDeviceAsync("a/b", dryRun: true));
            await Row("POST", "/v1/desktop_mfa/devices/a%2Fb/revoke?dry_run=true", null, true, c => c.DesktopMfa.RevokeDeviceAsync("a/b", dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/desktop_mfa/devices/a%2Fb/revoke_offline_cache?dry_run=true", null, true, c => c.DesktopMfa.RevokeOfflineCacheAsync("a/b", dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task IdentityVerification_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/identity_verification/settings", null, false, c => c.IdentityVerification.SettingsAsync());
            await Row("PUT", "/v1/identity_verification/settings?dry_run=true", "{\"enabled\":true,\"connector_id\":\"s\"}", false, c => c.IdentityVerification.UpdateSettingsAsync(body: new IdentityVerificationUpdateSettingsBody { Enabled = true, ConnectorId = "s" }, dryRun: true));
            await Row("GET", "/v1/identity_verifications?status=q1&limit=1&starting_after=q1", null, false, c => c.IdentityVerification.ListAsync(status: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/identity_verifications/a%2Fb", null, false, c => c.IdentityVerification.GetAsync("a/b"));
            await Row("POST", "/v1/identity_verifications/a%2Fb/decide?dry_run=true", "{\"decision\":\"s\",\"reason\":\"s\"}", true, c => c.IdentityVerification.DecideAsync("a/b", body: new IdentityVerificationDecideBody { Decision = "s", Reason = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/users/a%2Fb/identity_verifications?limit=1&starting_after=q1", null, false, c => c.IdentityVerification.ForUserAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/users/a%2Fb/identity_verifications?dry_run=true", "{\"redirect_url\":\"s\"}", true, c => c.IdentityVerification.StartAsync("a/b", body: new IdentityVerificationStartBody { RedirectUrl = "s" }, dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task Iga_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/iga/entitlements?kind=q1&limit=1&starting_after=q1", null, false, c => c.Iga.Entitlements.ListAsync(kind: "q1", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/iga/entitlements?dry_run=true", "{\"key\":\"s\",\"name\":\"s\"}", true, c => c.Iga.Entitlements.CreateAsync(body: new IgaEntitlementsCreateBody { Key = "s", Name = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/iga/entitlements/a%2Fb", null, false, c => c.Iga.Entitlements.GetAsync("a/b"));
            await Row("PATCH", "/v1/iga/entitlements/a%2Fb?dry_run=true", "{\"key\":\"s\",\"name\":\"s\"}", false, c => c.Iga.Entitlements.UpdateAsync("a/b", body: new IgaEntitlementsUpdateBody { Key = "s", Name = "s" }, dryRun: true));
            await Row("DELETE", "/v1/iga/entitlements/a%2Fb?dry_run=true", null, false, c => c.Iga.Entitlements.DeleteAsync("a/b", dryRun: true));
            await Row("GET", "/v1/iga/requests?status=q1&user_id=q1&requester_user_id=q1&entitlement=q1&limit=1&starting_after=q1", null, false, c => c.Iga.Requests.ListAsync(status: "q1", userId: "q1", requesterUserId: "q1", entitlement: "q1", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/iga/requests?dry_run=true", "{\"entitlement\":\"s\",\"user_id\":\"s\"}", true, c => c.Iga.Requests.CreateAsync(body: new IgaRequestsCreateBody { Entitlement = "s", UserId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/iga/requests/a%2Fb", null, false, c => c.Iga.Requests.GetAsync("a/b"));
            await Row("POST", "/v1/iga/requests/a%2Fb/decide?dry_run=true", "{\"decision\":\"s\",\"comment\":\"s\"}", true, c => c.Iga.Requests.DecideAsync("a/b", body: new IgaRequestsDecideBody { Decision = "s", Comment = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/iga/requests/a%2Fb/cancel?dry_run=true", null, true, c => c.Iga.Requests.CancelAsync("a/b", dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/iga/approvals?approver_user_id=q1&limit=1&starting_after=q1", null, false, c => c.Iga.Requests.InboxAsync(approverUserId: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/iga/grants?user_id=q1&entitlement=q1&status=q1&limit=1&starting_after=q1", null, false, c => c.Iga.Grants.ListAsync(userId: "q1", entitlement: "q1", status: "q1", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/iga/grants?dry_run=true", "{\"entitlement\":\"s\",\"user_id\":\"s\"}", true, c => c.Iga.Grants.CreateAsync(body: new IgaGrantsCreateBody { Entitlement = "s", UserId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/iga/grants/a%2Fb/revoke?dry_run=true", "{\"reason\":\"s\"}", true, c => c.Iga.Grants.RevokeAsync("a/b", body: new IgaGrantsRevokeBody { Reason = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/iga/delegations?delegator_user_id=q1&limit=1&starting_after=q1", null, false, c => c.Iga.Delegations.ListAsync(delegatorUserId: "q1", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/iga/delegations?dry_run=true", "{\"delegator_user_id\":\"s\",\"delegate_user_id\":\"s\"}", true, c => c.Iga.Delegations.CreateAsync(body: new IgaDelegationsCreateBody { DelegatorUserId = "s", DelegateUserId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("DELETE", "/v1/iga/delegations/a%2Fb?dry_run=true", null, false, c => c.Iga.Delegations.DeleteAsync("a/b", dryRun: true));
            await Row("GET", "/v1/iga/sod_rules?limit=1&starting_after=q1", null, false, c => c.Iga.Sod.ListRulesAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/iga/sod_rules?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", true, c => c.Iga.Sod.CreateRuleAsync(body: new IgaSodCreateRuleBody { Name = "s", Description = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("PATCH", "/v1/iga/sod_rules/a%2Fb?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", false, c => c.Iga.Sod.UpdateRuleAsync("a/b", body: new IgaSodUpdateRuleBody { Name = "s", Description = "s" }, dryRun: true));
            await Row("DELETE", "/v1/iga/sod_rules/a%2Fb?dry_run=true", null, false, c => c.Iga.Sod.DeleteRuleAsync("a/b", dryRun: true));
            await Row("GET", "/v1/iga/sod_violations?status=q1&limit=1&starting_after=q1", null, false, c => c.Iga.Sod.ViolationsAsync(status: "q1", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/iga/sod_violations/scan?dry_run=true", null, true, c => c.Iga.Sod.ScanAsync(dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/iga/birthright_rules?limit=1&starting_after=q1", null, false, c => c.Iga.Lifecycle.ListBirthrightRulesAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/iga/birthright_rules?dry_run=true", "{\"name\":\"s\",\"entitlement_ids\":[\"a\"]}", true, c => c.Iga.Lifecycle.CreateBirthrightRuleAsync(body: new IgaLifecycleCreateBirthrightRuleBody { Name = "s", EntitlementIds = new List<string> { "a" } }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("PATCH", "/v1/iga/birthright_rules/a%2Fb?dry_run=true", "{\"name\":\"s\",\"entitlement_ids\":[\"a\"]}", false, c => c.Iga.Lifecycle.UpdateBirthrightRuleAsync("a/b", body: new IgaLifecycleUpdateBirthrightRuleBody { Name = "s", EntitlementIds = new List<string> { "a" } }, dryRun: true));
            await Row("DELETE", "/v1/iga/birthright_rules/a%2Fb?dry_run=true", null, false, c => c.Iga.Lifecycle.DeleteBirthrightRuleAsync("a/b", dryRun: true));
            await Row("POST", "/v1/iga/users/a%2Fb/reconcile?dry_run=true", null, true, c => c.Iga.Lifecycle.ReconcileAsync("a/b", dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/iga/users/a%2Fb/offboard?dry_run=true", "{\"close_account\":true,\"grace_seconds\":7}", true, c => c.Iga.Lifecycle.OffboardAsync("a/b", body: new IgaLifecycleOffboardBody { CloseAccount = true, GraceSeconds = 7 }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/iga/settings", null, false, c => c.Iga.Lifecycle.SettingsAsync());
            await Row("PUT", "/v1/iga/settings?dry_run=true", "{\"jml_enabled\":true,\"leaver_action\":\"s\"}", false, c => c.Iga.Lifecycle.UpdateSettingsAsync(body: new IgaLifecycleUpdateSettingsBody { JmlEnabled = true, LeaverAction = "s" }, dryRun: true));
            await Row("GET", "/v1/iga/campaigns?status=q1&limit=1&starting_after=q1", null, false, c => c.Iga.Campaigns.ListAsync(status: "q1", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/iga/campaigns?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", true, c => c.Iga.Campaigns.CreateAsync(body: new IgaCampaignsCreateBody { Name = "s", Description = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/iga/campaigns/a%2Fb", null, false, c => c.Iga.Campaigns.GetAsync("a/b"));
            await Row("DELETE", "/v1/iga/campaigns/a%2Fb?dry_run=true", null, false, c => c.Iga.Campaigns.DeleteAsync("a/b", dryRun: true));
            await Row("POST", "/v1/iga/campaigns/a%2Fb/launch?dry_run=true", null, true, c => c.Iga.Campaigns.LaunchAsync("a/b", dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/iga/campaigns/a%2Fb/close?dry_run=true", null, true, c => c.Iga.Campaigns.CloseAsync("a/b", dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/iga/campaigns/a%2Fb/items?decision=q1&reviewer_user_id=q1&limit=1&starting_after=q1", null, false, c => c.Iga.Campaigns.ItemsAsync("a/b", decision: "q1", reviewerUserId: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/iga/campaigns/a%2Fb/progress", null, false, c => c.Iga.Campaigns.ProgressAsync("a/b"));
            await Row("GET", "/v1/iga/campaigns/a%2Fb/evidence?format=q1&masked_emails=q1", null, false, c => c.Iga.Campaigns.EvidenceAsync("a/b", format: "q1", maskedEmails: "q1"));
            await Row("POST", "/v1/iga/campaign_items/a%2Fb/decide?dry_run=true", "{\"decision\":\"s\",\"comment\":\"s\"}", true, c => c.Iga.Campaigns.DecideAsync("a/b", body: new IgaCampaignsDecideBody { Decision = "s", Comment = "s" }, dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task ScimProvisioningPresets_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/scim_provisioning_presets?limit=1&starting_after=q1", null, false, c => c.ScimProvisioningPresets.ListAsync(limit: 1, startingAfter: "q1"));
        }

        [Fact]
        public async Task Vault_Operations_HitTheirRoutes()
        {
            await Row("POST", "/v1/vault/objects?dry_run=true", "{\"name\":\"s\",\"value\":\"s\"}", true, c => c.Vault.CreateAsync(body: new VaultCreateBody { Name = "s", Value = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/vault/objects?name_prefix=q1&key_context=q1&starting_after=q1&limit=1", null, false, c => c.Vault.ListAsync(namePrefix: "q1", keyContext: "q1", startingAfter: "q1", limit: 1));
            await Row("GET", "/v1/vault/objects/a%2Fb/metadata", null, false, c => c.Vault.MetadataAsync("a/b"));
            await Row("GET", "/v1/vault/objects/a%2Fb/versions?limit=1&starting_after=q1", null, false, c => c.Vault.VersionsAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/vault/objects/a%2Fb?version=1", null, false, c => c.Vault.ReadAsync("a/b", version: 1));
            await Row("PUT", "/v1/vault/objects/a%2Fb?dry_run=true", "{\"value\":\"s\"}", false, c => c.Vault.UpdateAsync("a/b", body: new VaultUpdateBody { Value = "s" }, dryRun: true));
            await Row("DELETE", "/v1/vault/objects/a%2Fb?dry_run=true", null, false, c => c.Vault.DeleteAsync("a/b", dryRun: true));
            await Row("GET", "/v1/vault/key_provider", null, false, c => c.Vault.KeyProvider.GetAsync());
            await Row("PUT", "/v1/vault/key_provider?dry_run=true", "{\"provider\":\"s\"}", false, c => c.Vault.KeyProvider.SetAsync(body: new VaultKeyProviderSetBody { Provider = "s" }, dryRun: true));
            await Row("DELETE", "/v1/vault/key_provider?dry_run=true", null, false, c => c.Vault.KeyProvider.RemoveAsync(dryRun: true));
        }

        [Fact]
        public async Task Connections_Operations_HitTheirRoutes()
        {
            await Row("POST", "/v1/connections?dry_run=true", "{\"kind\":\"s\",\"provider\":\"s\"}", true, c => c.Connections.CreateAsync(body: new ConnectionsCreateBody { Kind = "s", Provider = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/connections?owner_type=q1&owner_id=q1&provider=q1&environment=q1&status=q1&starting_after=q1&limit=1", null, false, c => c.Connections.ListAsync(ownerType: "q1", ownerId: "q1", provider: "q1", environment: "q1", status: "q1", startingAfter: "q1", limit: 1));
            await Row("GET", "/v1/connections/a%2Fb", null, false, c => c.Connections.GetAsync("a/b"));
            await Row("PATCH", "/v1/connections/a%2Fb?dry_run=true", "{\"name\":\"s\",\"allowed_hosts\":[\"a\"]}", false, c => c.Connections.UpdateAsync("a/b", body: new ConnectionsUpdateBody { Name = "s", AllowedHosts = new List<string> { "a" } }, dryRun: true));
            await Row("DELETE", "/v1/connections/a%2Fb?dry_run=true&revoke=true", null, false, c => c.Connections.DeleteAsync("a/b", dryRun: true, revoke: true));
            await Row("PUT", "/v1/connections/a%2Fb/credentials?dry_run=true", "{\"api_key\":\"s\",\"token\":\"s\"}", false, c => c.Connections.SetCredentialsAsync("a/b", body: new ConnectionsSetCredentialsBody { ApiKey = "s", Token = "s" }, dryRun: true));
            await Row("POST", "/v1/connections/a%2Fb/connect?dry_run=true", "{\"redirect_url\":\"s\",\"environment\":\"s\"}", true, c => c.Connections.ConnectAsync("a/b", body: new ConnectionsConnectBody { RedirectUrl = "s", Environment = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/connections/a%2Fb/revoke?dry_run=true", "{}", true, c => c.Connections.RevokeAsync("a/b", body: new ConnectionsRevokeBody(), dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/connections/a%2Fb/oauth1/sign?dry_run=true", "{\"method\":\"s\",\"url\":\"s\"}", true, c => c.Connections.OAuth1SignAsync("a/b", body: new ConnectionsOAuth1SignBody { Method = "s", Url = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/connections/a%2Fb/relay?dry_run=true", "{\"method\":\"s\",\"url\":\"s\"}", true, c => c.Connections.RelayAsync("a/b", body: new ConnectionsRelayBody { Method = "s", Url = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/connections/a%2Fb/token?min_ttl_seconds=1&subject=q1&refresh=q1", null, false, c => c.Connections.TokenAsync("a/b", minTtlSeconds: 1, subject: "q1", refresh: "q1"));
            await Row("POST", "/v1/connections/a%2Fb/token/refresh?dry_run=true", "{\"reason\":\"s\",\"stale_token_hash\":\"s\"}", true, c => c.Connections.RefreshTokenAsync("a/b", body: new ConnectionsRefreshTokenBody { Reason = "s", StaleTokenHash = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/connections/tokens/batch?dry_run=true", "{\"ids\":[\"a\"],\"min_ttl_seconds\":7}", true, c => c.Connections.TokensAsync(body: new ConnectionsTokensBody { Ids = new List<string> { "a" }, MinTtlSeconds = 7 }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/connections/import?dry_run=true", "{\"validate\":true,\"dry_run\":true}", true, c => c.Connections.ImportConnectionsAsync(body: new ConnectionsImportConnectionsBody { Validate = true, DryRun = true }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/connections/export?dry_run=true", "{\"owner_type\":\"s\",\"owner_id\":\"s\"}", true, c => c.Connections.ExportConnectionsAsync(body: new ConnectionsExportConnectionsBody { OwnerType = "s", OwnerId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/connections/a%2Fb/secret?version=q1", null, false, c => c.Connections.SecretAsync("a/b", version: "q1"));
            await Row("GET", "/v1/connections/a%2Fb/credentials/versions?limit=1&starting_after=q1", null, false, c => c.Connections.SecretVersionsAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/connections/a%2Fb/credentials/rotate?dry_run=true", "{\"overlap_seconds\":7}", true, c => c.Connections.RotateCredentialsAsync("a/b", body: new ConnectionsRotateCredentialsBody { OverlapSeconds = 7 }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("PUT", "/v1/connections/oauth_clients?dry_run=true", "{\"provider\":\"s\",\"environment\":\"s\"}", false, c => c.Connections.OAuthClients.SetAsync(body: new ConnectionsOAuthClientsSetBody { Provider = "s", Environment = "s" }, dryRun: true));
            await Row("GET", "/v1/connections/oauth_clients?owner_type=q1&owner_id=q1&provider=q1&limit=1&starting_after=q1", null, false, c => c.Connections.OAuthClients.ListAsync(ownerType: "q1", ownerId: "q1", provider: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/connections/oauth_clients/a%2Fb", null, false, c => c.Connections.OAuthClients.GetAsync("a/b"));
            await Row("DELETE", "/v1/connections/oauth_clients/a%2Fb?dry_run=true", null, false, c => c.Connections.OAuthClients.DeleteAsync("a/b", dryRun: true));
            await Row("POST", "/v1/connections/a%2Fb/webhooks/verify?dry_run=true", "{\"scheme\":\"s\",\"raw_body\":\"s\"}", true, c => c.Connections.Webhooks.VerifyAsync("a/b", body: new ConnectionsWebhooksVerifyBody { Scheme = "s", RawBody = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/connections/a%2Fb/webhook_endpoints?dry_run=true", "{\"scheme\":\"s\",\"forward_url\":\"s\"}", true, c => c.Connections.Webhooks.CreateEndpointAsync("a/b", body: new ConnectionsWebhooksCreateEndpointBody { Scheme = "s", ForwardUrl = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/connections/a%2Fb/webhook_endpoints?limit=1&starting_after=q1", null, false, c => c.Connections.Webhooks.ListEndpointsAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("PATCH", "/v1/connections/a%2Fb/webhook_endpoints/p2?dry_run=true", "{\"scheme\":\"s\",\"forward_url\":\"s\"}", false, c => c.Connections.Webhooks.UpdateEndpointAsync("a/b", "p2", body: new ConnectionsWebhooksUpdateEndpointBody { Scheme = "s", ForwardUrl = "s" }, dryRun: true));
            await Row("DELETE", "/v1/connections/a%2Fb/webhook_endpoints/p2?dry_run=true", null, false, c => c.Connections.Webhooks.DeleteEndpointAsync("a/b", "p2", dryRun: true));
        }

        [Fact]
        public async Task Fga_Operations_HitTheirRoutes()
        {
            await Row("POST", "/v1/fga/stores/a%2Fb/list-users?dry_run=true", "{\"relation\":\"s\",\"authorization_model_id\":\"s\"}", true, c => c.Fga.ListUsersAsync("a/b", body: new FgaListUsersBody { Relation = "s", AuthorizationModelId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/fga/stores/a%2Fb/changes?type=q1&page_size=1&continuation_token=q1&start_time=q1&limit=1&starting_after=q1", null, false, c => c.Fga.ReadChangesAsync("a/b", type: "q1", pageSize: 1, continuationToken: "q1", startTime: "q1", limit: 1, startingAfter: "q1"));
        }

        [Fact]
        public async Task OpenFga_Operations_HitTheirRoutes()
        {
            await Row("POST", "/v1/openfga/stores/a%2Fb/list-users?dry_run=true", "{\"relation\":\"s\",\"authorization_model_id\":\"s\"}", true, c => c.OpenFga.ListUsersAsync("a/b", body: new OpenFgaListUsersBody { Relation = "s", AuthorizationModelId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/openfga/stores/a%2Fb/changes?type=q1&page_size=1&continuation_token=q1&start_time=q1", null, false, c => c.OpenFga.ReadChangesAsync("a/b", type: "q1", pageSize: 1, continuationToken: "q1", startTime: "q1"));
        }

        [Fact]
        public async Task AttackProtection_Operations_HitTheirRoutes()
        {
            await Row("PUT", "/v1/attack_protection/app_attestation_secret?dry_run=true", "{\"secret\":\"s\",\"kind\":\"s\"}", false, c => c.AttackProtection.SetAppAttestationSecretAsync(body: new AttackProtectionSetAppAttestationSecretBody { Secret = "s", Kind = "s" }, dryRun: true));
            await Row("DELETE", "/v1/attack_protection/app_attestation_secret?dry_run=true&kind=q1", null, false, c => c.AttackProtection.RemoveAppAttestationSecretAsync(dryRun: true, kind: "q1"));
        }

        [Fact]
        public async Task RiskBasedMfa_Operations_HitTheirRoutes()
        {
            await Row("PUT", "/v1/risk_based_mfa/reputation_secret?dry_run=true", "{\"secret\":\"s\",\"provider\":\"s\"}", false, c => c.RiskBasedMfa.SetReputationSecretAsync(body: new RiskBasedMfaSetReputationSecretBody { Secret = "s", Provider = "s" }, dryRun: true));
            await Row("DELETE", "/v1/risk_based_mfa/reputation_secret?dry_run=true&provider=q1", null, false, c => c.RiskBasedMfa.RemoveReputationSecretAsync(dryRun: true, provider: "q1"));
        }

        [Fact]
        public async Task Ssf_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/ssf/streams?limit=1&starting_after=q1", null, false, c => c.Ssf.Streams.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/ssf/streams?dry_run=true", "{\"delivery_method\":\"s\",\"endpoint_url\":\"s\"}", true, c => c.Ssf.Streams.CreateAsync(body: new SsfStreamsCreateBody { DeliveryMethod = "s", EndpointUrl = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/ssf/streams/a%2Fb", null, false, c => c.Ssf.Streams.GetAsync("a/b"));
            await Row("PATCH", "/v1/ssf/streams/a%2Fb?dry_run=true", "{\"endpoint_url\":\"s\",\"authorization_header\":\"s\"}", false, c => c.Ssf.Streams.UpdateAsync("a/b", body: new SsfStreamsUpdateBody { EndpointUrl = "s", AuthorizationHeader = "s" }, dryRun: true));
            await Row("DELETE", "/v1/ssf/streams/a%2Fb?dry_run=true", null, false, c => c.Ssf.Streams.DeleteAsync("a/b", dryRun: true));
            await Row("POST", "/v1/ssf/streams/a%2Fb/verify?dry_run=true", "{\"state\":\"s\"}", true, c => c.Ssf.Streams.VerifyAsync("a/b", body: new SsfStreamsVerifyBody { State = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/ssf/streams/a%2Fb/status", null, false, c => c.Ssf.Streams.GetStatusAsync("a/b"));
            await Row("POST", "/v1/ssf/streams/a%2Fb/status?dry_run=true", "{\"status\":\"s\"}", true, c => c.Ssf.Streams.UpdateStatusAsync("a/b", body: new SsfStreamsUpdateStatusBody { Status = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/ssf/streams/a%2Fb/events?limit=1&starting_after=q1", null, false, c => c.Ssf.Streams.EventsAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/ssf/receivers?limit=1&starting_after=q1", null, false, c => c.Ssf.Receivers.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/ssf/receivers?dry_run=true", "{\"name\":\"s\",\"issuer\":\"s\"}", true, c => c.Ssf.Receivers.CreateAsync(body: new SsfReceiversCreateBody { Name = "s", Issuer = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("PATCH", "/v1/ssf/receivers/a%2Fb?dry_run=true", "{\"name\":\"s\",\"jwks_uri\":\"s\"}", false, c => c.Ssf.Receivers.UpdateAsync("a/b", body: new SsfReceiversUpdateBody { Name = "s", JwksUri = "s" }, dryRun: true));
            await Row("DELETE", "/v1/ssf/receivers/a%2Fb?dry_run=true", null, false, c => c.Ssf.Receivers.DeleteAsync("a/b", dryRun: true));
        }

        [Fact]
        public async Task NetworkZones_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/network_zones?limit=1&starting_after=q1", null, false, c => c.NetworkZones.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/network_zones?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", true, c => c.NetworkZones.CreateAsync(body: new NetworkZonesCreateBody { Name = "s", Description = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/network_zones/a%2Fb", null, false, c => c.NetworkZones.GetAsync("a/b"));
            await Row("PATCH", "/v1/network_zones/a%2Fb?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", false, c => c.NetworkZones.UpdateAsync("a/b", body: new NetworkZonesUpdateBody { Name = "s", Description = "s" }, dryRun: true));
            await Row("DELETE", "/v1/network_zones/a%2Fb?dry_run=true", null, false, c => c.NetworkZones.DeleteAsync("a/b", dryRun: true));
            await Row("POST", "/v1/network_zones/a%2Fb/check?dry_run=true", "{\"ip\":\"s\"}", true, c => c.NetworkZones.CheckAsync("a/b", body: new NetworkZonesCheckBody { Ip = "s" }, dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task AccessPolicies_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/access_policies?limit=1&starting_after=q1", null, false, c => c.AccessPolicies.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/access_policies?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", true, c => c.AccessPolicies.CreateAsync(body: new AccessPoliciesCreateBody { Name = "s", Description = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/access_policies/simulate?dry_run=true", "{\"policy_id\":\"s\",\"stage\":\"s\"}", true, c => c.AccessPolicies.SimulateAsync(body: new AccessPoliciesSimulateBody { PolicyId = "s", Stage = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/access_policies/a%2Fb", null, false, c => c.AccessPolicies.GetAsync("a/b"));
            await Row("PATCH", "/v1/access_policies/a%2Fb?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", false, c => c.AccessPolicies.UpdateAsync("a/b", body: new AccessPoliciesUpdateBody { Name = "s", Description = "s" }, dryRun: true));
            await Row("DELETE", "/v1/access_policies/a%2Fb?dry_run=true", null, false, c => c.AccessPolicies.DeleteAsync("a/b", dryRun: true));
        }

        [Fact]
        public async Task Abac_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/abac_policies?limit=1&starting_after=q1", null, false, c => c.Abac.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/abac_policies?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", true, c => c.Abac.CreateAsync(body: new AbacCreateBody { Name = "s", Description = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/abac_policies/a%2Fb", null, false, c => c.Abac.GetAsync("a/b"));
            await Row("PATCH", "/v1/abac_policies/a%2Fb?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", false, c => c.Abac.UpdateAsync("a/b", body: new AbacUpdateBody { Name = "s", Description = "s" }, dryRun: true));
            await Row("DELETE", "/v1/abac_policies/a%2Fb?dry_run=true", null, false, c => c.Abac.DeleteAsync("a/b", dryRun: true));
            await Row("GET", "/v1/abac_policies/a%2Fb/versions?limit=1&starting_after=q1", null, false, c => c.Abac.VersionsAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/abac_policies/a%2Fb/test?dry_run=true", "{\"version\":7,\"source\":\"s\"}", true, c => c.Abac.TestAsync("a/b", body: new AbacTestBody { Version = 7, Source = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/abac/evaluate?dry_run=true", "{\"action\":\"s\",\"policy_ids\":[\"a\"]}", true, c => c.Abac.EvaluateAsync(body: new AbacEvaluateBody { Action = "s", PolicyIds = new List<string> { "a" } }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/abac/partial_evaluate?dry_run=true", "{\"action\":\"s\",\"policy_ids\":[\"a\"]}", true, c => c.Abac.PartialEvaluateAsync(body: new AbacPartialEvaluateBody { Action = "s", PolicyIds = new List<string> { "a" } }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/abac/accessible_resources?dry_run=true", "{\"action\":\"s\",\"resource_type\":\"s\"}", true, c => c.Abac.AccessibleResourcesAsync(body: new AbacAccessibleResourcesBody { Action = "s", ResourceType = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/abac_policies/a%2Fb/diff?from=1&to=1&semantic=true&max_requests=1&max_changes=1", null, false, c => c.Abac.DiffAsync("a/b", from: 1, to: 1, semantic: true, maxRequests: 1, maxChanges: 1));
            await Row("POST", "/v1/abac/diff?dry_run=true", "{\"before\":\"s\",\"after\":\"s\"}", true, c => c.Abac.DiffSourcesAsync(body: new AbacDiffSourcesBody { Before = "s", After = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/authzen/access/v1/evaluation?dry_run=true", "{}", true, c => c.Abac.AuthzenEvaluationAsync(body: new AbacAuthzenEvaluationBody(), dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/authzen/access/v1/evaluations?dry_run=true", "{}", true, c => c.Abac.AuthzenEvaluationsAsync(body: new AbacAuthzenEvaluationsBody(), dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/authzen/access/v1/search/subject?dry_run=true", "{}", true, c => c.Abac.AuthzenSearchSubjectAsync(body: new AbacAuthzenSearchSubjectBody(), dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/authzen/access/v1/search/resource?dry_run=true", "{}", true, c => c.Abac.AuthzenSearchResourceAsync(body: new AbacAuthzenSearchResourceBody(), dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/authzen/access/v1/search/action?dry_run=true", "{}", true, c => c.Abac.AuthzenSearchActionAsync(body: new AbacAuthzenSearchActionBody(), dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/abac_templates?limit=1&starting_after=q1", null, false, c => c.Abac.Templates.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/abac_templates?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", true, c => c.Abac.Templates.CreateAsync(body: new AbacTemplatesCreateBody { Name = "s", Description = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/abac_templates/a%2Fb", null, false, c => c.Abac.Templates.GetAsync("a/b"));
            await Row("PATCH", "/v1/abac_templates/a%2Fb?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", false, c => c.Abac.Templates.UpdateAsync("a/b", body: new AbacTemplatesUpdateBody { Name = "s", Description = "s" }, dryRun: true));
            await Row("DELETE", "/v1/abac_templates/a%2Fb?dry_run=true&force=true", null, false, c => c.Abac.Templates.DeleteAsync("a/b", dryRun: true, force: true));
            await Row("GET", "/v1/abac_policy_links?template_id=q1&limit=1&starting_after=q1", null, false, c => c.Abac.Links.ListAsync(templateId: "q1", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/abac_policy_links?dry_run=true", "{\"template_id\":\"s\",\"name\":\"s\"}", true, c => c.Abac.Links.CreateAsync(body: new AbacLinksCreateBody { TemplateId = "s", Name = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/abac_policy_links/a%2Fb", null, false, c => c.Abac.Links.GetAsync("a/b"));
            await Row("PATCH", "/v1/abac_policy_links/a%2Fb?dry_run=true", "{\"template_id\":\"s\",\"name\":\"s\"}", false, c => c.Abac.Links.UpdateAsync("a/b", body: new AbacLinksUpdateBody { TemplateId = "s", Name = "s" }, dryRun: true));
            await Row("DELETE", "/v1/abac_policy_links/a%2Fb?dry_run=true", null, false, c => c.Abac.Links.DeleteAsync("a/b", dryRun: true));
            await Row("GET", "/v1/abac_schema", null, false, c => c.Abac.Schema.GetAsync());
            await Row("PUT", "/v1/abac_schema?dry_run=true", "{}", false, c => c.Abac.Schema.SetAsync(body: new AbacSchemaSetBody(), dryRun: true));
            await Row("DELETE", "/v1/abac_schema?dry_run=true", null, false, c => c.Abac.Schema.DeleteAsync(dryRun: true));
            await Row("POST", "/v1/abac_schema/validate?dry_run=true", "{\"source\":\"s\",\"template\":true}", true, c => c.Abac.Schema.ValidateAsync(body: new AbacSchemaValidateBody { Source = "s", Template = true }, dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task Resources_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/resources?type=q1&parent_id=q1&limit=1&starting_after=q1", null, false, c => c.Resources.ListAsync(type: "q1", parentId: "q1", limit: 1, startingAfter: "q1"));
            await Row("PUT", "/v1/resources?dry_run=true", "{\"type\":\"s\",\"external_id\":\"s\"}", false, c => c.Resources.UpsertAsync(body: new ResourcesUpsertBody { Type = "s", ExternalId = "s" }, dryRun: true));
            await Row("POST", "/v1/resources/sync?dry_run=true", "{}", true, c => c.Resources.SyncAsync(body: new ResourcesSyncBody(), dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/resources/a%2Fb", null, false, c => c.Resources.GetAsync("a/b"));
            await Row("DELETE", "/v1/resources/a%2Fb?dry_run=true", null, false, c => c.Resources.DeleteAsync("a/b", dryRun: true));
            await Row("POST", "/v1/resource_checks?dry_run=true", "{\"permission\":\"s\",\"groups\":[\"a\"]}", true, c => c.Resources.CheckAsync(body: new ResourcesCheckBody { Permission = "s", Groups = new List<string> { "a" } }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/resource_checks/batch?dry_run=true", "{}", true, c => c.Resources.CheckBatchAsync(body: new ResourcesCheckBatchBody(), dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/resource_checks/list?dry_run=true", "{\"permission\":\"s\",\"type\":\"s\"}", true, c => c.Resources.AccessibleAsync(body: new ResourcesAccessibleBody { Permission = "s", Type = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/resource_types?limit=1&starting_after=q1", null, false, c => c.Resources.Types.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/resource_types?dry_run=true", "{\"key\":\"s\",\"name\":\"s\"}", true, c => c.Resources.Types.CreateAsync(body: new ResourcesTypesCreateBody { Key = "s", Name = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/resource_types/a%2Fb", null, false, c => c.Resources.Types.GetAsync("a/b"));
            await Row("PATCH", "/v1/resource_types/a%2Fb?dry_run=true", "{\"name\":\"s\",\"description\":\"s\"}", false, c => c.Resources.Types.UpdateAsync("a/b", body: new ResourcesTypesUpdateBody { Name = "s", Description = "s" }, dryRun: true));
            await Row("DELETE", "/v1/resource_types/a%2Fb?dry_run=true", null, false, c => c.Resources.Types.DeleteAsync("a/b", dryRun: true));
            await Row("GET", "/v1/resource_role_assignments?resource_id=q1&subject_type=q1&subject_id=q1&limit=1&starting_after=q1", null, false, c => c.Resources.Assignments.ListAsync(resourceId: "q1", subjectType: "q1", subjectId: "q1", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/resource_role_assignments?dry_run=true", "{\"role\":\"s\",\"inherit\":true}", true, c => c.Resources.Assignments.AssignAsync(body: new ResourcesAssignmentsAssignBody { Role = "s", Inherit = true }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("DELETE", "/v1/resource_role_assignments/a%2Fb?dry_run=true", null, false, c => c.Resources.Assignments.RevokeAsync("a/b", dryRun: true));
        }

        [Fact]
        public async Task OrgRoleGrants_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/org_role_grants?grantor_org_id=q1&grantee_org_id=q1&status=q1&limit=1&starting_after=q1", null, false, c => c.OrgRoleGrants.ListAsync(grantorOrgId: "q1", granteeOrgId: "q1", status: "q1", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/org_role_grants?dry_run=true", "{\"grantor_org_id\":\"s\",\"grantee_org_id\":\"s\"}", true, c => c.OrgRoleGrants.CreateAsync(body: new OrgRoleGrantsCreateBody { GrantorOrgId = "s", GranteeOrgId = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/org_role_grants/a%2Fb", null, false, c => c.OrgRoleGrants.GetAsync("a/b"));
            await Row("PATCH", "/v1/org_role_grants/a%2Fb?dry_run=true", "{\"role_keys\":[\"a\"]}", false, c => c.OrgRoleGrants.UpdateAsync("a/b", body: new OrgRoleGrantsUpdateBody { RoleKeys = new List<string> { "a" } }, dryRun: true));
            await Row("DELETE", "/v1/org_role_grants/a%2Fb?dry_run=true", null, false, c => c.OrgRoleGrants.RevokeAsync("a/b", dryRun: true));
            await Row("GET", "/v1/org_role_grants/a%2Fb/delegations?limit=1&starting_after=q1", null, false, c => c.OrgRoleGrants.ListDelegationsAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/org_role_grants/a%2Fb/delegations?dry_run=true", "{\"user_id\":\"s\",\"role_key\":\"s\"}", true, c => c.OrgRoleGrants.DelegateAsync("a/b", body: new OrgRoleGrantsDelegateBody { UserId = "s", RoleKey = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("DELETE", "/v1/org_role_grants/a%2Fb/delegations/p2?dry_run=true", null, false, c => c.OrgRoleGrants.UndelegateAsync("a/b", "p2", dryRun: true));
            await Row("GET", "/v1/org_role_grants/effective?user_id=q1&organization_id=q1&limit=1&starting_after=q1", null, false, c => c.OrgRoleGrants.EffectiveAsync(userId: "q1", organizationId: "q1", limit: 1, startingAfter: "q1"));
        }

        [Fact]
        public async Task Devices_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/devices?user_id=q1&source=q1&limit=1&starting_after=q1", null, false, c => c.Devices.ListAsync(userId: "q1", source: "q1", limit: 1, startingAfter: "q1"));
            await Row("PUT", "/v1/devices?dry_run=true", "{\"external_id\":\"s\",\"source\":\"s\"}", false, c => c.Devices.ReportAsync(body: new DevicesReportBody { ExternalId = "s", Source = "s" }, dryRun: true));
            await Row("GET", "/v1/devices/a%2Fb", null, false, c => c.Devices.GetAsync("a/b"));
            await Row("DELETE", "/v1/devices/a%2Fb?dry_run=true", null, false, c => c.Devices.RemoveAsync("a/b", dryRun: true));
            await Row("POST", "/v1/device_posture?dry_run=true", "{}", true, c => c.Devices.IngestPostureAsync(body: new DevicesIngestPostureBody(), dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task AuthzDecisions_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/authz_decisions?source=q1&decision=q1&principal=q1&action=q1&resource=q1&policy_revision=q1&since=q1&until=q1&limit=1&starting_after=q1", null, false, c => c.AuthzDecisions.ListAsync(source: "q1", decision: "q1", principal: "q1", action: "q1", resource: "q1", policyRevision: "q1", since: "q1", until: "q1", limit: 1, startingAfter: "q1"));
            await Row("GET", "/v1/authz_decisions/stats?since=q1&until=q1", null, false, c => c.AuthzDecisions.StatsAsync(since: "q1", until: "q1"));
            await Row("GET", "/v1/authz_decisions/a%2Fb", null, false, c => c.AuthzDecisions.GetAsync("a/b"));
            await Row("GET", "/v1/authz_decision_settings", null, false, c => c.AuthzDecisions.Settings.GetAsync());
            await Row("PUT", "/v1/authz_decision_settings?dry_run=true", "{\"enabled\":true,\"input_mode\":\"s\"}", false, c => c.AuthzDecisions.Settings.UpdateAsync(body: new AuthzDecisionsSettingsUpdateBody { Enabled = true, InputMode = "s" }, dryRun: true));
        }

        [Fact]
        public async Task PolicyBundles_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/policy_bundles/current", null, false, c => c.PolicyBundles.CurrentAsync());
            await Row("GET", "/v1/policy_bundles?limit=1&starting_after=q1", null, false, c => c.PolicyBundles.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/policy_bundles?dry_run=true", null, true, c => c.PolicyBundles.ExportAsync(dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/policy_bundles/a%2Fb", null, false, c => c.PolicyBundles.GetAsync("a/b"));
            await Row("GET", "/v1/policy_bundles/a%2Fb/archive", null, false, c => c.PolicyBundles.ArchiveAsync("a/b"));
            await Row("POST", "/v1/policy_bundles/verify?dry_run=true", "{\"bundle\":\"s\",\"signature\":\"s\"}", true, c => c.PolicyBundles.VerifyAsync(body: new PolicyBundlesVerifyBody { Bundle = "s", Signature = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("POST", "/v1/policy_bundles/import?dry_run=true", "{\"bundle\":\"s\",\"signature\":\"s\"}", true, c => c.PolicyBundles.ImportAsync(body: new PolicyBundlesImportBody { Bundle = "s", Signature = "s" }, dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task ProxyApps_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/proxy_apps?limit=1&starting_after=q1", null, false, c => c.ProxyApps.ListAsync(limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/proxy_apps?dry_run=true", "{\"name\":\"s\",\"hostname\":\"s\"}", true, c => c.ProxyApps.CreateAsync(body: new ProxyAppsCreateBody { Name = "s", Hostname = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/proxy_apps/a%2Fb", null, false, c => c.ProxyApps.GetAsync("a/b"));
            await Row("PATCH", "/v1/proxy_apps/a%2Fb?dry_run=true", "{\"name\":\"s\",\"hostname\":\"s\"}", false, c => c.ProxyApps.UpdateAsync("a/b", body: new ProxyAppsUpdateBody { Name = "s", Hostname = "s" }, dryRun: true));
            await Row("DELETE", "/v1/proxy_apps/a%2Fb?dry_run=true", null, false, c => c.ProxyApps.DeleteAsync("a/b", dryRun: true));
        }

        [Fact]
        public async Task McpGateways_Operations_HitTheirRoutes()
        {
            await Row("GET", "/v1/mcp_gateways", null, false, c => c.McpGateways.ListAsync());
            await Row("POST", "/v1/mcp_gateways?dry_run=true", "{\"name\":\"s\",\"upstream_url\":\"s\"}", true, c => c.McpGateways.CreateAsync(body: new McpGatewaysCreateBody { Name = "s", UpstreamUrl = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/mcp_gateways/a%2Fb", null, false, c => c.McpGateways.GetAsync("a/b"));
            await Row("PATCH", "/v1/mcp_gateways/a%2Fb?dry_run=true", "{}", false, c => c.McpGateways.UpdateAsync("a/b", body: new McpGatewaysUpdateBody(), dryRun: true));
            await Row("DELETE", "/v1/mcp_gateways/a%2Fb?dry_run=true", null, false, c => c.McpGateways.DeleteAsync("a/b", dryRun: true));
            await Row("GET", "/v1/mcp_gateways/a%2Fb/budgets?limit=1&starting_after=q1", null, false, c => c.McpGateways.Budgets.ListAsync("a/b", limit: 1, startingAfter: "q1"));
            await Row("POST", "/v1/mcp_gateways/a%2Fb/budgets?dry_run=true", "{\"name\":\"s\",\"scope\":\"s\"}", true, c => c.McpGateways.Budgets.CreateAsync("a/b", body: new McpGatewaysBudgetsCreateBody { Name = "s", Scope = "s" }, dryRun: true, idempotencyKey: "idem-1"));
            await Row("GET", "/v1/mcp_gateways/a%2Fb/budgets/p2", null, false, c => c.McpGateways.Budgets.GetAsync("a/b", "p2"));
            await Row("PATCH", "/v1/mcp_gateways/a%2Fb/budgets/p2?dry_run=true", "{\"name\":\"s\",\"scope\":\"s\"}", false, c => c.McpGateways.Budgets.UpdateAsync("a/b", "p2", body: new McpGatewaysBudgetsUpdateBody { Name = "s", Scope = "s" }, dryRun: true));
            await Row("DELETE", "/v1/mcp_gateways/a%2Fb/budgets/p2?dry_run=true", null, false, c => c.McpGateways.Budgets.DeleteAsync("a/b", "p2", dryRun: true));
            await Row("GET", "/v1/mcp_gateways/a%2Fb/budgets/p2/usage?agent_id=q1&session_id=q1", null, false, c => c.McpGateways.Budgets.UsageAsync("a/b", "p2", agentId: "q1", sessionId: "q1"));
        }

        [Fact]
        public async Task Instance_Operations_HitTheirRoutes()
        {
            await Row("POST", "/v1/instance/integrations/a%2Fb/events_secret/rotate?dry_run=true", "{\"overlap_seconds\":7}", true, c => c.Instance.Integrations.RotateEventsSecretAsync("a/b", body: new InstanceIntegrationsRotateEventsSecretBody { OverlapSeconds = 7 }, dryRun: true, idempotencyKey: "idem-1"));
        }

        [Fact]
        public async Task Integrations_Operations_HitTheirRoutes()
        {
            await Row("POST", "/v1/integrations/a%2Fb/card/actions/p2?dry_run=true", "{\"sub\":\"s\",\"iss\":\"s\"}", true, c => c.Integrations.RunCardActionAsync("a/b", "p2", body: new IntegrationsRunCardActionBody { Sub = "s", Iss = "s" }, dryRun: true, idempotencyKey: "idem-1"));
        }
    }
}
