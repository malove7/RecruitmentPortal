using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RecruitmentPortal.Controllers;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;
using RecruitmentPortal.Models.DTOs;
using RecruitmentPortal.Services;
using System.Text;
using Xunit;

namespace RecruitmentPortal.Tests.Controllers
{
    public class HomepageChatControllerTests
    {
        private readonly Mock<IRagChatService> _ragChatServiceMock = new();
        private readonly Mock<IOpenAITranscriptionService> _transcriptionServiceMock = new();
        private readonly Mock<ILogger<HomepageChatController>> _loggerMock = new();

        private HomepageChatOptions CreateDefaultOptions()
        {
            return new HomepageChatOptions
            {
                Enabled = true,
                DefaultKnowledgeBaseId = 1,
                MaxMessageLength = 1000,
                MaxAudioSizeMB = 10,
                EnableVoiceInput = true,
                FallbackMessage = "Fallback answer for tests."
            };
        }

        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public void GetConfig_ShouldReturnConfigValues()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var options = CreateDefaultOptions();
            var controller = new HomepageChatController(
                db,
                _ragChatServiceMock.Object,
                _transcriptionServiceMock.Object,
                Options.Create(options),
                _loggerMock.Object);

            // Act
            var result = controller.GetConfig();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var config = okResult.Value.Should().BeOfType<HomepageChatConfigDto>().Subject;
            config.Enabled.Should().BeTrue();
            config.EnableVoiceInput.Should().BeTrue();
            config.MaxMessageLength.Should().Be(1000);
            config.FallbackMessage.Should().Be("Fallback answer for tests.");
        }

        [Fact]
        public async Task SendMessage_WhenDisabled_ShouldReturn503ServiceUnavailable()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var options = CreateDefaultOptions();
            options.Enabled = false;

            var controller = new HomepageChatController(
                db,
                _ragChatServiceMock.Object,
                _transcriptionServiceMock.Object,
                Options.Create(options),
                _loggerMock.Object);

            // Act
            var result = await controller.SendMessage(new HomepageChatMessageRequest { Message = "Hello" }, CancellationToken.None);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
            var response = statusResult.Value.Should().BeOfType<HomepageChatMessageResponse>().Subject;
            response.Success.Should().BeFalse();
            response.IsFallback.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task SendMessage_WhenEmptyMessage_ShouldReturnBadRequest(string? message)
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var controller = new HomepageChatController(
                db,
                _ragChatServiceMock.Object,
                _transcriptionServiceMock.Object,
                Options.Create(CreateDefaultOptions()),
                _loggerMock.Object);

