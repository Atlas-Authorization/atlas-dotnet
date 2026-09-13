using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class Action
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "action";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Trigger { get; init; } = "";
        public string Code { get; init; } = "";
        public string Runtime { get; init; } = "";
        public bool Enabled { get; init; }
        public List<string> SecretNames { get; init; } = new List<string>();
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class CreateActionBody
    {
        public string Name { get; set; } = "";
        public string Trigger { get; set; } = "";
        public string Code { get; set; } = "";
        public bool? Enabled { get; set; }
        /// <summary>Write-only, encrypted at rest, never read back.</summary>
        public Dictionary<string, string>? Secrets { get; set; }
    }

    public sealed class UpdateActionBody
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public bool? Enabled { get; set; }
        public Dictionary<string, string>? Secrets { get; set; }
    }

    public sealed class ActionTestEventUser
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        [JsonPropertyName("emailVerified")] public bool? EmailVerified { get; set; }
        public Dictionary<string, object?>? Metadata { get; set; }
    }

    public sealed class ActionTestEventConnection
    {
        public string? Strategy { get; set; }
    }

    public sealed class ActionTestEventRequest
    {
        public string? Ip { get; set; }
        [JsonPropertyName("user_agent")] public string? UserAgent { get; set; }
    }

    /// <summary>The sample event a dry-run runs against; every field has a stand-in default.</summary>
    public sealed class ActionTestEvent
    {
        public ActionTestEventUser? User { get; set; }
        public ActionTestEventConnection? Connection { get; set; }
        public ActionTestEventRequest? Request { get; set; }
    }

    public sealed class ActionTestResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "action_test_result";
        public string Id { get; init; } = "";
        public bool Denied { get; init; }
        public string? DenyReason { get; init; }
        public Dictionary<string, object?> AccessTokenClaims { get; init; } = new Dictionary<string, object?>();
        public Dictionary<string, object?> IdTokenClaims { get; init; } = new Dictionary<string, object?>();
        public Dictionary<string, object?> AppMetadata { get; init; } = new Dictionary<string, object?>();
        public List<string> Logs { get; init; } = new List<string>();
        public string? Error { get; init; }
    }

    /// <summary>The ordered binding list for a trigger, replaced atomically as a set.</summary>
    public sealed class ActionBindingList
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "action_binding_list";
        public string Trigger { get; init; } = "";
        public List<string> ActionIds { get; init; } = new List<string>();
    }

    /// <summary>The actions namespace (<c>/v1/actions</c>) — tenant code at auth-pipeline triggers.</summary>
    public sealed class ActionsResource : ResourceBase
    {
        public ActionsResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<Action>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<Action>>(HttpVerb.Get, "/v1/actions", cancellationToken: ct);

        public Task<Action> CreateAsync(CreateActionBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<Action>(HttpVerb.Post, "/v1/actions", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<Action> GetAsync(string id, CancellationToken ct = default)
            => Req<Action>(HttpVerb.Get, $"/v1/actions/{Enc(id)}", cancellationToken: ct);

        public Task<Action> UpdateAsync(string id, UpdateActionBody body, CancellationToken ct = default)
            => Req<Action>(HttpVerb.Patch, $"/v1/actions/{Enc(id)}", body, cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/actions/{Enc(id)}", cancellationToken: ct);

        /// <summary>Dry-run against a sample event in the sandbox; nothing is persisted.</summary>
        public Task<ActionTestResult> TestAsync(string id, ActionTestEvent? @event = null, CancellationToken ct = default)
            => Req<ActionTestResult>(HttpVerb.Post, $"/v1/actions/{Enc(id)}/test",
                body: Body(("event", (object?)(@event ?? new ActionTestEvent()))), cancellationToken: ct);

        /// <summary>The actions bound to a trigger, in run order.</summary>
        public Task<ActionBindingList> GetBindingsAsync(string trigger, CancellationToken ct = default)
            => Req<ActionBindingList>(HttpVerb.Get, $"/v1/actions/bindings/{Enc(trigger)}", cancellationToken: ct);

        /// <summary>Replace a trigger's ordered binding set.</summary>
        public Task<ActionBindingList> SetBindingsAsync(string trigger, IEnumerable<string> actionIds, CancellationToken ct = default)
            => Req<ActionBindingList>(HttpVerb.Put, $"/v1/actions/bindings/{Enc(trigger)}",
                body: Body(("action_ids", new List<string>(actionIds))), cancellationToken: ct);
    }
}
