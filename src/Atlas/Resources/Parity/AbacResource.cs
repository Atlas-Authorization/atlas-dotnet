using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    /// <summary>The body of <c>POST /v1/abac_policies</c>. Only the fields set are sent.</summary>
    public sealed class AbacCreateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("fga_store_id")] public string? FgaStoreId { get; set; }
        [JsonPropertyName("source")] public string? Source { get; set; }
        [JsonPropertyName("tests")] public List<object?>? Tests { get; set; }
    }

    /// <summary>The body of <c>PATCH /v1/abac_policies/{id}</c>. Only the fields set are sent.</summary>
    public sealed class AbacUpdateBody
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("fga_store_id")] public string? FgaStoreId { get; set; }
        [JsonPropertyName("source")] public string? Source { get; set; }
        [JsonPropertyName("tests")] public List<object?>? Tests { get; set; }
        [JsonPropertyName("current_version")] public long? CurrentVersion { get; set; }
    }

    /// <summary>The body of <c>POST /v1/abac_policies/{id}/test</c>. Only the fields set are sent.</summary>
    public sealed class AbacTestBody
    {
        [JsonPropertyName("version")] public long? Version { get; set; }
        [JsonPropertyName("source")] public string? Source { get; set; }
        [JsonPropertyName("tests")] public List<object?>? Tests { get; set; }
    }

    /// <summary>The body of <c>POST /v1/abac/evaluate</c>. Only the fields set are sent.</summary>
    public sealed class AbacEvaluateBody
    {
        [JsonPropertyName("principal")] public Dictionary<string, object?>? Principal { get; set; }
        [JsonPropertyName("action")] public string? Action { get; set; }
        [JsonPropertyName("resource")] public Dictionary<string, object?>? Resource { get; set; }
        [JsonPropertyName("context")] public Dictionary<string, object?>? Context { get; set; }
        [JsonPropertyName("entities")] public List<object?>? Entities { get; set; }
        [JsonPropertyName("policy_ids")] public List<string>? PolicyIds { get; set; }
    }

    /// <summary>The body of <c>POST /v1/abac/partial_evaluate</c>. Only the fields set are sent.</summary>
    public sealed class AbacPartialEvaluateBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("principal")] public Dictionary<string, object?>? Principal { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("action")] public string? Action { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("resource")] public Dictionary<string, object?>? Resource { get; set; }
        [JsonPropertyName("context")] public Dictionary<string, object?>? Context { get; set; }
        /// <summary>Other entities the policies reference (hierarchy and attributes).</summary>
        [JsonPropertyName("entities")] public List<object?>? Entities { get; set; }
        /// <summary>Only these policies (default: every enabled one).</summary>
        [JsonPropertyName("policy_ids")] public List<string>? PolicyIds { get; set; }
        /// <summary>Which entity’s attributes are unknown. Default ["resource"].</summary>
        [JsonPropertyName("unknowns")] public List<string>? Unknowns { get; set; }
        /// <summary>Replace relationship conditions with the ids the FGA store returns. Default true.</summary>
        [JsonPropertyName("resolve_relations")] public bool? ResolveRelations { get; set; }
        [JsonPropertyName("output")] public Dictionary<string, object?>? Output { get; set; }
    }

    /// <summary>The body of <c>POST /v1/abac/accessible_resources</c>. Only the fields set are sent.</summary>
    public sealed class AbacAccessibleResourcesBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("principal")] public Dictionary<string, object?>? Principal { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("action")] public string? Action { get; set; }
        /// <summary>Required. The Atlas resource type key (the policy type is its PascalCase form).</summary>
        [JsonPropertyName("resource_type")] public string? ResourceType { get; set; }
        [JsonPropertyName("context")] public Dictionary<string, object?>? Context { get; set; }
        [JsonPropertyName("entities")] public List<object?>? Entities { get; set; }
        [JsonPropertyName("candidates")] public List<object?>? Candidates { get; set; }
        [JsonPropertyName("policy_ids")] public List<string>? PolicyIds { get; set; }
        /// <summary>Default 100.</summary>
        [JsonPropertyName("limit")] public long? Limit { get; set; }
        [JsonPropertyName("starting_after")] public string? StartingAfter { get; set; }
    }

    /// <summary>The body of <c>POST /v1/abac/diff</c>. Only the fields set are sent.</summary>
    public sealed class AbacDiffSourcesBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("before")] public string? Before { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("after")] public string? After { get; set; }
        [JsonPropertyName("semantic")] public bool? Semantic { get; set; }
        [JsonPropertyName("max_requests")] public long? MaxRequests { get; set; }
        [JsonPropertyName("max_changes")] public long? MaxChanges { get; set; }
    }

    /// <summary>The body of <c>POST /v1/authzen/access/v1/evaluation</c>. Only the fields set are sent.</summary>
    public sealed class AbacAuthzenEvaluationBody
    {
        [JsonPropertyName("subject")] public Dictionary<string, object?>? Subject { get; set; }
        [JsonPropertyName("resource")] public Dictionary<string, object?>? Resource { get; set; }
        [JsonPropertyName("action")] public Dictionary<string, object?>? Action { get; set; }
        [JsonPropertyName("context")] public Dictionary<string, object?>? Context { get; set; }
        [JsonPropertyName("evaluations")] public List<object?>? Evaluations { get; set; }
        [JsonPropertyName("options")] public Dictionary<string, object?>? Options { get; set; }
    }

    /// <summary>The body of <c>POST /v1/authzen/access/v1/evaluations</c>. Only the fields set are sent.</summary>
    public sealed class AbacAuthzenEvaluationsBody
    {
        [JsonPropertyName("subject")] public Dictionary<string, object?>? Subject { get; set; }
        [JsonPropertyName("resource")] public Dictionary<string, object?>? Resource { get; set; }
        [JsonPropertyName("action")] public Dictionary<string, object?>? Action { get; set; }
        [JsonPropertyName("context")] public Dictionary<string, object?>? Context { get; set; }
        [JsonPropertyName("evaluations")] public List<object?>? Evaluations { get; set; }
        [JsonPropertyName("options")] public Dictionary<string, object?>? Options { get; set; }
    }

    /// <summary>The body of <c>POST /v1/authzen/access/v1/search/subject</c>. Only the fields set are sent.</summary>
    public sealed class AbacAuthzenSearchSubjectBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("subject")] public Dictionary<string, object?>? Subject { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("action")] public Dictionary<string, object?>? Action { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("resource")] public Dictionary<string, object?>? Resource { get; set; }
        [JsonPropertyName("context")] public Dictionary<string, object?>? Context { get; set; }
        [JsonPropertyName("page")] public Dictionary<string, object?>? Page { get; set; }
    }

    /// <summary>The body of <c>POST /v1/authzen/access/v1/search/resource</c>. Only the fields set are sent.</summary>
    public sealed class AbacAuthzenSearchResourceBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("subject")] public Dictionary<string, object?>? Subject { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("action")] public Dictionary<string, object?>? Action { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("resource")] public Dictionary<string, object?>? Resource { get; set; }
        [JsonPropertyName("context")] public Dictionary<string, object?>? Context { get; set; }
        [JsonPropertyName("page")] public Dictionary<string, object?>? Page { get; set; }
    }

    /// <summary>The body of <c>POST /v1/authzen/access/v1/search/action</c>. Only the fields set are sent.</summary>
    public sealed class AbacAuthzenSearchActionBody
    {
        /// <summary>Required.</summary>
        [JsonPropertyName("subject")] public Dictionary<string, object?>? Subject { get; set; }
        /// <summary>Required.</summary>
        [JsonPropertyName("resource")] public Dictionary<string, object?>? Resource { get; set; }
        [JsonPropertyName("context")] public Dictionary<string, object?>? Context { get; set; }
        [JsonPropertyName("page")] public Dictionary<string, object?>? Page { get; set; }
    }

    /// <summary>Operations of <c>abac</c>.</summary>
    public sealed partial class AbacResource : ResourceBase
    {
        public AbacResource(AtlasTransport t) : base(t) { }

        private AbacTemplatesResource? _templates;
        /// <summary>The <c>abac.templates</c> operations.</summary>
        public AbacTemplatesResource Templates => _templates ??= new AbacTemplatesResource(Transport);

        private AbacLinksResource? _links;
        /// <summary>The <c>abac.links</c> operations.</summary>
        public AbacLinksResource Links => _links ??= new AbacLinksResource(Transport);

        private AbacSchemaResource? _schema;
        /// <summary>The <c>abac.schema</c> operations.</summary>
        public AbacSchemaResource Schema => _schema ??= new AbacSchemaResource(Transport);

        /// <summary><c>GET /v1/abac_policies</c> - List the ABAC policies (current version of each).</summary>
        /// <remarks>Needs the <c>abac:read</c> scope.</remarks>
        public Task<JsonElement> ListAsync(long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, "/v1/abac_policies", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/abac_policies</c> - Create an ABAC policy from `source` written in the Cedar-subset policy language (permit/forbid with when/unless over principal, action, resource and context attributes; `fga.check(...)` consults the policy’s FGA store). Source that does not parse is refused with the character position; with an entity schema set it is also type-checked and every problem is reported. Optional `tests` are saved with version 1.</summary>
        /// <remarks>Needs the <c>abac:write</c> scope.</remarks>
        public Task<JsonElement> CreateAsync(AbacCreateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/abac_policies", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/abac_policies/{id}</c> - Read one ABAC policy with its current source and tests.</summary>
        /// <remarks>Needs the <c>abac:read</c> scope.</remarks>
        public Task<JsonElement> GetAsync(string id, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/abac_policies/{Enc(id)}", cancellationToken: ct);

        /// <summary><c>PATCH /v1/abac_policies/{id}</c> - Update an ABAC policy. Sending `source` appends a new version and makes it current; `current_version` (without `source`) rolls back to an earlier version; name, description, enabled and fga_store_id change in place.</summary>
        /// <remarks>Needs the <c>abac:write</c> scope.</remarks>
        public Task<JsonElement> UpdateAsync(string id, AbacUpdateBody? body = null, bool? dryRun = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Patch, $"/v1/abac_policies/{Enc(id)}", body, query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>DELETE /v1/abac_policies/{id}</c> - Delete an ABAC policy and all its versions.</summary>
        /// <remarks>Needs the <c>abac:write</c> scope.</remarks>
        public Task DeleteAsync(string id, bool? dryRun = null, CancellationToken ct = default)
            => ReqVoid(HttpVerb.Delete, $"/v1/abac_policies/{Enc(id)}", query: Q(("dry_run", dryRun)), cancellationToken: ct);

        /// <summary><c>GET /v1/abac_policies/{id}/versions</c> - List every saved version of an ABAC policy, newest first.</summary>
        /// <remarks>Needs the <c>abac:read</c> scope.</remarks>
        public Task<JsonElement> VersionsAsync(string id, long? limit = null, string? startingAfter = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/abac_policies/{Enc(id)}/versions", query: Q(("limit", limit), ("starting_after", startingAfter)), cancellationToken: ct);

        /// <summary><c>POST /v1/abac_policies/{id}/test</c> - Run test cases against a policy version (default: the current one), or against a draft `source` and `tests` sent in the body, without saving anything. Each test is a request plus the expected decision; the policy is evaluated on its own, not with the rest of the set. The response also carries `coverage`: which rules and `when`/`unless` clauses the tests exercised (a clause no test saw both true and false is listed as a gap), with line numbers.</summary>
        /// <remarks>Needs the <c>abac:read</c> scope.</remarks>
        public Task<JsonElement> TestAsync(string id, AbacTestBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/abac_policies/{Enc(id)}/test", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/abac/evaluate</c> - Policy decision point: ask whether `principal` may perform `action` on `resource` given `context` (and optional extra `entities` for hierarchy). All enabled policies are evaluated together with Cedar semantics: a matching forbid wins, otherwise a matching permit allows, otherwise deny. Each decision is written to the event feed (type authz.decision), so log streams carry it.</summary>
        /// <remarks>Needs the <c>abac:evaluate</c> scope.</remarks>
        public Task<JsonElement> EvaluateAsync(AbacEvaluateBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/abac/evaluate", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/abac/partial_evaluate</c> - Partial evaluation (data filtering): ask what is left of the decision when an entity is unknown. With a known principal, an action and a resource TYPE, the response is a residual `filter` over the resource attributes (or constant `true`/`false`) that is equivalent to evaluating the policies on each resource. `unknowns: ["principal"]` answers "who may do this on that resource". Relationship conditions (`fga.check`) are resolved to id lists from the policy’s FGA store unless `resolve_relations` is false. `output` asks for a ready-made parameterised SQL WHERE (`sql`: dialect and a field-to-column map) and/or a Prisma where object (`prisma`: a field map). Policies that build sets from unknown attributes cannot be expressed as a filter and are refused with a message naming the policy.</summary>
        /// <remarks>Needs the <c>abac:evaluate</c> scope.</remarks>
        public Task<JsonElement> PartialEvaluateAsync(AbacPartialEvaluateBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/abac/partial_evaluate", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/abac/accessible_resources</c> - List the resources of a type a principal may perform an action on, paged (`limit`, `starting_after`; the response carries `has_more` and `next_cursor`). Candidates are the Atlas resources of that type (their metadata are the attributes) or the `candidates` you send. The policies are reduced to one filter and applied to every candidate; `method` says whether that worked or each candidate was evaluated in full.</summary>
        /// <remarks>Needs the <c>abac:evaluate</c> scope.</remarks>
        public Task<JsonElement> AccessibleResourcesAsync(AbacAccessibleResourcesBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/abac/accessible_resources", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/abac_policies/{id}/diff</c> - Compare two versions of a policy: a line diff of the source and, unless `semantic=false`, the requests whose decision changes. The semantic part enumerates a bounded set of requests built from the attributes, groups and actions the policies test (from the entity schema when one is set), and lists those that come out differently, with newly-allowed and newly-denied counts per action. `exhaustive` says whether every request in the space was compared or the space was sampled.</summary>
        /// <remarks>Needs the <c>abac:read</c> scope.</remarks>
        public Task<JsonElement> DiffAsync(string id, long? from = null, long? to = null, bool? semantic = null, long? maxRequests = null, long? maxChanges = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Get, $"/v1/abac_policies/{Enc(id)}/diff", query: Q(("from", from), ("to", to), ("semantic", semantic), ("max_requests", maxRequests), ("max_changes", maxChanges)), cancellationToken: ct);

        /// <summary><c>POST /v1/abac/diff</c> - The same comparison for two policy sources you send (a draft against the saved version, or two templates), without saving anything.</summary>
        /// <remarks>Needs the <c>abac:read</c> scope.</remarks>
        public Task<JsonElement> DiffSourcesAsync(AbacDiffSourcesBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/abac/diff", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/authzen/access/v1/evaluation</c> - AuthZEN Access Evaluation: `{subject, resource, action, context}` -&gt; `{decision: boolean}`. Entity types map to PascalCase policy types.</summary>
        /// <remarks>Needs the <c>abac:evaluate</c> scope.</remarks>
        public Task<JsonElement> AuthzenEvaluationAsync(AbacAuthzenEvaluationBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/authzen/access/v1/evaluation", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/authzen/access/v1/evaluations</c> - AuthZEN Access Evaluations (batch, up to 100): shared defaults plus an `evaluations` array, with `options.evaluations_semantic` of execute_all (default), deny_on_first_deny or permit_on_first_permit.</summary>
        /// <remarks>Needs the <c>abac:evaluate</c> scope.</remarks>
        public Task<JsonElement> AuthzenEvaluationsAsync(AbacAuthzenEvaluationsBody? body = null, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/authzen/access/v1/evaluations", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/authzen/access/v1/search/subject</c> - AuthZEN Subject Search: which subjects of `subject.type` may perform `action` on `resource` (give the resource a `type` and `id`). Answers from the same ABAC policies as an evaluation, reduced once by partial evaluation and applied to the candidates Atlas knows: your users (type `user`, their public metadata as attributes) and the users in the FGA stores the policies consult. `page.limit` and `page.token` page the result; the response carries `page.next_token` (empty on the last page), `count` and `total`.</summary>
        /// <remarks>Needs the <c>abac:evaluate</c> scope.</remarks>
        public Task<JsonElement> AuthzenSearchSubjectAsync(AbacAuthzenSearchSubjectBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/authzen/access/v1/search/subject", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/authzen/access/v1/search/resource</c> - AuthZEN Resource Search: which resources of `resource.type` the `subject` may perform `action` on. Candidates are your synced Atlas resources of that type (their metadata are the attributes) and the objects of that type in the FGA stores the policies consult. Paged with `page.limit` and `page.token`.</summary>
        /// <remarks>Needs the <c>abac:evaluate</c> scope.</remarks>
        public Task<JsonElement> AuthzenSearchResourceAsync(AbacAuthzenSearchResourceBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/authzen/access/v1/search/resource", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/authzen/access/v1/search/action</c> - AuthZEN Action Search: which actions the `subject` may perform on the `resource`. The actions considered are the ones the policies name (and the entity schema declares), each evaluated in full; the result lists the permitted action names.</summary>
        /// <remarks>Needs the <c>abac:evaluate</c> scope.</remarks>
        public Task<JsonElement> AuthzenSearchActionAsync(AbacAuthzenSearchActionBody body, bool? dryRun = null, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, "/v1/authzen/access/v1/search/action", body, query: Q(("dry_run", dryRun)), idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
