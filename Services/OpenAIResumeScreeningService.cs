using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecruitmentPortal.Models.DTOs;

namespace RecruitmentPortal.Services
{
    public class OpenAIResumeScreeningService : IAIResumeScreeningService
    {
        private readonly HttpClient _httpClient;
        private readonly OpenAIOptions _options;
        private readonly ILogger<OpenAIResumeScreeningService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public const string StrictSystemPrompt =
@"You are an AI Resume Screening Assistant supporting human recruiters.

Analyze the candidate resume strictly against the supplied Job Position.

Never invent skills, experience, achievements, qualifications or responsibilities.

Only use information explicitly supported by the resume as evidence.

If information is missing or unclear, mark it as missing, unclear or not demonstrated.

Evaluate all important job requirements and required skills.

For every skill return:
- score 0–10
- status
- confidence
- resume evidence
- gaps

Scoring:
0–2 = No evidence / major gap
3–4 = Limited evidence
5–6 = Moderate evidence
7–8 = Good match
9–10 = Strong match

Identify:
- strengths
- evidence
- concerns
- requirement gaps
- experience match

Generate specific recruiter follow-up questions for missing, weak, ambiguous or important information.

Prefer resume-specific questions instead of generic interview questions.

Example:

Resume says:
'Improved API performance.'

Ask:
'You mention improving API performance. What was the original issue, what changes did you make, and what measurable improvement did you achieve?'

Generate a concise recruiter summary covering:
- overall suitability
- strongest matches
- important evidence
- weaknesses/gaps
- areas to verify during interview

Do not infer or evaluate protected personal characteristics.

Do not make the final hiring decision.

Return only valid JSON matching the required response schema.";

        public OpenAIResumeScreeningService(
            HttpClient httpClient,
            IOptions<OpenAIOptions> options,
            ILogger<OpenAIResumeScreeningService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<AIScreeningServiceResult> ScreenResumeAsync(AIScreeningInput input, CancellationToken cancellationToken = default)
        {
            var apiKey = !string.IsNullOrWhiteSpace(_options.ApiKey)
                ? _options.ApiKey
                : Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("OpenAI API key is missing in configuration and environment variables.");
                return new AIScreeningServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "OpenAI API Key is not configured. Please configure 'OpenAI:ApiKey' in appsettings.json or set the 'OPENAI_API_KEY' environment variable.",
                    PromptVersion = _options.PromptVersion,
                    ModelUsed = _options.Model
                };
            }

            var model = !string.IsNullOrWhiteSpace(input.Model)
                ? input.Model
                : (!string.IsNullOrWhiteSpace(_options.Model) ? _options.Model : "gpt-4o-mini");
            var endpoint = string.IsNullOrWhiteSpace(_options.Endpoint) ? "https://api.openai.com/v1/chat/completions" : _options.Endpoint;

            var userPrompt = BuildUserPrompt(input);

            var requestBody = new Dictionary<string, object>
            {
                ["model"] = model,
                ["temperature"] = 0.1,
                ["max_completion_tokens"] = 2000,
                ["response_format"] = new { type = "json_object" },
                ["messages"] = new object[]
                {
                    new { role = "system", content = StrictSystemPrompt },
                    new { role = "user", content = userPrompt }
                }
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpRequest.Version = HttpVersion.Version20;
            httpRequest.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
            httpRequest.Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            // Log metadata safely WITHOUT logging full resume content
            _logger.LogInformation("Sending resume screening request for position '{JobTitle}', filename '{ResumeFileName}' ({ResumeLength} chars) using model '{Model}'.",
                input.JobTitle, input.ResumeFileName, input.ResumeText?.Length ?? 0, model);

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 90));