            // Act
            var result = await controller.SendMessage(new HomepageChatMessageRequest { Message = message! }, CancellationToken.None);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequest.Value.Should().BeOfType<HomepageChatMessageResponse>().Subject;
            response.Success.Should().BeFalse();
            response.ErrorMessage.Should().Contain("cannot be empty");
        }

        [Fact]
        public async Task SendMessage_WhenExceedsMaxMessageLength_ShouldReturnBadRequest()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var options = CreateDefaultOptions();
            options.MaxMessageLength = 50;

            var controller = new HomepageChatController(
                db,
                _ragChatServiceMock.Object,
                _transcriptionServiceMock.Object,
                Options.Create(options),
                _loggerMock.Object);

            var longMessage = new string('a', 60);

            // Act
            var result = await controller.SendMessage(new HomepageChatMessageRequest { Message = longMessage }, CancellationToken.None);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequest.Value.Should().BeOfType<HomepageChatMessageResponse>().Subject;
            response.Success.Should().BeFalse();
            response.ErrorMessage.Should().Contain("cannot exceed");
        }

        [Fact]
        public async Task SendMessage_WhenNoActiveKnowledgeBaseExists_ShouldReturnFallback()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var options = CreateDefaultOptions();

            var controller = new HomepageChatController(
                db,
                _ragChatServiceMock.Object,
                _transcriptionServiceMock.Object,
                Options.Create(options),
                _loggerMock.Object);

            // Act
            var result = await controller.SendMessage(new HomepageChatMessageRequest { Message = "What are the benefits?" }, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<HomepageChatMessageResponse>().Subject;
            response.Success.Should().BeTrue();
            response.IsFallback.Should().BeTrue();
            response.Answer.Should().Be(options.FallbackMessage);
        }

        [Fact]
        public async Task SendMessage_WhenValid_ShouldCallRagServiceAndReturnAnswer()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var kb = new KnowledgeBase
            {
                Id = 1,
                Name = "Public KB",
                IsActive = true
            };
            db.KnowledgeBases.Add(kb);
            await db.SaveChangesAsync();

            var options = CreateDefaultOptions();

            _ragChatServiceMock
                .Setup(r => r.AskQuestionAsync(1, "What are the perks?", null, null, It.IsAny<CancellationToken>(), false))
                .ReturnsAsync(new RagChatResponse
                {
                    IsSuccess = true,
                    SessionId = 42,
                    MessageId = 101,
                    Answer = "We offer flexible hours and health insurance.",
                    Citations = new List<RagCitation>
                    {
                        new RagCitation { ChunkId = 1, DocumentId = 2, FileName = "Benefits.pdf", PageNumber = 3, ChunkIndex = 0 }
                    }
                });

            var controller = new HomepageChatController(
                db,
                _ragChatServiceMock.Object,
                _transcriptionServiceMock.Object,
                Options.Create(options),
                _loggerMock.Object);

            // Act
            var result = await controller.SendMessage(new HomepageChatMessageRequest { Message = "What are the perks?" }, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<HomepageChatMessageResponse>().Subject;
            response.Success.Should().BeTrue();
            response.IsFallback.Should().BeFalse();
            response.Answer.Should().Be("We offer flexible hours and health insurance.");
            response.SessionId.Should().Be(42);
            response.Citations.Should().BeEmpty();
        }

        [Fact]
        public async Task SendMessage_WhenAnswerContainsInlineCitations_ShouldStripCitationsAndReturnCleanText()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            db.KnowledgeBases.Add(new KnowledgeBase { Id = 1, Name = "Public KB", IsActive = true });
            await db.SaveChangesAsync();

            var options = CreateDefaultOptions();

            _ragChatServiceMock
                .Setup(r => r.AskQuestionAsync(1, "When does holiday start?", null, null, It.IsAny<CancellationToken>(), false))
                .ReturnsAsync(new RagChatResponse
                {
                    IsSuccess = true,
                    SessionId = 42,
                    Answer = "Annual leave resets on January 1st (Citation 2, HR POLICY HANDBOOK_Updated.pdf, Page 36, Chunk 35)."
                });

            var controller = new HomepageChatController(
                db,
                _ragChatServiceMock.Object,
                _transcriptionServiceMock.Object,
                Options.Create(options),
                _loggerMock.Object);

            // Act
            var result = await controller.SendMessage(new HomepageChatMessageRequest { Message = "When does holiday start?" }, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<HomepageChatMessageResponse>().Subject;
            response.Success.Should().BeTrue();
            response.Answer.Should().Be("Annual leave resets on January 1st.");
            response.Citations.Should().BeEmpty();
        }

        [Fact]
        public async Task SendMessage_WhenRagReturnsNotFound_ShouldReturnConfiguredFallback()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            db.KnowledgeBases.Add(new KnowledgeBase { Id = 1, Name = "Public KB", IsActive = true });
            await db.SaveChangesAsync();

            var options = CreateDefaultOptions();

            _ragChatServiceMock
                .Setup(r => r.AskQuestionAsync(1, "Unknown question", null, null, It.IsAny<CancellationToken>(), false))
                .ReturnsAsync(new RagChatResponse
                {
                    IsSuccess = true,
                    SessionId = 42,
                    Answer = RagChatService.DefaultNotFoundMessage,
                    Citations = new List<RagCitation>()
                });

            var controller = new HomepageChatController(
                db,
                _ragChatServiceMock.Object,
                _transcriptionServiceMock.Object,
                Options.Create(options),
                _loggerMock.Object);

            // Act
            var result = await controller.SendMessage(new HomepageChatMessageRequest { Message = "Unknown question" }, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<HomepageChatMessageResponse>().Subject;
            response.IsFallback.Should().BeTrue();
            response.Answer.Should().Be(options.FallbackMessage);
        }

        [Fact]
        public async Task TranscribeAudio_WhenDisabled_ShouldReturn503()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var options = CreateDefaultOptions();
            options.Enabled = false;

            var controller = new HomepageChatController(
                db,
                _ragChatServiceMock.Object,
                _transcriptionServiceMock.Object,
                Options.Create(options),
                _loggerMock.Object);

            var fileMock = new Mock<IFormFile>();

            // Act
            var result = await controller.TranscribeAudio(fileMock.Object, CancellationToken.None);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        }

        [Fact]
        public async Task TranscribeAudio_WhenVoiceDisabled_ShouldReturnBadRequest()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var options = CreateDefaultOptions();
            options.EnableVoiceInput = false;

            var controller = new HomepageChatController(
                db,
                _ragChatServiceMock.Object,
                _transcriptionServiceMock.Object,
                Options.Create(options),
                _loggerMock.Object);

            var fileMock = new Mock<IFormFile>();

            // Act
            var result = await controller.TranscribeAudio(fileMock.Object, CancellationToken.None);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequest.Value.Should().BeOfType<HomepageVoiceTranscribeResponse>().Subject;
            response.Success.Should().BeFalse();
            response.ErrorMessage.Should().Contain("Voice input is currently disabled");
        }

        [Fact]
        public async Task TranscribeAudio_WhenFileIsNull_ShouldReturnBadRequest()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var controller = new HomepageChatController(
                db,
                _ragChatServiceMock.Object,
                _transcriptionServiceMock.Object,
                Options.Create(CreateDefaultOptions()),
                _loggerMock.Object);

            // Act
            var result = await controller.TranscribeAudio(null, CancellationToken.None);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequest.Value.Should().BeOfType<HomepageVoiceTranscribeResponse>().Subject;
            response.Success.Should().BeFalse();
            response.ErrorMessage.Should().Contain("Audio file is required");
        }

        [Fact]
        public async Task TranscribeAudio_WhenValidAudio_ShouldCallTranscriptionService()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var options = CreateDefaultOptions();

            _transcriptionServiceMock
                .Setup(t => t.TranscribeAudioAsync(It.IsAny<Stream>(), "test.webm", "audio/webm", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TranscriptionResult
                {
                    IsSuccess = true,
                    Text = "What is the interview procedure?"
                });

            var controller = new HomepageChatController(
                db,
                _ragChatServiceMock.Object,
                _transcriptionServiceMock.Object,
                Options.Create(options),
                _loggerMock.Object);

            var audioBytes = Encoding.UTF8.GetBytes("fake audio data");
            var ms = new MemoryStream(audioBytes);
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(audioBytes.Length);
            fileMock.Setup(f => f.FileName).Returns("test.webm");
            fileMock.Setup(f => f.ContentType).Returns("audio/webm");
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);

            // Act
            var result = await controller.TranscribeAudio(fileMock.Object, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<HomepageVoiceTranscribeResponse>().Subject;
            response.Success.Should().BeTrue();
            response.Transcript.Should().Be("What is the interview procedure?");
        }
    }
}
