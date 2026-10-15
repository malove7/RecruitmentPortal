using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RecruitmentPortal.Models.DTOs;
using RecruitmentPortal.Services;
using Xunit;

namespace RecruitmentPortal.Tests.Services
{
    public class OpenAIResumeScreeningServiceTests
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
        public async Task ScreenResumeAsync_ShouldReturnError_WhenApiKeyIsEmpty()
        {
            var options = Options.Create(new OpenAIOptions { ApiKey = "" });
            var httpClient = new HttpClient();
            var logger = new Mock<ILogger<OpenAIResumeScreeningService>>().Object;
            var service = new OpenAIResumeScreeningService(httpClient, options, logger);

            var input = new AIScreeningInput
            {
                JobTitle = ".NET Dev",
                JobDescription = "APIs",
                JobRequirements = "C#",
                ResumeFileName = "test.pdf",
                ResumeText = "John Doe 5 years C#"
            };

            var result = await service.ScreenResumeAsync(input);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("API Key is not configured");
        }

        [Fact]
        public async Task ScreenResumeAsync_ShouldParseValidJsonResponse_Successfully()
        {
            var jsonPayload = @"{
  ""id"": ""chatcmpl-123"",
  ""choices"": [
    {
      ""message"": {
        ""role"": ""assistant"",
        ""content"": ""{\""overallScore\"": 82, \""recommendation\"": \""Proceed to Technical Interview\"", \""recruiterSummary\"": \""Candidate matches .NET backend criteria.\"", \""skills\"": [ { \""skill\"": \""ASP.NET Core\"", \""score\"": 9, \""maxScore\"": 10, \""status\"": \""Strong Match\"", \""confidence\"": 0.95, \""evidence\"": [\""Developed REST APIs\""], \""gaps\"": [] } ], \""strengths\"": [\""Solid .NET expertise\""], \""evidenceExtracted\"": [ { \""category\"": \""Technical\"", \""evidence\"": \""Developed REST APIs\"" } ], \""concerns\"": [], \""gaps\"": [], \""experienceAnalysis\"": { \""requiredExperience\"": \""5+ years\"", \""candidateExperience\"": \""7 years\"", \""status\"": \""Meets Requirement\"", \""comments\"": \""Sufficient experience\"" }, \""followUpQuestions\"": [ { \""category\"": \""Technical\"", \""skill\"": \""Azure\"", \""priority\"": \""High\"", \""reason\"": \""Unclear Azure depth\"", \""question\"": \""Which Azure services did you personally configure?\"" } ]}""
      }
    }
  ]
}";

            var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            });

            var httpClient = new HttpClient(handler);
            var options = Options.Create(new OpenAIOptions { ApiKey = "sk-mock-key-for-testing", Model = "gpt-4o" });
            var logger = new Mock<ILogger<OpenAIResumeScreeningService>>().Object;
            var service = new OpenAIResumeScreeningService(httpClient, options, logger);

            var input = new AIScreeningInput
            {
                JobTitle = "Senior .NET Developer",
                JobDescription = "REST APIs",
                JobRequirements = "ASP.NET Core",
                ResumeFileName = "resume.pdf",
                ResumeText = "Developed REST APIs using ASP.NET Core for 7 years."
            };

            var result = await service.ScreenResumeAsync(input);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.OverallScore.Should().Be(82);
            result.Data.Recommendation.Should().Be("Proceed to Technical Interview");
            result.Data.Skills.Should().HaveCount(1);
            result.Data.Skills[0].Skill.Should().Be("ASP.NET Core");
            result.Data.Skills[0].Score.Should().Be(9);
            result.Data.FollowUpQuestions.Should().HaveCount(1);
            result.Data.FollowUpQuestions[0].Priority.Should().Be("High");
        }

        [Fact]
        public async Task ScreenResumeAsync_ShouldHandleMarkdownWrappedJson()
        {
            var rawContent = "```json\n{\n  \"overallScore\": 75,\n  \"recommendation\": \"Proceed\",\n  \"recruiterSummary\": \"Good\",\n  \"skills\": [],\n  \"strengths\": [],\n  \"evidenceExtracted\": [],\n  \"concerns\": [],\n  \"gaps\": [],\n  \"experienceAnalysis\": {\"requiredExperience\":\"\",\"candidateExperience\":\"\",\"status\":\"\",\"comments\":\"\"},\n  \"followUpQuestions\": []\n}\n```";

            var escapedContent = rawContent.Replace("\"", "\\\"").Replace("\n", "\\n");
            var jsonPayload = $@"{{
  ""choices"": [
    {{
      ""message"": {{
        ""role"": ""assistant"",
        ""content"": ""{escapedContent}""
      }}
    }}
  ]
}}";

            var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            });

            var httpClient = new HttpClient(handler);
            var options = Options.Create(new OpenAIOptions { ApiKey = "sk-mock-key", Model = "gpt-4o" });
            var logger = new Mock<ILogger<OpenAIResumeScreeningService>>().Object;
            var service = new OpenAIResumeScreeningService(httpClient, options, logger);

            var input = new AIScreeningInput
            {
                JobTitle = "Developer",
                JobDescription = "Testing",
                JobRequirements = "None",
                ResumeFileName = "test.docx",
                ResumeText = "Developer with experience"
            };

            var result = await service.ScreenResumeAsync(input);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.OverallScore.Should().Be(75);
        }

        [Fact]
        public async Task ScreenResumeAsync_ShouldReturnError_WhenUnauthorized()
        {
            var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\": {\"message\": \"Incorrect API key\"}}", Encoding.UTF8, "application/json")
            });

            var httpClient = new HttpClient(handler);
            var options = Options.Create(new OpenAIOptions { ApiKey = "invalid-key" });
            var logger = new Mock<ILogger<OpenAIResumeScreeningService>>().Object;
            var service = new OpenAIResumeScreeningService(httpClient, options, logger);

            var input = new AIScreeningInput { JobTitle = "Dev", ResumeFileName = "test.pdf", ResumeText = "text" };
            var result = await service.ScreenResumeAsync(input);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Authentication failed");
        }

        [Fact]
        public async Task ScreenResumeAsync_ShouldReturnError_WhenRateLimited()
        {
            var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("{\"error\": {\"message\": \"Rate limit exceeded\"}}", Encoding.UTF8, "application/json")
            });

            var httpClient = new HttpClient(handler);
            var options = Options.Create(new OpenAIOptions { ApiKey = "rate-limited-key" });
            var logger = new Mock<ILogger<OpenAIResumeScreeningService>>().Object;
            var service = new OpenAIResumeScreeningService(httpClient, options, logger);

            var input = new AIScreeningInput { JobTitle = "Dev", ResumeFileName = "test.pdf", ResumeText = "text" };
            var result = await service.ScreenResumeAsync(input);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("rate limit exceeded");
        }
    }
}