                using var response = await _httpClient.SendAsync(httpRequest, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI API call failed with status code {StatusCode}: {Response}", response.StatusCode, responseContent);

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return new AIScreeningServiceResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "OpenAI Authentication failed. Please verify that your configured OpenAI API Key is valid and active.",
                            ModelUsed = model,
                            PromptVersion = _options.PromptVersion
                        };
                    }

                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        return new AIScreeningServiceResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "OpenAI rate limit exceeded or quota exhausted. Please verify your OpenAI plan limits and try again shortly.",
                            ModelUsed = model,
                            PromptVersion = _options.PromptVersion
                        };
                    }

                    string errorSummary = ExtractOpenAIErrorMessage(responseContent) ?? $"OpenAI error (HTTP {(int)response.StatusCode})";
                    return new AIScreeningServiceResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"OpenAI screening request failed: {errorSummary}",
                        ModelUsed = model,
                        PromptVersion = _options.PromptVersion
                    };
                }

                // Parse OpenAI response payload
                var completionResult = JsonDocument.Parse(responseContent);
                var root = completionResult.RootElement;
                if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                {
                    return new AIScreeningServiceResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "OpenAI returned an empty completion response.",
                        ModelUsed = model,
                        PromptVersion = _options.PromptVersion
                    };
                }

                var content = choices[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
                var cleanedJson = CleanJsonString(content);

                var screeningDto = JsonSerializer.Deserialize<AIScreeningResponseDto>(cleanedJson, JsonOptions);
                if (screeningDto == null)
                {
                    return new AIScreeningServiceResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Failed to parse structured JSON screening output from OpenAI response.",
                        RawJsonResponse = content,
                        ModelUsed = model,
                        PromptVersion = _options.PromptVersion
                    };
                }

                _logger.LogInformation("Successfully completed AI screening for '{ResumeFileName}'. Overall score: {OverallScore}/100, Recommendation: '{Recommendation}'.",
                    input.ResumeFileName, screeningDto.OverallScore, screeningDto.Recommendation);

                return new AIScreeningServiceResult
                {
                    IsSuccess = true,
                    Data = screeningDto,
                    RawJsonResponse = cleanedJson,
                    ModelUsed = model,
                    PromptVersion = _options.PromptVersion
                };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError("OpenAI screening request timed out after {Timeout} seconds.", _options.TimeoutSeconds);
                return new AIScreeningServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"The AI screening request timed out after {_options.TimeoutSeconds} seconds. Please try again or check your network connection.",
                    ModelUsed = model,
                    PromptVersion = _options.PromptVersion
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during AI resume screening.");
                return new AIScreeningServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"An unexpected error occurred during AI screening: {ex.Message}",
                    ModelUsed = model,
                    PromptVersion = _options.PromptVersion
                };
            }
        }

        private static string BuildUserPrompt(AIScreeningInput input)
        {
            var resumeText = input.ResumeText ?? string.Empty;
            if (resumeText.Length > 16000)
            {
                resumeText = resumeText.Substring(0, 16000) + "\n\n[Note: Additional pages beyond character limit truncated for analysis efficiency]";
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== TARGET JOB POSITION ===");
            sb.AppendLine($"Job Title: {input.JobTitle}");
            sb.AppendLine();
            sb.AppendLine("Job Description:");
            sb.AppendLine(string.IsNullOrWhiteSpace(input.JobDescription) ? "Not specified" : input.JobDescription);
            sb.AppendLine();
            sb.AppendLine("Requirements & Skills:");
            sb.AppendLine(string.IsNullOrWhiteSpace(input.JobRequirements) ? "Not specified" : input.JobRequirements);
            sb.AppendLine();
            sb.AppendLine("=== CANDIDATE RESUME ===");
            sb.AppendLine($"Resume Filename: {input.ResumeFileName}");
            sb.AppendLine();
            sb.AppendLine("Extracted Resume Text:");
            sb.AppendLine(resumeText);
            sb.AppendLine();
            sb.AppendLine("=== REQUIRED JSON OUTPUT SCHEMA ===");
            sb.AppendLine("Respond strictly with a JSON object conforming to the following structure:");
            sb.AppendLine(@"{
  ""overallScore"": 82,
  ""recommendation"": ""Proceed to Technical Interview"",
  ""recruiterSummary"": ""Concise recruiter-friendly summary"",
  ""skills"": [
    {
      ""skill"": ""ASP.NET Core"",
      ""score"": 9,
      ""maxScore"": 10,
      ""status"": ""Strong Match"",
      ""confidence"": 0.95,
      ""evidence"": [""Developed REST APIs using ASP.NET Core""],
      ""gaps"": []
    }
  ],
  ""strengths"": [""Strong backend development experience""],
  ""evidenceExtracted"": [
    {
      ""category"": ""Technical"",
      ""evidence"": ""Built REST APIs using ASP.NET Core""
    }
  ],
  ""concerns"": [""Brief gap in employment during 2024""],
  ""gaps"": [""No mention of Docker experience required for the role""],
  ""experienceAnalysis"": {
    ""requiredExperience"": ""5+ years"",
    ""candidateExperience"": ""7 years"",
    ""status"": ""Meets Requirement"",
    ""comments"": ""Candidate meets required experience threshold""
  },
  ""followUpQuestions"": [
    {
      ""category"": ""Technical"",
      ""skill"": ""Azure"",
      ""priority"": ""High"",
      ""reason"": ""Azure responsibilities are unclear."",
      ""question"": ""Which Azure services have you worked with and what did you personally implement?""
    }
  ]
}");
            return sb.ToString();
        }

        private static string CleanJsonString(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            var trimmed = content.Trim();
            if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(7);
            }
            else if (trimmed.StartsWith("```"))
            {
                trimmed = trimmed.Substring(3);
            }

            if (trimmed.EndsWith("```"))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 3);
            }

            return trimmed.Trim();
        }

        private static string? ExtractOpenAIErrorMessage(string responseContent)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseContent);
                if (doc.RootElement.TryGetProperty("error", out var errorEl) &&
                    errorEl.TryGetProperty("message", out var msgEl))
                {
                    return msgEl.GetString();
                }
            }
            catch
            {
                // Fall back to null if parsing error JSON fails
            }

            return null;
        }
    }
}
