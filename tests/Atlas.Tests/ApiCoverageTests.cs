using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Resources;
using Xunit;

namespace Atlas.Tests
{
    /// <summary>
    /// Walks every resource reachable from <see cref="AtlasClient"/>, calls every public
    /// <c>…Async</c> method with placeholder arguments against a mock handler, and checks
    /// that every request lands on a documented operation and every documented
    /// secret-key + platform-key operation (api-operations.txt) is reached.
    /// </summary>
    public class ApiCoverageTests
    {
        private sealed record Op(string Method, Regex Path, string Raw);

        private static List<Op> DocumentedOps()
        {
            using var stream = typeof(ApiCoverageTests).Assembly.GetManifestResourceStream("api-operations.txt")!;
            using var reader = new StreamReader(stream);
            var ops = new List<Op>();
            foreach (var raw in reader.ReadToEnd().Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var parts = line.Split(' ');
                var regex = "^" + Regex.Replace(parts[1], @"\{[^}]+\}", "[^/]+") + "$";
                ops.Add(new Op(parts[0], new Regex(regex), line));
            }
            return ops;
        }

        [Fact]
        public async Task EveryDocumentedOperation_IsReachable_AndEveryCall_IsDocumented()
        {
            var ops = DocumentedOps();
            var handler = MockHttpMessageHandler.Json("{}");
            using var client = new AtlasClient(new AtlasClientOptions
            {
                SecretKey = "sk_test_123",
                PlatformKey = "psk_test_123",
                ApiUrl = "https://api.test.example",
                HttpClient = new HttpClient(handler),
            });

            var hit = new HashSet<string>();
            var undocumented = new SortedSet<string>();
            var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
            var queue = new Queue<object>();
            foreach (var p in typeof(AtlasClient).GetProperties())
            {
                if (typeof(ResourceBase).IsAssignableFrom(p.PropertyType)) queue.Enqueue(p.GetValue(client)!);
            }

            var calls = 0;
            while (queue.Count > 0)
            {
                var res = queue.Dequeue();
                if (!seen.Add(res)) continue;
                foreach (var p in res.GetType().GetProperties())
                {
                    if (typeof(ResourceBase).IsAssignableFrom(p.PropertyType)) queue.Enqueue(p.GetValue(res)!);
                }
                foreach (var m in res.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (!m.Name.EndsWith("Async") || !typeof(Task).IsAssignableFrom(m.ReturnType)) continue;
                    var args = m.GetParameters().Select(Placeholder).ToArray();
                    var before = handler.Requests.Count;
                    try
                    {
                        await (Task)m.Invoke(res, args)!;
                    }
                    catch (Exception)
                    {
                        // A helper that validates its input may refuse a placeholder; coverage below decides.
                    }
                    calls++;
                    if (handler.Requests.Count == before) continue;
                    var req = handler.Requests[handler.Requests.Count - 1];
                    var matched = false;
                    foreach (var op in ops)
                    {
                        if (op.Method == req.Method && op.Path.IsMatch(req.Path))
                        {
                            hit.Add(op.Raw);
                            matched = true;
                        }
                    }
                    if (!matched && req.Path != "/v1/design/catalog")
                        undocumented.Add($"{req.Method} {req.Path} <- {res.GetType().Name}.{m.Name}");
                }
            }

            var missing = ops.Where(o => !hit.Contains(o.Raw)).Select(o => o.Raw).ToList();
            Assert.True(calls > 400, $"expected to exercise the whole surface, made {calls} calls");
            Assert.True(undocumented.Count == 0, "calls to undocumented routes: " + string.Join("; ", undocumented));
            Assert.True(missing.Count == 0, $"documented operations no method reaches ({missing.Count}): " + string.Join("; ", missing));
        }

        private static object? Placeholder(ParameterInfo p)
        {
            var t = p.ParameterType;
            if (t == typeof(CancellationToken)) return CancellationToken.None;
            if (t == typeof(string))
                return p.Name == "contentType" ? "image/png" : "x1";
            if (t == typeof(byte[])) return new byte[] { 1, 2, 3 };
            if (p.HasDefaultValue) return p.DefaultValue;
            var u = Nullable.GetUnderlyingType(t) ?? t;
            if (u == typeof(int)) return 1;
            if (u == typeof(long)) return 1L;
            if (u == typeof(double)) return 1.0;
            if (u == typeof(bool)) return true;
            if (t.IsInterface && typeof(IEnumerable).IsAssignableFrom(t)) return new List<string>();
            if (t.IsAbstract) return null;
            try
            {
                return Activator.CreateInstance(t);
            }
            catch
            {
                return null;
            }
        }
    }
}
