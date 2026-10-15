using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RecruitmentPortal.Models.DTOs;
using RecruitmentPortal.Services;
using System.Net;
using System.Text;
using Xunit;

namespace RecruitmentPortal.Tests.Services
{
    public class OpenAITranscriptionServiceTests
    {
        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

            public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
            {
                _responseFactory = responseFactory;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_responseFactory(request));
            }
        }

        [Fact]
        public async Task TranscribeAudioAsync_WhenStreamIsEmpty_ShouldReturnFailure()
        {
            // Arrange
            var options = Options.Create(new OpenAIOptions { ApiKey = "dummy-key" });
            var httpClient = new HttpClient();
            var service = new OpenAITranscriptionService(httpClient, options, NullLogger<OpenAITranscriptionService>.Instance);
            using var emptyStream = new MemoryStream();

            // Act
            var result = await service.TranscribeAudioAsync(emptyStream, "test.webm", "audio/webm");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("empty");
        }

        [Fact]
        public async Task TranscribeAudioAsync_WhenApiKeyIsMissing_ShouldReturnFailure()
        {
            // Arrange
            var prevEnv = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);

            try
            {
                var options = Options.Create(new OpenAIOptions { ApiKey = "" });
                var httpClient = new HttpClient();
                var service = new OpenAITranscriptionService(httpClient, options, NullLogger<OpenAITranscriptionService>.Instance);
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake audio"));

                // Act
                var result = await service.TranscribeAudioAsync(stream, "test.webm", "audio/webm");

                // Assert
                result.IsSuccess.Should().BeFalse();
                result.ErrorMessage.Should().Contain("OpenAI API key is missing");
            }
            finally
            {
                Environment.SetEnvironmentVariable("OPENAI_API_KEY", prevEnv);
            }
        }

        [Fact]
        public async Task TranscribeAudioAsync_WhenValidApiCall_ShouldReturnTranscript()
        {
            // Arrange
            var options = Options.Create(new OpenAIOptions { ApiKey = "test-key" });

            var handler = new MockHttpMessageHandler(req =>
            {
                req.Headers.Authorization?.Scheme.Should().Be("Bearer");
                req.Headers.Authorization?.Parameter.Should().Be("test-key");

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"text\": \"How do I submit my application?\"}", Encoding.UTF8, "application/json")
                };
            });

            var httpClient = new HttpClient(handler);
            var service = new OpenAITranscriptionService(httpClient, options, NullLogger<OpenAITranscriptionService>.Instance);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("audio-payload"));

            // Act
            var result = await service.TranscribeAudioAsync(stream, "audio.webm", "audio/webm");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Text.Should().Be("How do I submit my application?");
        }

        [Fact]
        public async Task TranscribeAudioAsync_WhenApiReturnsError_ShouldReturnFailure()
        {
            // Arrange
            var options = Options.Create(new OpenAIOptions { ApiKey = "test-key" });

            var handler = new MockHttpMessageHandler(req =>
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\": {\"message\": \"Invalid audio format\"}}", Encoding.UTF8, "application/json")
                };
            });

            var httpClient = new HttpClient(handler);
            var service = new OpenAITranscriptionService(httpClient, options, NullLogger<OpenAITranscriptionService>.Instance);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("bad audio"));

            // Act
            var result = await service.TranscribeAudioAsync(stream, "audio.webm", "audio/webm");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("failed with status BadRequest");
        }

        [Fact]
        public async Task TranscribeAudioAsync_ShouldSendPromptLanguageTemperature_AndNormalizeResult()
        {
            // Arrange
            var options = Options.Create(new OpenAIOptions
            {
                ApiKey = "test-key",
                TranscriptionLanguage = "en",
                TranscriptionPrompt = "Space Stem recruitment portal, leave policy"
            });

            string? sentLanguage = null;
            string? sentTemperature = null;
            string? sentPrompt = null;

            var handler = new MockHttpMessageHandler(req =>
            {
                if (req.Content is MultipartFormDataContent form)
                {
                    foreach (var content in form)
                    {
                        var disposition = content.Headers.ContentDisposition;
                        if (disposition?.Name?.Trim('"') == "language")
                        {
                            sentLanguage = content.ReadAsStringAsync().Result;
                        }
                        else if (disposition?.Name?.Trim('"') == "temperature")
                        {
                            sentTemperature = content.ReadAsStringAsync().Result;
                        }
                        else if (disposition?.Name?.Trim('"') == "prompt")
                        {
                            sentPrompt = content.ReadAsStringAsync().Result;
                        }
                    }
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"text\": \"Describe the live policy of space team.\"}", Encoding.UTF8, "application/json")
                };
            });

            var httpClient = new HttpClient(handler);
            var service = new OpenAITranscriptionService(httpClient, options, NullLogger<OpenAITranscriptionService>.Instance);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("sample audio"));

            // Act
            var result = await service.TranscribeAudioAsync(stream, "sample.webm", "audio/webm");

            // Assert
            result.IsSuccess.Should().BeTrue();
            sentLanguage.Should().Be("en");
            sentTemperature.Should().Be("0");
            sentPrompt.Should().Contain("Space Stem");
            // Verified that "live policy of space team" was normalized to "leave policy of Space Stem"
            result.Text.Should().Be("Describe the leave policy of Space Stem.");
        }

        [Theory]
        [InlineData("Describe the live policy of space team.", "Describe the leave policy of Space Stem.")]
        [InlineData("what is the live policy of spacestem?", "what is the leave policy of Space Stem?")]
        [InlineData("tell me about space them and sick live", "tell me about Space Stem and sick leave")]
        [InlineData("what are space time annual live rules", "what are Space Stem annual leave rules")]
        public void NormalizeTranscript_ShouldCorrectPhoneticMistakes(string input, string expected)
        {
            var result = OpenAITranscriptionService.NormalizeTranscript(input);
            result.Should().Be(expected);
        }
    }
}
