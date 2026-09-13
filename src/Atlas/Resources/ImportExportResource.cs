using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class ExportedUserEmail
    {
        public string Id { get; init; } = "";
        [JsonPropertyName("email_address")] public string EmailAddress { get; init; } = "";
        public bool Verified { get; init; }
        public bool Primary { get; init; }
    }

    /// <summary>A user as serialised into an export job's <c>result</c>. Never a secret.</summary>
    public sealed class ExportedUser
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "user";
        public string Id { get; init; } = "";
        public string? Username { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? ImageUrl { get; init; }
        public Metadata PublicMetadata { get; init; } = new Metadata();
        public Metadata PrivateMetadata { get; init; } = new Metadata();
        public List<ExportedUserEmail> EmailAddresses { get; init; } = new List<ExportedUserEmail>();
        public bool MfaEnabled { get; init; }
        public bool Banned { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class Job
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "job";
        public string Id { get; init; } = "";
        public string Type { get; init; } = "";
        public string Status { get; init; } = "";
        public int Total { get; init; }
        public int Processed { get; init; }
        public int Succeeded { get; init; }
        public int Failed { get; init; }
        public int ErrorCount { get; init; }
        /// <summary>Present only on an export job: the serialised user array, or null.</summary>
        public List<ExportedUser>? Result { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
        public long? CompletedAt { get; init; }
    }

    /// <summary>One recorded per-row failure inside a job's <c>errors</c> array.</summary>
    public sealed class JobError
    {
        public int Index { get; init; }
        public string? Email { get; init; }
        public string Code { get; init; } = "";
        public string Message { get; init; } = "";
    }

    /// <summary>One user row accepted by an import. Provide <c>password</c> or a <c>password_hash</c>.</summary>
    public sealed class ImportUserRow
    {
        public string? EmailAddress { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? PasswordHash { get; set; }
        public bool? EmailVerified { get; set; }
        public bool? Verified { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public Metadata? PublicMetadata { get; set; }
        public Metadata? PrivateMetadata { get; set; }
        public List<ImportExternalAccount>? ExternalAccounts { get; set; }
    }

    public sealed class ImportExternalAccount
    {
        public string? Provider { get; set; }
        public string? ProviderUserId { get; set; }
        public string? Email { get; set; }
        public bool? EmailVerified { get; set; }
    }

    public sealed class ImportUsersBody
    {
        public List<ImportUserRow> Users { get; set; } = new List<ImportUserRow>();
        /// <summary>Update an existing user matched by verified email instead of skipping it.</summary>
        public bool? Upsert { get; set; }
    }

    /// <summary>The bulk user import/export namespace (<c>/v1/user_imports</c>, <c>/v1/jobs</c>).</summary>
    public sealed class ImportExportResource : ResourceBase
    {
        public ImportExportResource(AtlasTransport transport) : base(transport) { }

        /// <summary>Import users through the same per-row path sign-up uses. Deduped by verified email.</summary>
        public Task<Job> ImportUsersAsync(ImportUsersBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<Job>(HttpVerb.Post, "/v1/user_imports", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>Export every non-deleted user in the instance, serialised without secrets.</summary>
        public Task<Job> ExportUsersAsync(string? idempotencyKey = null, CancellationToken ct = default)
            => Req<Job>(HttpVerb.Post, "/v1/user_exports", idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>Poll the instance's import/export jobs, newest first.</summary>
        public Task<CursorPage<Job>> ListJobsAsync(CursorParams? @params = null, CancellationToken ct = default)
            => Req<CursorPage<Job>>(HttpVerb.Get, "/v1/jobs", query: (@params ?? new CursorParams()).ToQuery(), cancellationToken: ct);

        public Task<Job> GetJobAsync(string id, CancellationToken ct = default)
            => Req<Job>(HttpVerb.Get, $"/v1/jobs/{Enc(id)}", cancellationToken: ct);

        /// <summary>The per-row failures recorded against a job.</summary>
        public Task<ListPage<JobError>> GetJobErrorsAsync(string id, CancellationToken ct = default)
            => Req<ListPage<JobError>>(HttpVerb.Get, $"/v1/jobs/{Enc(id)}/errors", cancellationToken: ct);
    }
}
