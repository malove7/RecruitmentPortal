using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecruitmentPortal.Models.DTOs;

namespace RecruitmentPortal.Services
{
    public class OpenAIEmbeddingService : IOpenAIEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly OpenAIOptions _options;
        private readonly ILogger<OpenAIEmbeddingService> _logger;

        public OpenAIEmbeddingService(
            HttpClient httpClient,
            IOptions<OpenAIOptions> options,
            ILogger<OpenAIEmbeddingService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<List<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text cannot be null or whitespace when generating an embedding.", nameof(text));
            }

            var results = await GenerateEmbeddingsBatchAsync(new[] { text }, cancellationToken);
            if (results.Count == 0)
            {
                throw new InvalidOperationException("Failed to generate embedding: OpenAI returned no data.");
            }

            return results[0];
        }

        public async Task<List<List<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
        {
            var textList = texts?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (textList == null || textList.Count == 0)
            {
                return new List<List<float>>();
            }

            var apiKey = !string.IsNullOrWhiteSpace(_options.ApiKey)
                ? _options.ApiKey
                : Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is missing. Please configure 'OpenAI:ApiKey' in User Secrets or set the 'OPENAI_API_KEY' environment variable.");
            }

            var model = _options.EffectiveEmbeddingModel;
            var endpoint = !string.IsNullOrWhiteSpace(_options.EmbeddingsEndpoint)
                ? _options.EmbeddingsEndpoint
                : "https://api.openai.com/v1/embeddings";

            const int maxBatchSize = 64;
            var allEmbeddings = new List<List<float>>(textList.Count);

            for (int i = 0; i < textList.Count; i += maxBatchSize)
            {
                var batch = textList.Skip(i).Take(maxBatchSize).ToList();

                var requestBody = new
                {
                    model = model,
                    input = batch
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI Embeddings API error ({StatusCode}): {Response}", response.StatusCode, responseContent);
                    throw new HttpRequestException($"OpenAI Embeddings request failed with status {response.StatusCode}: {responseContent}");
                }

                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                if (!root.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException($"Invalid OpenAI embeddings response structure: {responseContent}");
                }

                var batchResults = new List<(int Index, List<float> Vector)>();

                foreach (var item in dataElement.EnumerateArray())
                {
                    int index = item.GetProperty("index").GetInt32();
                    var embeddingArray = item.GetProperty("embedding");

                    var vector = new List<float>();
                    foreach (var val in embeddingArray.EnumerateArray())
                    {
                        vector.Add(val.GetSingle());
                    }

                    batchResults.Add((index, vector));
                }

                allEmbeddings.AddRange(batchResults.OrderBy(b => b.Index).Select(b => b.Vector));
            }

            return allEmbeddings;
        }
    }
}
