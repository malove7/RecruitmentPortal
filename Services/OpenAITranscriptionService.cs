using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecruitmentPortal.Models.DTOs;

namespace RecruitmentPortal.Services
{
    public class OpenAITranscriptionService : IOpenAITranscriptionService
    {
        private readonly HttpClient _httpClient;
        private readonly OpenAIOptions _options;
        private readonly ILogger<OpenAITranscriptionService> _logger;

        public OpenAITranscriptionService(
            HttpClient httpClient,
            IOptions<OpenAIOptions> options,
            ILogger<OpenAITranscriptionService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<TranscriptionResult> TranscribeAudioAsync(
            Stream audioStream,
            string fileName,
            string? contentType = null,
            CancellationToken cancellationToken = default,
            string? prompt = null,
            string? language = null)
        {
            if (audioStream == null || audioStream.Length == 0)
            {
                return new TranscriptionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Audio stream is empty."
                };
            }

            var apiKey = !string.IsNullOrWhiteSpace(_options.ApiKey)
                ? _options.ApiKey
                : Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("OpenAI API key is missing for audio transcription.");
                return new TranscriptionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "OpenAI API key is missing. Please configure 'OpenAI:ApiKey' or set the 'OPENAI_API_KEY' environment variable."
                };
            }

            var endpoint = !string.IsNullOrWhiteSpace(_options.TranscriptionEndpoint)
                ? _options.TranscriptionEndpoint
                : "https://api.openai.com/v1/audio/transcriptions";

            var model = _options.EffectiveTranscriptionModel;

            try
            {
                using var form = new MultipartFormDataContent();

                var streamContent = new StreamContent(audioStream);
                if (!string.IsNullOrWhiteSpace(contentType))
                {
                    var mime = contentType.Split(';')[0].Trim();
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(mime);
                }
                else
                {
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
                }

                var safeFileName = fileName;
                if (!Path.HasExtension(safeFileName))
                {
                    safeFileName += ".webm";
                }

                form.Add(streamContent, "file", safeFileName);
                form.Add(new StringContent(model), "model");

                // Target language (e.g. "en") to avoid multilingual phonetic drift
                var effectiveLanguage = !string.IsNullOrWhiteSpace(language) ? language : _options.TranscriptionLanguage;
                if (!string.IsNullOrWhiteSpace(effectiveLanguage))
                {
                    form.Add(new StringContent(effectiveLanguage), "language");
                }

                // Temperature 0 for greedy decoding / maximum deterministic accuracy
                form.Add(new StringContent("0"), "temperature");

                // Context prompt to heavily bias Whisper toward domain vocabulary, company name & HR terms
                var effectivePrompt = !string.IsNullOrWhiteSpace(prompt) ? prompt : _options.TranscriptionPrompt;
                if (!string.IsNullOrWhiteSpace(effectivePrompt))
                {
                    form.Add(new StringContent(effectivePrompt), "prompt");
                }

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = form;

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI transcription failed with status code {StatusCode}: {Response}", response.StatusCode, responseContent);
                    return new TranscriptionResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Transcription request failed with status {response.StatusCode}."
                    };
                }

                using var doc = JsonDocument.Parse(responseContent);
                if (doc.RootElement.TryGetProperty("text", out var textProp))
                {
                    var rawTranscript = textProp.GetString()?.Trim() ?? string.Empty;
                    var normalizedTranscript = NormalizeTranscript(rawTranscript);

                    return new TranscriptionResult
                    {
                        IsSuccess = true,
                        Text = normalizedTranscript
                    };
                }

                return new TranscriptionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Unexpected transcription response format from OpenAI."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during audio transcription.");
                return new TranscriptionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Audio transcription error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Post-processes speech recognition text to normalize common phonetic confusions,
        /// company brand names, and HR terms.
        /// </summary>
        public static string NormalizeTranscript(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var cleaned = text.Trim();

            // Fix common phonetic misrecognitions of "Space Stem"
            cleaned = Regex.Replace(cleaned, @"\b(?:space\s+stem|spacestem|space\s+team|space\s+them|space\s+ten|space\s+time|spaceteam)\b", "Space Stem", RegexOptions.IgnoreCase);

            // Fix common phonetic misrecognitions of "leave policy" in HR/recruitment context
            cleaned = Regex.Replace(cleaned, @"\b(?:live\s+policy|life\s+policy|leaf\s+policy)\b", "leave policy", RegexOptions.IgnoreCase);

            // Fix "live balance" -> "leave balance"
            cleaned = Regex.Replace(cleaned, @"\b(?:live\s+balance)\b", "leave balance", RegexOptions.IgnoreCase);

            // Fix "live encashment" -> "leave encashment"
            cleaned = Regex.Replace(cleaned, @"\b(?:live\s+encashment)\b", "leave encashment", RegexOptions.IgnoreCase);

            // Fix "sick live" -> "sick leave"
            cleaned = Regex.Replace(cleaned, @"\b(?:sick\s+live)\b", "sick leave", RegexOptions.IgnoreCase);

            // Fix "annual live" -> "annual leave"
            cleaned = Regex.Replace(cleaned, @"\b(?:annual\s+live)\b", "annual leave", RegexOptions.IgnoreCase);

            // Fix "casual live" -> "casual leave"
            cleaned = Regex.Replace(cleaned, @"\b(?:casual\s+live)\b", "casual leave", RegexOptions.IgnoreCase);

            return cleaned;
        }
    }
}
