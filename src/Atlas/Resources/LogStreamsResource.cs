using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Resources
{
    public sealed class LogStream
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "log_stream";
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Type { get; init; } = "";
        public bool Enabled { get; init; }
        public string Status { get; init; } = "";
        public List<string>? EventFilter { get; init; }
        /// <summary>Non-secret destination fields plus <c>has_*</c> markers; never a secret value.</summary>
        public Dictionary<string, object?> Destination { get; init; } = new Dictionary<string, object?>();
        public string? Cursor { get; init; }
        public int ConsecutiveFailures { get; init; }
        public string? LastError { get; init; }
        public long? LastDeliveredAt { get; init; }
        public long CreatedAt { get; init; }
        public long UpdatedAt { get; init; }
    }

    public sealed class CreateLogStreamBody
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        /// <summary>Shape depends on <c>type</c>: http {url,headers?}, datadog {site,api_key}, splunk {hec_endpoint,token}.</summary>
        public Dictionary<string, object?> Destination { get; set; } = new Dictionary<string, object?>();
        /// <summary>null (or omitted) forwards everything; an array of event-type prefixes filters.</summary>
        public List<string>? EventFilter { get; set; }
        public bool? Enabled { get; set; }
    }

    /// <summary>PATCH accepts everything create does except <c>type</c>, which is immutable.</summary>
    public sealed class UpdateLogStreamBody
    {
        public string? Name { get; set; }
        /// <summary>Merged over the stored destination; a secret left out is kept.</summary>
        public Dictionary<string, object?>? Destination { get; set; }
        public List<string>? EventFilter { get; set; }
        public bool? Enabled { get; set; }
        public string? Status { get; set; }
    }

    public sealed class LogStreamTestResult
    {
        [JsonPropertyName("object")] public string ObjectType { get; init; } = "log_stream_test_result";
        public string Id { get; init; } = "";
        public bool Ok { get; init; }
        public int? Status { get; init; }
        public string? Error { get; init; }
    }

    /// <summary>The log-streams namespace (<c>/v1/log_streams</c>).</summary>
    public sealed class LogStreamsResource : ResourceBase
    {
        public LogStreamsResource(AtlasTransport transport) : base(transport) { }

        public Task<ListPage<LogStream>> ListAsync(CancellationToken ct = default)
            => Req<ListPage<LogStream>>(HttpVerb.Get, "/v1/log_streams", cancellationToken: ct);

        public Task<LogStream> GetAsync(string id, CancellationToken ct = default)
            => Req<LogStream>(HttpVerb.Get, $"/v1/log_streams/{Enc(id)}", cancellationToken: ct);

        public Task<LogStream> CreateAsync(CreateLogStreamBody body, string? idempotencyKey = null, CancellationToken ct = default)
            => Req<LogStream>(HttpVerb.Post, "/v1/log_streams", body, idempotencyKey: idempotencyKey, cancellationToken: ct);

        public Task<LogStream> UpdateAsync(string id, UpdateLogStreamBody body, CancellationToken ct = default)
            => Req<LogStream>(HttpVerb.Patch, $"/v1/log_streams/{Enc(id)}", body, cancellationToken: ct);

        public Task<DeletedObject> DeleteAsync(string id, CancellationToken ct = default)
            => Req<DeletedObject>(HttpVerb.Delete, $"/v1/log_streams/{Enc(id)}", cancellationToken: ct);

        /// <summary>Send ONE synthetic event with the real credentials; the cursor is never touched.</summary>
        public Task<LogStreamTestResult> TestAsync(string id, CancellationToken ct = default)
            => Req<LogStreamTestResult>(HttpVerb.Post, $"/v1/log_streams/{Enc(id)}/test", cancellationToken: ct);
    }
}
