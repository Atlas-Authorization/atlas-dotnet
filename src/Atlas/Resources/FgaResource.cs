using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class FgaStore
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "fga.store";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class FgaAuthorizationModel
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "fga.authorization_model";
        public string Id { get; init; } = "";
        public string StoreId { get; init; } = "";
        public string SchemaVersion { get; init; } = "";
        /// <summary>The OpenFGA model. Present on get/create; omitted from list rows.</summary>
        public Dictionary<string, object?>? Model { get; init; }
        public long CreatedAt { get; init; }
    }

    public sealed class FgaTuple
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "fga.tuple";
        /// <summary><c>type:id</c> or <c>type:id#relation</c> for a userset.</summary>
        public string User { get; init; } = "";
        public string Relation { get; init; } = "";
        /// <summary>The object of the tuple as <c>type:id</c>.</summary>
        public string Target { get; init; } = "";
        public long CreatedAt { get; init; }
    }

    /// <summary>A tuple key. <c>object</c> is the target as <c>type:id</c>.</summary>
    public sealed class FgaTupleKey
    {
        public string User { get; set; } = "";
        public string Relation { get; set; } = "";
        [JsonPropertyName("object")] public string Object { get; set; } = "";
    }

    /// <summary>An OpenFGA authorization model to persist. Validated before storage.</summary>
    public sealed class FgaAuthorizationModelInput
    {
        public List<object> TypeDefinitions { get; set; } = new List<object>();
        /// <summary>Defaults to <c>1.1</c> when omitted.</summary>
        public string? SchemaVersion { get; set; }
        public Dictionary<string, object?>? Conditions { get; set; }
    }

    public sealed class FgaCheckBody
    {
        public string User { get; set; } = "";
        public string Relation { get; set; } = "";
        [JsonPropertyName("object")] public string Object { get; set; } = "";
        /// <summary>Defaults to the store's latest model.</summary>
        public string? AuthorizationModelId { get; set; }
        public List<FgaTupleKey>? ContextualTuples { get; set; }
    }

    public sealed class FgaListObjectsBody
    {
        public string User { get; set; } = "";
        public string Relation { get; set; } = "";
        public string Type { get; set; } = "";
        public string? AuthorizationModelId { get; set; }
        public List<FgaTupleKey>? ContextualTuples { get; set; }
    }

    /// <summary>One item in a batch check; <c>correlation_id</c> is echoed back to match results.</summary>
    public sealed class FgaBatchCheckItem
    {
        public string User { get; set; } = "";
        public string Relation { get; set; } = "";
        [JsonPropertyName("object")] public string Object { get; set; } = "";
        public string? CorrelationId { get; set; }
        public List<FgaTupleKey>? ContextualTuples { get; set; }
    }

    public sealed class FgaBatchCheckResultItem
    {
        public string CorrelationId { get; init; } = "";
        public bool Allowed { get; init; }
    }

    public sealed class FgaBatchCheckResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "fga.batch_check";
        public string AuthorizationModelId { get; init; } = "";
        public List<FgaBatchCheckResultItem> Result { get; init; } = new List<FgaBatchCheckResultItem>();
    }

    public sealed class FgaWriteResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "fga.write_result";
        public int Writes { get; init; }
        public int Deletes { get; init; }
    }

    public sealed class FgaCheckResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "fga.check";
        public bool Allowed { get; init; }
        public string AuthorizationModelId { get; init; } = "";
    }

    public sealed class FgaListObjectsResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "fga.list_objects";
        public List<string> Objects { get; init; } = new List<string>();
        public string AuthorizationModelId { get; init; } = "";
    }

    public sealed class FgaExpandResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "fga.expand";
        public object? Tree { get; init; }
        public string AuthorizationModelId { get; init; } = "";
    }

    public sealed class DeletedFgaStore
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "fga.store";
        public string Id { get; init; } = "";
        public bool Deleted { get; init; }
    }

    public sealed class FgaWriteBody
    {
        public List<FgaTupleKey>? Writes { get; set; }
        public List<FgaTupleKey>? Deletes { get; set; }
    }

    public sealed class FgaReadBody
    {
        public string? User { get; set; }
        public string? Relation { get; set; }
        [JsonPropertyName("object")] public string? Object { get; set; }
    }

    public sealed class FgaBatchCheckBody
    {
        public List<FgaBatchCheckItem> Checks { get; set; } = new List<FgaBatchCheckItem>();
        public string? AuthorizationModelId { get; set; }
    }

    public sealed class FgaExpandBody
    {
        [JsonPropertyName("object")] public string Object { get; set; } = "";
        public string Relation { get; set; } = "";
        public string? AuthorizationModelId { get; set; }
    }

    /// <summary>Nested FGA stores namespace.</summary>
    public sealed class FgaStoresResource : ResourceBase
    {
        public FgaStoresResource(AtlasTransport t) : base(t) { }

        public Task<ListPage<FgaStore>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<FgaStore>>(HttpVerb.Get, "/v1/fga/stores", cancellationToken: ct);

        public Task<FgaStore> CreateAsync(string name, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<FgaStore>(HttpVerb.Post, "/v1/fga/stores", body: Body(("name", name)), idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<FgaStore> GetAsync(string id, CancellationToken ct = default)
            => Req<FgaStore>(HttpVerb.Get, $"/v1/fga/stores/{Enc(id)}", cancellationToken: ct);

        public Task<DeletedFgaStore> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedFgaStore>(HttpVerb.Delete, $"/v1/fga/stores/{Enc(id)}", cancellationToken: ct);
    }

    /// <summary>Nested FGA authorization-models namespace.</summary>
    public sealed class FgaModelsResource : ResourceBase
    {
        public FgaModelsResource(AtlasTransport t) : base(t) { }

        public Task<ListPage<FgaAuthorizationModel>> ListAsync(string storeId, CancellationToken ct = default)
            => Req<ListPage<FgaAuthorizationModel>>(HttpVerb.Get, $"/v1/fga/stores/{Enc(storeId)}/authorization-models", cancellationToken: ct);

        public Task<FgaAuthorizationModel> CreateAsync(string storeId, FgaAuthorizationModelInput body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<FgaAuthorizationModel>(HttpVerb.Post, $"/v1/fga/stores/{Enc(storeId)}/authorization-models", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<FgaAuthorizationModel> GetAsync(string storeId, string modelId, CancellationToken ct = default)
            => Req<FgaAuthorizationModel>(HttpVerb.Get, $"/v1/fga/stores/{Enc(storeId)}/authorization-models/{Enc(modelId)}", cancellationToken: ct);
    }

    /// <summary>
    /// Fine-grained relationship-based authorization (Zanzibar / OpenFGA). The
    /// <c>/v1/fga</c> namespace: stores, models, and the tuple/query operations.
    /// Call <see cref="Store"/> to bind a default store and drop the repeated
    /// <c>storeId</c> argument.
    /// </summary>
    public sealed class FgaResource : ResourceBase
    {
        public FgaResource(AtlasTransport transport) : base(transport)
        {
            Stores = new FgaStoresResource(transport);
            Models = new FgaModelsResource(transport);
        }

        public FgaStoresResource Stores { get; }
        public FgaModelsResource Models { get; }

        /// <summary>Write and/or delete tuples in one atomic call.</summary>
        public Task<FgaWriteResult> WriteAsync(string storeId, FgaWriteBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<FgaWriteResult>(HttpVerb.Post, $"/v1/fga/stores/{Enc(storeId)}/write", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        /// <summary>Query stored tuples by any of user / relation / object.</summary>
        public Task<ListPage<FgaTuple>> ReadAsync(string storeId, FgaReadBody body, CancellationToken ct = default)
            => Req<ListPage<FgaTuple>>(HttpVerb.Post, $"/v1/fga/stores/{Enc(storeId)}/read", body, cancellationToken: ct);

        /// <summary>Resolve a single access question against the model.</summary>
        public Task<FgaCheckResult> CheckAsync(string storeId, FgaCheckBody body, CancellationToken ct = default)
            => Req<FgaCheckResult>(HttpVerb.Post, $"/v1/fga/stores/{Enc(storeId)}/check", body, cancellationToken: ct);

        /// <summary>Resolve MANY access questions in one round trip (list-authorization UIs).</summary>
        public Task<FgaBatchCheckResult> BatchCheckAsync(string storeId, FgaBatchCheckBody body, CancellationToken ct = default)
            => Req<FgaBatchCheckResult>(HttpVerb.Post, $"/v1/fga/stores/{Enc(storeId)}/batch-check", body, cancellationToken: ct);

        /// <summary>List the objects of a type a user has a relation to.</summary>
        public Task<FgaListObjectsResult> ListObjectsAsync(string storeId, FgaListObjectsBody body, CancellationToken ct = default)
            => Req<FgaListObjectsResult>(HttpVerb.Post, $"/v1/fga/stores/{Enc(storeId)}/list-objects", body, cancellationToken: ct);

        /// <summary>Expand the full userset tree for an object#relation.</summary>
        public Task<FgaExpandResult> ExpandAsync(string storeId, FgaExpandBody body, CancellationToken ct = default)
            => Req<FgaExpandResult>(HttpVerb.Post, $"/v1/fga/stores/{Enc(storeId)}/expand", body, cancellationToken: ct);

        /// <summary>
        /// Bind a default store so an app that uses one store (the common case) can
        /// call <c>fga.Store(id).CheckAsync(...)</c> instead of threading the store
        /// id through every call.
        /// </summary>
        public FgaScopedStore Store(string storeId) => new FgaScopedStore(this, storeId);
    }

    /// <summary>A <see cref="FgaResource"/> with <c>storeId</c> pre-applied.</summary>
    public sealed class FgaScopedStore
    {
        private readonly FgaResource _fga;
        private readonly string _storeId;

        public FgaScopedStore(FgaResource fga, string storeId)
        {
            _fga = fga;
            _storeId = storeId;
        }

        public Task<FgaWriteResult> WriteAsync(FgaWriteBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => _fga.WriteAsync(_storeId, body, idempotencyKey, ct);

        public Task<ListPage<FgaTuple>> ReadAsync(FgaReadBody body, CancellationToken ct = default)
            => _fga.ReadAsync(_storeId, body, ct);

        public Task<FgaCheckResult> CheckAsync(FgaCheckBody body, CancellationToken ct = default)
            => _fga.CheckAsync(_storeId, body, ct);

        public Task<FgaBatchCheckResult> BatchCheckAsync(FgaBatchCheckBody body, CancellationToken ct = default)
            => _fga.BatchCheckAsync(_storeId, body, ct);

        public Task<FgaListObjectsResult> ListObjectsAsync(FgaListObjectsBody body, CancellationToken ct = default)
            => _fga.ListObjectsAsync(_storeId, body, ct);

        public Task<FgaExpandResult> ExpandAsync(FgaExpandBody body, CancellationToken ct = default)
            => _fga.ExpandAsync(_storeId, body, ct);

        public Task<ListPage<FgaAuthorizationModel>> ListModelsAsync(CancellationToken ct = default)
            => _fga.Models.ListAsync(_storeId, ct);

        public Task<FgaAuthorizationModel> CreateModelAsync(FgaAuthorizationModelInput body, string? idempotencyKey = null, CancellationToken ct = default)
            => _fga.Models.CreateAsync(_storeId, body, idempotencyKey, ct);

        public Task<FgaAuthorizationModel> GetModelAsync(string modelId, CancellationToken ct = default)
            => _fga.Models.GetAsync(_storeId, modelId, ct);
    }
}
