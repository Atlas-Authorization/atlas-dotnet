using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Atlas.Resources
{
    public sealed class OpenFgaAuthorizationModelBody
    {
        public List<Dictionary<string, object?>?>? TypeDefinitions { get; set; }
        public string? SchemaVersion { get; set; }
        public string? Dsl { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public OpenFgaAuthorizationModelBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class OpenFgaTupleKey
    {
        /// <summary>e.g. user:alice</summary>
        public string? User { get; set; }
        /// <summary>e.g. viewer</summary>
        public string? Relation { get; set; }
        /// <summary>e.g. document:roadmap</summary>
        [JsonPropertyName("object")] public string? ObjectType { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public OpenFgaTupleKey Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class OpenFgaCheckBody
    {
        public OpenFgaTupleKey? TupleKey { get; set; }
        public string? AuthorizationModelId { get; set; }
        /// <summary>OpenFGA contextual tuples.</summary>
        public Dictionary<string, object?>? ContextualTuples { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public OpenFgaCheckBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class OpenFgaBatchCheckItem
    {
        public OpenFgaTupleKey? TupleKey { get; set; }
        public string? CorrelationId { get; set; }
        public Dictionary<string, object?>? ContextualTuples { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public OpenFgaBatchCheckItem Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class OpenFgaBatchCheckBody
    {
        public List<OpenFgaBatchCheckItem>? Checks { get; set; }
        public string? AuthorizationModelId { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public OpenFgaBatchCheckBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class OpenFgaTupleKeys
    {
        public List<OpenFgaTupleKey>? TupleKeys { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public OpenFgaTupleKeys Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class OpenFgaWriteBody
    {
        public OpenFgaTupleKeys? Writes { get; set; }
        public OpenFgaTupleKeys? Deletes { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public OpenFgaWriteBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class OpenFgaReadBody
    {
        public OpenFgaTupleKey? TupleKey { get; set; }
        public long? PageSize { get; set; }
        public string? ContinuationToken { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public OpenFgaReadBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class OpenFgaListObjectsBody
    {
        /// <summary>e.g. document</summary>
        public string? Type { get; set; }
        /// <summary>e.g. viewer</summary>
        public string? Relation { get; set; }
        /// <summary>e.g. user:alice</summary>
        public string? User { get; set; }
        public string? AuthorizationModelId { get; set; }
        /// <summary>OpenFGA contextual tuples.</summary>
        public Dictionary<string, object?>? ContextualTuples { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public OpenFgaListObjectsBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class OpenFgaExpandTupleKey
    {
        [JsonPropertyName("object")] public string? ObjectType { get; set; }
        public string? Relation { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public OpenFgaExpandTupleKey Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class OpenFgaExpandBody
    {
        public OpenFgaExpandTupleKey? TupleKey { get; set; }
        public string? AuthorizationModelId { get; set; }

        /// <summary>Fields set with <see cref="Set"/>, written as-is (an explicit null clears a stored value on a partial update).</summary>
        [JsonExtensionData] public Dictionary<string, object?>? AdditionalFields { get; set; }

        /// <summary>Set any field by its wire name — including an explicit <c>null</c>, or a field this SDK does not model yet.</summary>
        public OpenFgaExpandBody Set(string key, object? value)
        {
            (AdditionalFields ??= new Dictionary<string, object?>())[key] = value;
            return this;
        }
    }

    public sealed class OpenFgaStore
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string CreatedAt { get; init; } = "";
        public string UpdatedAt { get; init; } = "";
    }

    public sealed class OpenFgaStoreList
    {
        public List<OpenFgaStore> Stores { get; init; } = new List<OpenFgaStore>();
    }

    public sealed class OpenFgaWriteModelResult
    {
        public string AuthorizationModelId { get; init; } = "";
    }

    public sealed class OpenFgaAuthorizationModel
    {
        public string Id { get; init; } = "";
        public string SchemaVersion { get; init; } = "";
    }

    public sealed class OpenFgaAuthorizationModelList
    {
        public List<OpenFgaAuthorizationModel> AuthorizationModels { get; init; } = new List<OpenFgaAuthorizationModel>();
    }

    public sealed class OpenFgaReadModelResult
    {
        public OpenFgaAuthorizationModel? AuthorizationModel { get; init; }
    }

    public sealed class OpenFgaCheckResult
    {
        public bool? Allowed { get; init; }
    }

    public sealed class OpenFgaBatchCheckResult
    {
        public Dictionary<string, OpenFgaCheckResult?>? Result { get; init; }
    }

    public sealed class OpenFgaTuple
    {
        public OpenFgaTupleKey? Key { get; init; }
        public string? Timestamp { get; init; }
    }

    public sealed class OpenFgaReadResult
    {
        public List<OpenFgaTuple>? Tuples { get; init; }
        public string? ContinuationToken { get; init; }
    }

    public sealed class OpenFgaListObjectsResult
    {
        public List<string>? Objects { get; init; }
    }

    public sealed class OpenFgaExpandResultTree
    {
        public Dictionary<string, object?>? Root { get; init; }
    }

    public sealed class OpenFgaExpandResult
    {
        public OpenFgaExpandResultTree? Tree { get; init; }
    }

    /// <summary>The OpenFGA-compatible authorization API (<c>/v1/openfga</c>).</summary>
    public sealed class OpenFgaResource : ResourceBase
    {
        public OpenFgaResource(AtlasTransport transport) : base(transport)
        {
        }

        /// <summary><c>GET /v1/openfga/stores</c> — List the instance's OpenFGA (ReBAC) authorization stores.</summary>
        public Task<OpenFgaStoreList> ListStoresAsync(CancellationToken ct = default)
            => Req<OpenFgaStoreList>(HttpVerb.Get, "/v1/openfga/stores", cancellationToken: ct);

        /// <summary><c>POST /v1/openfga/stores</c> — Create an OpenFGA authorization store.</summary>
        public Task<OpenFgaStore> CreateStoreAsync(string? name, CancellationToken ct = default)
            => Req<OpenFgaStore>(HttpVerb.Post, "/v1/openfga/stores", Body(("name", name)), cancellationToken: ct);

        /// <summary><c>GET /v1/openfga/stores/{store}</c> — Get one OpenFGA store (OpenFGA-wire GetStore).</summary>
        public Task<OpenFgaStore> GetStoreAsync(string store, CancellationToken ct = default)
            => Req<OpenFgaStore>(HttpVerb.Get, $"/v1/openfga/stores/{Enc(store)}", cancellationToken: ct);

        /// <summary><c>POST /v1/openfga/stores/{store}/authorization-models</c> — Write a new authorization model to a store.</summary>
        public Task<OpenFgaWriteModelResult> WriteAuthorizationModelAsync(string store, OpenFgaAuthorizationModelBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<OpenFgaWriteModelResult>(HttpVerb.Post, $"/v1/openfga/stores/{Enc(store)}/authorization-models", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>GET /v1/openfga/stores/{store}/authorization-models</c> — List the authorization models in a store.</summary>
        public Task<OpenFgaAuthorizationModelList> ListAuthorizationModelsAsync(string store, CancellationToken ct = default)
            => Req<OpenFgaAuthorizationModelList>(HttpVerb.Get, $"/v1/openfga/stores/{Enc(store)}/authorization-models", cancellationToken: ct);

        /// <summary><c>GET /v1/openfga/stores/{store}/authorization-models/{mid}</c> — Read one authorization model (OpenFGA-wire ReadAuthorizationModel).</summary>
        public Task<OpenFgaReadModelResult> ReadAuthorizationModelAsync(string store, string mid, CancellationToken ct = default)
            => Req<OpenFgaReadModelResult>(HttpVerb.Get, $"/v1/openfga/stores/{Enc(store)}/authorization-models/{Enc(mid)}", cancellationToken: ct);

        /// <summary><c>POST /v1/openfga/stores/{store}/check</c> — Check whether a user has a relation on an object (OpenFGA-wire check).</summary>
        public Task<OpenFgaCheckResult> CheckAsync(string store, OpenFgaCheckBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<OpenFgaCheckResult>(HttpVerb.Post, $"/v1/openfga/stores/{Enc(store)}/check", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/openfga/stores/{store}/batch-check</c> — Check up to 100 tuples in one request, keyed by correlation id (OpenFGA-wire BatchCheck).</summary>
        public Task<OpenFgaBatchCheckResult> BatchCheckAsync(string store, OpenFgaBatchCheckBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<OpenFgaBatchCheckResult>(HttpVerb.Post, $"/v1/openfga/stores/{Enc(store)}/batch-check", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/openfga/stores/{store}/write</c> — Write or delete relationship tuples in a store (OpenFGA-wire write).</summary>
        public Task<JsonElement> WriteAsync(string store, OpenFgaWriteBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<JsonElement>(HttpVerb.Post, $"/v1/openfga/stores/{Enc(store)}/write", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/openfga/stores/{store}/read</c> — Read relationship tuples, optionally filtered by a tuple key (OpenFGA-wire Read).</summary>
        public Task<OpenFgaReadResult> ReadAsync(string store, OpenFgaReadBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<OpenFgaReadResult>(HttpVerb.Post, $"/v1/openfga/stores/{Enc(store)}/read", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/openfga/stores/{store}/list-objects</c> — List the objects of a type a user has a relation on (OpenFGA-wire ListObjects).</summary>
        public Task<OpenFgaListObjectsResult> ListObjectsAsync(string store, OpenFgaListObjectsBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<OpenFgaListObjectsResult>(HttpVerb.Post, $"/v1/openfga/stores/{Enc(store)}/list-objects", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary><c>POST /v1/openfga/stores/{store}/expand</c> — Expand the userset tree for an (object, relation) (OpenFGA-wire Expand).</summary>
        public Task<OpenFgaExpandResult> ExpandAsync(string store, OpenFgaExpandBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<OpenFgaExpandResult>(HttpVerb.Post, $"/v1/openfga/stores/{Enc(store)}/expand", body, idempotencyKey: idempotencyKey, cancellationToken: ct);
    }
}
