using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    // ---- Sign in with Atlas test and review accounts ---------------------------------
    // Real Atlas accounts confined to the app(s) they were made for: reviewer logins, CI, demos.

    /// <summary>One app a provisioned account is confined to.</summary>
    public sealed class AtlasIdProvisionedAccountApp
    {
        public string Id { get; init; } = "";
        public string ClientId { get; init; } = "";
        public string Name { get; init; } = "";
    }

    /// <summary>Who created a provisioned account.</summary>
    public sealed class AtlasIdProvisionedAccountActor
    {
        public string Type { get; init; } = "";
        public string Id { get; init; } = "";
    }

    /// <summary>
    /// A Sign in with Atlas test or review account. <see cref="Password"/> / <see cref="SealedPassword"/> are present only on
    /// create and on password rotation, exactly once; <see cref="ToString"/> never prints them.
    /// </summary>
    public sealed class AtlasIdProvisionedAccount
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "atlas_id_provisioned_account";
        public string Id { get; init; } = "";
        /// <summary>The Atlas ID subject the app sees.</summary>
        public string Sub { get; init; } = "";
        public string? Email { get; init; }
        public bool EmailVerified { get; init; }
        public string Label { get; init; } = "";
        /// <summary><c>provisioned</c>, <c>test_mode</c> or <c>developer</c>.</summary>
        public string Kind { get; init; } = "";
        /// <summary><c>app</c> or <c>staff</c>.</summary>
        public string ProvisionedBy { get; init; } = "";
        public List<string> AppIds { get; init; } = new List<string>();
        public List<AtlasIdProvisionedAccountApp> Apps { get; init; } = new List<AtlasIdProvisionedAccountApp>();
        public bool AnyApp { get; init; }
        public bool TwoFactorExempt { get; init; }
        /// <summary>Epoch ms; null = never expires.</summary>
        public long? ExpiresAt { get; init; }
        public bool Expired { get; init; }
        public string? DeveloperAccountId { get; init; }
        public AtlasIdProvisionedAccountActor? CreatedBy { get; init; }
        public long? LastSignInAt { get; init; }
        public long? PasswordRotatedAt { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
        /// <summary>Only on create and password rotation: the password, returned exactly once.</summary>
        public string? Password { get; init; }
        /// <summary>With a secret recipient: the password sealed to it (then <see cref="Password"/> is absent).</summary>
        public string? SealedPassword { get; init; }
        public string? Note { get; init; }

        public override string ToString()
            => "AtlasIdProvisionedAccount { Id = " + Id + ", Email = " + Email + ", Kind = " + Kind
               + ", Password = " + (Password != null ? "***" : "null") + ", SealedPassword = " + (SealedPassword != null ? "***" : "null") + " }";
    }

    /// <summary><c>GET .../provisioned_accounts</c>.</summary>
    public sealed class AtlasIdProvisionedAccountList
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "list";
        public List<AtlasIdProvisionedAccount> Data { get; init; } = new List<AtlasIdProvisionedAccount>();
        /// <summary>The account-wide cap (default 25).</summary>
        public int Limit { get; init; }
        /// <summary>Test accounts the account holds across all its apps.</summary>
        public int Total { get; init; }
    }

    /// <summary>The body of the create call. <see cref="ToString"/> hides the password.</summary>
    public sealed class CreateAtlasIdProvisionedAccountBody
    {
        public string Email { get; set; } = "";
        /// <summary>What the account is for, e.g. "app-store reviewer".</summary>
        public string Label { get; set; } = "";
        /// <summary>At least 12 characters. Omit it and Atlas generates one. Returned once either way.</summary>
        public string? Password { get; set; }
        /// <summary>Default true.</summary>
        public bool? EmailVerified { get; set; }
        /// <summary>Never asked for a second factor when signing in to this app. A risk you opt into.</summary>
        public bool? TwoFactorExempt { get; set; }
        /// <summary>Epoch ms (<c>long</c>), an ISO 8601 string, or null for a non-expiring account.</summary>
        public object? ExpiresAt { get; set; }

        public override string ToString()
            => "CreateAtlasIdProvisionedAccountBody { Email = " + Email + ", Label = " + Label + ", Password = " + (Password != null ? "***" : "null") + " }";
    }

    /// <summary>The body of the password rotation call.</summary>
    public sealed class RotateAtlasIdProvisionedPasswordBody
    {
        /// <summary>At least 12 characters; omit it and Atlas generates one.</summary>
        public string? Password { get; set; }

        public override string ToString() => "RotateAtlasIdProvisionedPasswordBody { Password = " + (Password != null ? "***" : "null") + " }";
    }

    /// <summary>The result of removing a provisioned account.</summary>
    public sealed class AtlasIdProvisionedAccountRemoved
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "atlas_id_provisioned_account";
        public string Id { get; init; } = "";
        public bool Deleted { get; init; }
        public int SessionsEnded { get; init; }
    }

    /// <summary>
    /// Test and review accounts of one Sign in with Atlas app. Needs <c>platform:atlas_id_apps:provision</c> or the per-app
    /// <c>atlas_id_apps:&lt;app id&gt;:provision</c> scope. Passwords come back exactly once, on create and on rotation; set
    /// <c>AtlasClientOptions.SecretRecipient</c> to receive them sealed (<see cref="AtlasIdProvisionedAccount.SealedPassword"/>).
    /// </summary>
    public sealed class PlatformProvisionedAccountsResource : ResourceBase
    {
        public PlatformProvisionedAccountsResource(AtlasTransport transport) : base(transport) { }

        /// <summary><c>GET /v1/platform/atlas_id_apps/{id}/provisioned_accounts</c></summary>
        public Task<AtlasIdProvisionedAccountList> ListAsync(string appId, CancellationToken ct = default)
            => Req<AtlasIdProvisionedAccountList>(HttpVerb.Get, $"/v1/platform/atlas_id_apps/{Enc(appId)}/provisioned_accounts", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/atlas_id_apps/{id}/provisioned_accounts</c> — 409 <c>PROVISIONED_ACCOUNT_LIMIT_REACHED</c> past the cap, 422 <c>DOMAIN_NOT_VERIFIED</c> when the app restricts accounts to your verified domains.</summary>
        public Task<AtlasIdProvisionedAccount> CreateAsync(string appId, CreateAtlasIdProvisionedAccountBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<AtlasIdProvisionedAccount>(HttpVerb.Post, $"/v1/platform/atlas_id_apps/{Enc(appId)}/provisioned_accounts", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/platform/atlas_id_apps/{id}/provisioned_accounts/{acct}</c></summary>
        public Task<AtlasIdProvisionedAccount> GetAsync(string appId, string accountId, CancellationToken ct = default)
            => Req<AtlasIdProvisionedAccount>(HttpVerb.Get, $"/v1/platform/atlas_id_apps/{Enc(appId)}/provisioned_accounts/{Enc(accountId)}", cancellationToken: ct);

        /// <summary><c>DELETE /v1/platform/atlas_id_apps/{id}/provisioned_accounts/{acct}</c> — deletes the account and ends every session and grant it holds.</summary>
        public Task<AtlasIdProvisionedAccountRemoved> RemoveAsync(string appId, string accountId, CancellationToken ct = default)
            => Req<AtlasIdProvisionedAccountRemoved>(HttpVerb.Delete, $"/v1/platform/atlas_id_apps/{Enc(appId)}/provisioned_accounts/{Enc(accountId)}", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/atlas_id_apps/{id}/provisioned_accounts/{acct}/password</c> — a new password Atlas generates, returned once.</summary>
        public Task<AtlasIdProvisionedAccount> RotatePasswordAsync(string appId, string accountId, CancellationToken ct = default)
            => Req<AtlasIdProvisionedAccount>(HttpVerb.Post, $"/v1/platform/atlas_id_apps/{Enc(appId)}/provisioned_accounts/{Enc(accountId)}/password", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/atlas_id_apps/{id}/provisioned_accounts/{acct}/password</c> — set a password of your own (at least 12 characters), returned once.</summary>
        public Task<AtlasIdProvisionedAccount> RotatePasswordAsync(string appId, string accountId, RotateAtlasIdProvisionedPasswordBody body, CancellationToken ct = default)
            => Req<AtlasIdProvisionedAccount>(HttpVerb.Post, $"/v1/platform/atlas_id_apps/{Enc(appId)}/provisioned_accounts/{Enc(accountId)}/password", body, cancellationToken: ct);
    }

    // ---- account domains --------------------------------------------------------------

    /// <summary>The DNS record to publish to verify a domain.</summary>
    public sealed class AccountDomainVerification
    {
        /// <summary><c>TXT</c>.</summary>
        public string Type { get; init; } = "";
        public string Name { get; init; } = "";
        public string Value { get; init; } = "";
    }

    /// <summary>A domain the account claimed; verified ones gate test sign-in mode.</summary>
    public sealed class AccountDomain
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "account_domain";
        public string Id { get; init; } = "";
        public string Domain { get; init; } = "";
        public bool Verified { get; init; }
        public long? VerifiedAt { get; init; }
        public long? LastCheckedAt { get; init; }
        public AccountDomainVerification? Verification { get; init; }
        public bool? RecordFound { get; init; }
    }

    /// <summary><c>GET /v1/platform/domains</c>.</summary>
    public sealed class AccountDomainList
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "list";
        public List<AccountDomain> Data { get; init; } = new List<AccountDomain>();
        public List<string> VerifiedDomains { get; init; } = new List<string>();
    }

    /// <summary>Domains the account verified by DNS (<c>/v1/platform/domains</c>).</summary>
    public sealed class PlatformDomainsResource : ResourceBase
    {
        public PlatformDomainsResource(AtlasTransport transport) : base(transport) { }

        /// <summary><c>GET /v1/platform/domains</c></summary>
        public Task<AccountDomainList> ListAsync(CancellationToken ct = default)
            => Req<AccountDomainList>(HttpVerb.Get, "/v1/platform/domains", cancellationToken: ct);

        /// <summary><c>POST /v1/platform/domains</c> — claim a domain; returns the TXT record to publish.</summary>
        public Task<AccountDomain> ClaimAsync(string domain, CancellationToken ct = default)
            => Req<AccountDomain>(HttpVerb.Post, "/v1/platform/domains", Body(("domain", domain)), cancellationToken: ct);

        /// <summary><c>POST /v1/platform/domains/{id}/verify</c> — look for the TXT record now.</summary>
        public Task<AccountDomain> CheckAsync(string domainId, CancellationToken ct = default)
            => Req<AccountDomain>(HttpVerb.Post, $"/v1/platform/domains/{Enc(domainId)}/verify", cancellationToken: ct);

        /// <summary>Alias of <see cref="CheckAsync"/>.</summary>
        public Task<AccountDomain> VerifyAsync(string domainId, CancellationToken ct = default) => CheckAsync(domainId, ct);

        /// <summary><c>DELETE /v1/platform/domains/{id}</c></summary>
        public Task<DeletedObject> RemoveAsync(string domainId, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/platform/domains/{Enc(domainId)}", cancellationToken: ct);
    }
}
