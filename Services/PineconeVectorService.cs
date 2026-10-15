using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecruitmentPortal.Models.DTOs;

namespace RecruitmentPortal.Services
{
    public class PineconeVectorService : IPineconeVectorService
    {
        private readonly HttpClient _httpClient;
        private readonly PineconeOptions _options;
        private readonly ILogger<PineconeVectorService> _logger;

        public PineconeVectorService(
            HttpClient httpClient,
            IOptions<PineconeOptions> options,
            ILogger<PineconeVectorService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public string BuildVectorId(int knowledgeBaseId, int documentId, int chunkIndex)
        {
            return $"kb-{knowledgeBaseId}-doc-{documentId}-chunk-{chunkIndex}";
        }

        public string BuildNamespace(int knowledgeBaseId)
        {
            var prefix = !string.IsNullOrWhiteSpace(_options.NamespacePrefix) ? _options.NamespacePrefix : "kb";
            return $"{prefix}-{knowledgeBaseId}";
        }

        private (string ApiKey, string BaseHost) GetConfig()
        {
            var apiKey = !string.IsNullOrWhiteSpace(_options.ApiKey)
                ? _options.ApiKey
                : Environment.GetEnvironmentVariable("PINECONE_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Pinecone API key is missing. Please configure 'Pinecone:ApiKey' in User Secrets or set the 'PINECONE_API_KEY' environment variable.");
            }

            var host = !string.IsNullOrWhiteSpace(_options.Host)
                ? _options.Host.TrimEnd('/')
                : "https://recruitment-knowledgebase-6540nuo.svc.aped-4627-b74a.pinecone.io";

            return (apiKey, host);
        }

        public async Task UpsertVectorsAsync(string namespaceName, IEnumerable<PineconeVectorRecord> vectors, CancellationToken cancellationToken = default)
        {
            var vectorList = vectors?.ToList();
            if (vectorList == null || vectorList.Count == 0)
                return;

            var (apiKey, host) = GetConfig();
            var endpoint = $"{host}/vectors/upsert";

            const int batchSize = 50;
            for (int i = 0; i < vectorList.Count; i += batchSize)
            {
                var batch = vectorList.Skip(i).Take(batchSize).Select(v => new
                {
                    id = v.Id,
                    values = v.Values,
                    metadata = v.Metadata
                }).ToList();

                var body = new
                {
                    vectors = batch,
                    @namespace = namespaceName
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("Api-Key", apiKey);
                request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Pinecone upsert error ({StatusCode}): {Response}", response.StatusCode, responseContent);
                    throw new HttpRequestException($"Pinecone upsert failed with status {response.StatusCode}: {responseContent}");
                }

                _logger.LogInformation("Successfully upserted {Count} vectors to Pinecone namespace '{Namespace}'", batch.Count, namespaceName);
            }
        }

        public async Task DeleteVectorsAsync(string namespaceName, IEnumerable<string> vectorIds, CancellationToken cancellationToken = default)
        {
            var idList = vectorIds?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
            if (idList == null || idList.Count == 0)
                return;

            var (apiKey, host) = GetConfig();
            var endpoint = $"{host}/vectors/delete";

            const int batchSize = 100;
            for (int i = 0; i < idList.Count; i += batchSize)
            {
                var batch = idList.Skip(i).Take(batchSize).ToList();
                var body = new
                {
                    ids = batch,
                    @namespace = namespaceName
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("Api-Key", apiKey);
                request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Pinecone delete vectors returned status {StatusCode}: {Response}", response.StatusCode, responseContent);
                }
            }
        }

        public async Task DeleteNamespaceAsync(string namespaceName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                return;

            var (apiKey, host) = GetConfig();
            var endpoint = $"{host}/vectors/delete";

            var body = new
            {
                deleteAll = true,
                @namespace = namespaceName
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("Api-Key", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Pinecone delete namespace '{Namespace}' returned status {StatusCode}: {Response}",
                    namespaceName, response.StatusCode, responseContent);
            }
            else
            {
                _logger.LogInformation("Successfully deleted Pinecone namespace '{Namespace}'", namespaceName);
            }
        }

        public async Task<List<PineconeQueryResult>> QueryVectorsAsync(string namespaceName, List<float> vector, int topK = 5, CancellationToken cancellationToken = default)
        {
            if (vector == null || vector.Count == 0)
            {
                return new List<PineconeQueryResult>();
            }

            var (apiKey, host) = GetConfig();
            var endpoint = $"{host}/query";

            var body = new
            {
                @namespace = namespaceName,
                vector = vector,
                topK = topK,
                includeMetadata = true,
                includeValues = false
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("Api-Key", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Pinecone query error ({StatusCode}): {Response}", response.StatusCode, responseContent);
                throw new HttpRequestException($"Pinecone query failed with status {response.StatusCode}: {responseContent}");
            }

            var results = new List<PineconeQueryResult>();
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("matches", out var matches) && matches.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in matches.EnumerateArray())
                {
                    var id = item.GetProperty("id").GetString() ?? string.Empty;
                    var score = item.TryGetProperty("score", out var scoreEl) ? scoreEl.GetDouble() : 0.0;

                    var metadataDict = new Dictionary<string, object>();
                    if (item.TryGetProperty("metadata", out var metaEl) && metaEl.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in metaEl.EnumerateObject())
                        {
                            switch (prop.Value.ValueKind)
                            {
                                case JsonValueKind.String:
                                    metadataDict[prop.Name] = prop.Value.GetString() ?? string.Empty;
                                    break;
                                case JsonValueKind.Number:
                                    if (prop.Value.TryGetInt32(out int intVal))
                                        metadataDict[prop.Name] = intVal;
                                    else
                                        metadataDict[prop.Name] = prop.Value.GetDouble();
                                    break;
                                case JsonValueKind.True:
                                case JsonValueKind.False:
                                    metadataDict[prop.Name] = prop.Value.GetBoolean();
                                    break;
                                default:
                                    metadataDict[prop.Name] = prop.Value.ToString();
                                    break;
                            }
                        }
                    }

                    results.Add(new PineconeQueryResult
                    {
                        Id = id,
                        Score = score,
                        Metadata = metadataDict
                    });
                }
            }

            return results;
        }
    }
}
