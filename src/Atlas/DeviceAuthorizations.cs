using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas
{
    /// <summary>A refused device-approval request; <see cref="Code"/> is the server's stable error code.</summary>
    public sealed class DeviceAuthorizationException : Exception
    {
        public DeviceAuthorizationException(int status, string code, string message, string? param = null, string? reason = null) : base(message)
        {
            Status = status;
            Code = code;
            Param = param;
            Reason = reason;
        }

        public int Status { get; }
        public string Code { get; }
        public string? Param { get; }
        public string? Reason { get; }
        /// <summary>Approving needs a fresh re-authentication first.</summary>
        public bool NeedsReverification => Code == "STEP_UP_REQUIRED";
        /// <summary>The code is unknown, expired or already used.</summary>
        public bool IsInvalidCode => Status == 404 || Status == 409;
    }

    /// <summary>
    /// Device sign-in approval (OAuth 2.0 Device Authorization Grant, RFC 8628) from your own app: the signed-in user
    /// types — or scans — the code a TV, console or CLI shows, and your app approves it instead of sending them to
    /// Atlas's hosted <c>/oauth2/device</c> page. These calls run on the Frontend API as the signed-in USER: they need
    /// the user's session token (a native app's session JWT), never a secret key, PAT or OAuth token.
    /// </summary>
    public sealed class DeviceAuthorizations
    {
        private const string Base = "/v1/client/me/device_authorizations";
        private readonly string _base;
        private readonly string _sessionToken;
        private readonly string? _publishableKey;
        private readonly HttpClient _http;

        public DeviceAuthorizations(string frontendApi, string sessionToken, string? publishableKey = null, HttpClient? httpClient = null)
        {
            _base = frontendApi.TrimEnd('/');
            _sessionToken = sessionToken;
            _publishableKey = publishableKey;
            _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        }

        /// <summary>Normalise what a person typed into the <c>XXXX-XXXX</c> form devices show.</summary>
        public static string FormatUserCode(string value)
        {
            var cleaned = Regex.Replace(value.ToUpperInvariant(), "[^A-Z0-9]", "");
            return cleaned.Length == 8 ? cleaned.Substring(0, 4) + "-" + cleaned.Substring(4) : value.Trim();
        }

        /// <summary>The <c>user_code</c> of a verification URL the device displayed, formatted; null when none.</summary>
        public static string? UserCodeFromUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var u) || string.IsNullOrEmpty(u.Query)) return null;
            foreach (var pair in u.Query.TrimStart('?').Split('&'))
            {
                var eq = pair.IndexOf('=');
                if (eq < 0) continue;
                if (Uri.UnescapeDataString(pair.Substring(0, eq)) != "user_code") continue;
                var v = Uri.UnescapeDataString(pair.Substring(eq + 1).Replace('+', ' '));
                return v.Length == 0 ? null : FormatUserCode(v);
            }
            return null;
        }

        /// <summary>Find a pending device sign-in by the code the device shows.</summary>
        public Task<JsonElement> LookupAsync(string userCode, CancellationToken ct = default)
            => CallAsync(Base + "/lookup", FormatUserCode(userCode), ct);

        /// <summary>Approve for the signed-in user; the device's next poll receives its tokens.</summary>
        public Task<JsonElement> ApproveAsync(JsonElement authorization, CancellationToken ct = default)
            => CallAsync(Base + "/" + Uri.EscapeDataString(authorization.GetProperty("id").GetString()!) + "/approve",
                authorization.GetProperty("user_code").GetString()!, ct);

        /// <summary>Deny; the device's next poll is told <c>access_denied</c>.</summary>
        public Task<JsonElement> DenyAsync(JsonElement authorization, CancellationToken ct = default)
            => CallAsync(Base + "/" + Uri.EscapeDataString(authorization.GetProperty("id").GetString()!) + "/deny",
                authorization.GetProperty("user_code").GetString()!, ct);

        private async Task<JsonElement> CallAsync(string path, string userCode, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, _base + path)
            {
                Content = new StringContent(JsonSerializer.Serialize(new Dictionary<string, string> { ["user_code"] = userCode }), Encoding.UTF8, "application/json"),
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sessionToken);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (_publishableKey != null) req.Headers.TryAddWithoutValidation("x-publishable-key", _publishableKey);
            using var res = await _http.SendAsync(req, ct).ConfigureAwait(false);
            var text = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            JsonElement data = default;
            try
            {
                using var doc = JsonDocument.Parse(string.IsNullOrEmpty(text) ? "{}" : text);
                data = doc.RootElement.Clone();
            }
            catch (JsonException) { }
            var status = (int)res.StatusCode;
            if (status < 200 || status > 299)
            {
                string? Field(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                var first = default(JsonElement);
                if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Array && errs.GetArrayLength() > 0)
                    first = errs[0];
                throw new DeviceAuthorizationException(status, Field(first, "code") ?? "UNKNOWN", Field(first, "message") ?? "The request failed.", Field(first, "param"), Field(first, "reason"));
            }
            return data;
        }
    }
}
