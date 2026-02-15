using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CashTracker.Tests.Support
{
    internal sealed record RecordedHttpRequest(HttpMethod Method, string Url, string Body);

    internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly System.Func<HttpRequestMessage, string, HttpResponseMessage>? _responder;

        public RecordingHttpMessageHandler(System.Func<HttpRequestMessage, string, HttpResponseMessage>? responder = null)
        {
            _responder = responder;
        }

        public List<RecordedHttpRequest> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(new RecordedHttpRequest(
                request.Method,
                request.RequestUri?.ToString() ?? string.Empty,
                body));

            if (_responder is not null)
                return _responder(request, body);

            return OkJson("{\"ok\":true,\"result\":{}}");
        }

        public string? GetLastFormFieldValue(string endpointContains, string fieldKey)
        {
            for (var i = Requests.Count - 1; i >= 0; i--)
            {
                var req = Requests[i];
                if (!req.Url.Contains(endpointContains, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                if (TryReadFormField(req.Body, fieldKey, out var value))
                    return value;

                return null;
            }

            return null;
        }

        public static bool TryReadFormField(string body, string key, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(body))
                return false;

            var parts = body.Split('&', System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2, System.StringSplitOptions.None);
                if (kv.Length == 0)
                    continue;

                var decodedKey = DecodeFormValue(kv[0]);
                if (!string.Equals(decodedKey, key, System.StringComparison.Ordinal))
                    continue;

                value = kv.Length > 1 ? DecodeFormValue(kv[1]) : string.Empty;
                return true;
            }

            return false;
        }

        private static string DecodeFormValue(string encoded)
        {
            return System.Uri.UnescapeDataString((encoded ?? string.Empty).Replace('+', ' '));
        }

        public static HttpResponseMessage OkJson(string json)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }
}
