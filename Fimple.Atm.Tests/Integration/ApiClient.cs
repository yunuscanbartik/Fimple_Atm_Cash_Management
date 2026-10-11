using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fimple.Atm.Tests.Integration
{
    public class ApiClient
    {
        private const string ERROR_CODE_PROPERTY = "errorCode";

        public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
        };

        private readonly HttpClient _httpClient;

        public ApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<HttpResponseMessage> Get(string url, params (string Name, string Value)[] headers)
        {
            return Send(HttpMethod.Get, url, null, headers);
        }

        public Task<HttpResponseMessage> Post(string url, object? body = null, params (string Name, string Value)[] headers)
        {
            return Send(HttpMethod.Post, url, body, headers);
        }

        public Task<HttpResponseMessage> Put(string url, object? body = null, params (string Name, string Value)[] headers)
        {
            return Send(HttpMethod.Put, url, body, headers);
        }

        public static async Task<T> Read<T>(HttpResponseMessage response)
        {
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
            return result ?? throw new InvalidOperationException($"The response body could not be read as {typeof(T).Name}.");
        }

        public static async Task<string> ErrorCode(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty(ERROR_CODE_PROPERTY, out var errorCode)
                ? errorCode.GetString() ?? string.Empty
                : string.Empty;
        }

        public static async Task EnsureSuccess(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;

            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Expected a success status but got {(int)response.StatusCode}: {body}");
        }

        private async Task<HttpResponseMessage> Send(HttpMethod method, string url, object? body, (string Name, string Value)[] headers)
        {
            using var request = new HttpRequestMessage(method, url);
            if (body != null)
            {
                request.Content = JsonContent.Create(body, options: JsonOptions);
            }

            foreach (var (name, value) in headers)
            {
                request.Headers.Add(name, value);
            }

            return await _httpClient.SendAsync(request);
        }
    }
}
