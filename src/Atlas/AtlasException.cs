using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Atlas
{
    /// <summary>
    /// One entry of the BAPI error envelope. Atlas answers a failed call with the
    /// §9.1 shape <c>{ "errors": [ { "code", "message", "param?", "meta?" } ] }</c>.
    /// <see cref="Code"/> is the stable, machine-readable part integrators branch
    /// on (<c>LAST_ADMIN</c>, <c>NOT_FOUND</c>, <c>SCOPE_MISSING</c>, …).
    /// </summary>
    public sealed class AtlasErrorItem
    {
        [JsonPropertyName("code")]
        public string Code { get; init; } = "UNKNOWN";

        [JsonPropertyName("message")]
        public string Message { get; init; } = "";

        [JsonPropertyName("param")]
        public string? Param { get; init; }

        [JsonPropertyName("meta")]
        public Dictionary<string, object?>? Meta { get; init; }
    }

    /// <summary>Internal wrapper used to parse the <c>{ errors: [...] }</c> envelope.</summary>
    internal sealed class AtlasErrorEnvelope
    {
        [JsonPropertyName("errors")]
        public List<AtlasErrorItem>? Errors { get; init; }
    }

    /// <summary>
    /// Thrown on any non-2xx response from the Backend API. Carries the HTTP
    /// <see cref="Status"/> and the full parsed <see cref="Errors"/> envelope.
    /// Branch on <see cref="Code"/> (the first error's stable code) or
    /// <see cref="HasCode"/>. Status-specific subclasses
    /// (<see cref="AtlasAuthenticationException"/>, <see cref="AtlasNotFoundException"/>,
    /// <see cref="AtlasRateLimitException"/>, …) let callers catch by category.
    /// </summary>
    public class AtlasException : Exception
    {
        /// <summary>HTTP status of the failed response.</summary>
        public int Status { get; }

        /// <summary>The full §9.1 error envelope, in order.</summary>
        public IReadOnlyList<AtlasErrorItem> Errors { get; }

        public AtlasException(int status, IReadOnlyList<AtlasErrorItem> errors, string? message = null)
            : base(message ?? errors.FirstOrDefault()?.Message ?? $"Atlas API request failed with status {status}")
        {
            Status = status;
            Errors = errors;
        }

        /// <summary>The first error's stable code, the field callers branch on most.</summary>
        public string? Code => Errors.Count > 0 ? Errors[0].Code : null;

        /// <summary>True when any error in the envelope carries the given stable code.</summary>
        public bool HasCode(string code) => Errors.Any(e => e.Code == code);

        /// <summary>Build the right subclass for a status + parsed envelope.</summary>
        public static AtlasException FromStatus(int status, IReadOnlyList<AtlasErrorItem> errors)
        {
            switch (status)
            {
                case 400:
                case 422:
                    return new AtlasBadRequestException(status, errors);
                case 401:
                    return new AtlasAuthenticationException(status, errors);
                case 403:
                    return new AtlasAuthorizationException(status, errors);
                case 404:
                    return new AtlasNotFoundException(status, errors);
                case 409:
                    return new AtlasConflictException(status, errors);
                case 429:
                    return new AtlasRateLimitException(status, errors);
                default:
                    if (status >= 500) return new AtlasServerException(status, errors);
                    return new AtlasException(status, errors);
            }
        }
    }

    /// <summary>400 / 422 — the request was rejected as invalid.</summary>
    public sealed class AtlasBadRequestException : AtlasException
    {
        public AtlasBadRequestException(int status, IReadOnlyList<AtlasErrorItem> errors) : base(status, errors) { }
    }

    /// <summary>401 — the secret key was missing, malformed, or rejected.</summary>
    public sealed class AtlasAuthenticationException : AtlasException
    {
        public AtlasAuthenticationException(int status, IReadOnlyList<AtlasErrorItem> errors) : base(status, errors) { }
    }

    /// <summary>403 — the key is valid but not permitted this operation.</summary>
    public sealed class AtlasAuthorizationException : AtlasException
    {
        public AtlasAuthorizationException(int status, IReadOnlyList<AtlasErrorItem> errors) : base(status, errors) { }
    }

    /// <summary>404 — the resource does not exist (or belongs to another instance).</summary>
    public sealed class AtlasNotFoundException : AtlasException
    {
        public AtlasNotFoundException(int status, IReadOnlyList<AtlasErrorItem> errors) : base(status, errors) { }
    }

    /// <summary>409 — a state conflict (e.g. <c>LAST_ADMIN</c>, a terminal request re-actioned).</summary>
    public sealed class AtlasConflictException : AtlasException
    {
        public AtlasConflictException(int status, IReadOnlyList<AtlasErrorItem> errors) : base(status, errors) { }
    }

    /// <summary>429 — rate limited. Inspect the first error's <c>meta.retry_after</c>.</summary>
    public sealed class AtlasRateLimitException : AtlasException
    {
        public AtlasRateLimitException(int status, IReadOnlyList<AtlasErrorItem> errors) : base(status, errors) { }

        /// <summary>Seconds to wait before retrying, when the server reports it in <c>meta</c>.</summary>
        public double? RetryAfter
        {
            get
            {
                if (Errors.Count == 0 || Errors[0].Meta == null) return null;
                if (Errors[0].Meta!.TryGetValue("retry_after", out var v) && v is System.Text.Json.JsonElement el
                    && el.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    return el.GetDouble();
                }
                return null;
            }
        }
    }

    /// <summary>5xx — the Backend API failed to serve the request.</summary>
    public sealed class AtlasServerException : AtlasException
    {
        public AtlasServerException(int status, IReadOnlyList<AtlasErrorItem> errors) : base(status, errors) { }
    }
}
