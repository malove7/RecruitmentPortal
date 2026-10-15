using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RecruitmentPortal.Controllers;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;
using RecruitmentPortal.Models.DTOs;
using RecruitmentPortal.Models.ViewModels;
using RecruitmentPortal.Services;
using Xunit;

namespace RecruitmentPortal.Tests.Controllers
{
    public class AIResumeScreeningControllerTests
    {
        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static void SeedJobPositions(ApplicationDbContext context)
        {
            context.JobPositions.AddRange(
                new JobPosition
                {
                    Id = 1,
                    Title = "Senior .NET Developer",
                    Description = "Build scalable backend APIs using ASP.NET Core.",
                    Requirements = "C#, .NET 8/10, Azure, SQL Server",
                    IsActive = true
                },
                new JobPosition
                {
                    Id = 2,
                    Title = "Frontend Engineer",
                    Description = "Build modern web user interfaces.",
                    Requirements = "React, TypeScript, CSS",
                    IsActive = false // Inactive
                });
            context.SaveChanges();
        }

        private static Mock<IFormFile> CreateMockFormFile(string fileName, string content = "Sample resume content for screening testing")
        {
            var fileMock = new Mock<IFormFile>();
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream target, CancellationToken _) =>
                {
                    ms.Position = 0;
                    return ms.CopyToAsync(target);
                });

            return fileMock;
        }

        [Fact]
        public async Task Index_ShouldReturnView_WithActiveJobPositionsOnly()
        {
            // Arrange
            using var context = CreateContext();
            SeedJobPositions(context);

            var parserMock = new Mock<IResumeParserService>();
            var aiServiceMock = new Mock<IAIResumeScreeningService>();
            var envMock = new Mock<IWebHostEnvironment>();
            var loggerMock = new Mock<ILogger<AIResumeScreeningController>>();

            var controller = new AIResumeScreeningController(
                context, parserMock.Object, aiServiceMock.Object, envMock.Object, loggerMock.Object);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<AIResumeScreeningCreateViewModel>().Subject;
            model.JobPositions.Should().HaveCount(1);
            model.JobPositions.First().Text.Should().Be("Senior .NET Developer");
        }

        [Fact]
        public async Task Screen_ShouldReturnIndexView_WhenModelStateIsInvalid()
        {
            // Arrange
            using var context = CreateContext();
            SeedJobPositions(context);

            var parserMock = new Mock<IResumeParserService>();
            var aiServiceMock = new Mock<IAIResumeScreeningService>();
            var envMock = new Mock<IWebHostEnvironment>();
            var loggerMock = new Mock<ILogger<AIResumeScreeningController>>();

            var controller = new AIResumeScreeningController(
                context, parserMock.Object, aiServiceMock.Object, envMock.Object, loggerMock.Object);
            controller.ModelState.AddModelError("JobPositionId", "Required");

            var model = new AIResumeScreeningCreateViewModel { JobPositionId = 0 };

            // Act
            var result = await controller.Screen(model);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewName.Should().Be("Index");
            model.JobPositions.Should().HaveCount(1);
        }

        [Fact]
        public async Task Screen_ShouldReturnIndexView_WhenJobPositionDoesNotExist()
        {
            // Arrange
            using var context = CreateContext();
            SeedJobPositions(context);

            var parserMock = new Mock<IResumeParserService>();
            var aiServiceMock = new Mock<IAIResumeScreeningService>();
            var envMock = new Mock<IWebHostEnvironment>();
            var loggerMock = new Mock<ILogger<AIResumeScreeningController>>();

            var controller = new AIResumeScreeningController(
                context, parserMock.Object, aiServiceMock.Object, envMock.Object, loggerMock.Object);

            var fileMock = CreateMockFormFile("resume.pdf");
            var model = new AIResumeScreeningCreateViewModel
            {
                JobPositionId = 999, // non-existent
                ResumeFile = fileMock.Object
            };

            // Act
            var result = await controller.Screen(model);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewName.Should().Be("Index");
            controller.ModelState.ContainsKey(nameof(model.JobPositionId)).Should().BeTrue();
        }

        [Fact]
        public async Task Screen_ShouldReturnIndexView_WhenResumeParsingFails()
        {
            // Arrange
            using var context = CreateContext();
            SeedJobPositions(context);

            var parserMock = new Mock<IResumeParserService>();
            parserMock.Setup(p => p.ParseResumeAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResumeParseResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Corrupted PDF format."
                });

            var aiServiceMock = new Mock<IAIResumeScreeningService>();
            var envMock = new Mock<IWebHostEnvironment>();
            var loggerMock = new Mock<ILogger<AIResumeScreeningController>>();

            var controller = new AIResumeScreeningController(
                context, parserMock.Object, aiServiceMock.Object, envMock.Object, loggerMock.Object);

            var fileMock = CreateMockFormFile("corrupt.pdf");
            var model = new AIResumeScreeningCreateViewModel
            {
                JobPositionId = 1,
                ResumeFile = fileMock.Object
            };

            // Act
            var result = await controller.Screen(model);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewName.Should().Be("Index");
            controller.ModelState.ContainsKey(nameof(model.ResumeFile)).Should().BeTrue();
            controller.ModelState[nameof(model.ResumeFile)]!.Errors[0].ErrorMessage.Should().Contain("Corrupted PDF format.");
        }

        [Fact]
        public async Task Screen_ShouldReturnIndexView_WhenAIServiceReturnsFailure()
        {
            // Arrange
            using var context = CreateContext();
            SeedJobPositions(context);

            var parserMock = new Mock<IResumeParserService>();
            parserMock.Setup(p => p.ParseResumeAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResumeParseResult
                {
                    IsSuccess = true,
                    ExtractedText = "Valid resume text with .NET and C# experience."
                });

            var aiServiceMock = new Mock<IAIResumeScreeningService>();
            aiServiceMock.Setup(a => a.ScreenResumeAsync(It.IsAny<AIScreeningInput>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIScreeningServiceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "OpenAI rate limit exceeded."
                });

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.WebRootPath).Returns(tempDir);

            var loggerMock = new Mock<ILogger<AIResumeScreeningController>>();

            var controller = new AIResumeScreeningController(
                context, parserMock.Object, aiServiceMock.Object, envMock.Object, loggerMock.Object);

            var fileMock = CreateMockFormFile("resume.pdf");
            var model = new AIResumeScreeningCreateViewModel
            {
                JobPositionId = 1,
                ResumeFile = fileMock.Object
            };

            // Act
            var result = await controller.Screen(model);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewName.Should().Be("Index");
            controller.ModelState[string.Empty]!.Errors[0].ErrorMessage.Should().Contain("OpenAI rate limit exceeded.");

            // Cleanup
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task Screen_ShouldSaveScreeningAndRedirectToResult_WhenSuccessful()
        {
            // Arrange
            using var context = CreateContext();
            SeedJobPositions(context);

            var parserMock = new Mock<IResumeParserService>();
            parserMock.Setup(p => p.ParseResumeAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResumeParseResult
                {
                    IsSuccess = true,
                    ExtractedText = "Experienced ASP.NET Core developer with 7 years of building REST APIs."
                });

            var aiResponse = new AIScreeningResponseDto
            {
                OverallScore = 85,
                Recommendation = "Proceed to Technical Interview",
                RecruiterSummary = "Strong backend match with extensive .NET experience.",
                Skills = new List<AISkillEvaluationDto>
                {
                    new()
                    {
                        Skill = "ASP.NET Core",
                        Score = 9,
                        MaxScore = 10,
                        Status = "Strong Match",
                        Confidence = 0.95,
                        Evidence = new List<string> { "Built REST APIs with ASP.NET Core" },
                        Gaps = new List<string>()
                    }
                },
                Strengths = new List<string> { "Deep C# and backend architecture knowledge" },
                EvidenceExtracted = new List<AIEvidenceDto>
                {
                    new() { Category = "Technical", Evidence = "Built REST APIs with ASP.NET Core" }
                },
                Concerns = new List<string> { "No microservices architecture mentioned" },
                Gaps = new List<string> { "Azure Service Bus" },
                ExperienceAnalysis = new AIExperienceAnalysisDto
                {
                    RequiredExperience = "5+ years",
                    CandidateExperience = "7 years",
                    Status = "Meets Requirement",
                    Comments = "Candidate has 7 years of relevant experience"
                },
                FollowUpQuestions = new List<AIFollowUpQuestionDto>
                {
                    new()
                    {
                        Category = "Technical",
                        Skill = "Azure",
                        Priority = "High",
                        Reason = "Azure experience details are minimal",
                        Question = "Which Azure services have you used in production?"
                    }
                }
            };

            var aiServiceMock = new Mock<IAIResumeScreeningService>();
            aiServiceMock.Setup(a => a.ScreenResumeAsync(It.IsAny<AIScreeningInput>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIScreeningServiceResult
                {
                    IsSuccess = true,
                    Data = aiResponse,
                    RawJsonResponse = "{}",
                    ModelUsed = "gpt-4o",
                    PromptVersion = "1.0"
                });

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.WebRootPath).Returns(tempDir);

            var loggerMock = new Mock<ILogger<AIResumeScreeningController>>();

            var controller = new AIResumeScreeningController(
                context, parserMock.Object, aiServiceMock.Object, envMock.Object, loggerMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "recruiter@portal.com"),
                new Claim(ClaimTypes.Role, "HR")
            }, "TestAuth"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var fileMock = CreateMockFormFile("JohnDoe_Resume.pdf");
            var model = new AIResumeScreeningCreateViewModel
            {
                JobPositionId = 1,
                ResumeFile = fileMock.Object
            };

            // Act
            var result = await controller.Screen(model);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be("Result");
            var screeningId = redirectResult.RouteValues!["id"].Should().BeOfType<int>().Subject;

            // Verify persistence in DB
            var saved = await context.AIResumeScreenings
                .Include(s => s.Skills)
                .Include(s => s.Strengths)
                .Include(s => s.EvidenceExtracted)
                .Include(s => s.Concerns)
                .Include(s => s.Gaps)
                .Include(s => s.FollowUpQuestions)
                .FirstOrDefaultAsync(s => s.Id == screeningId);

            saved.Should().NotBeNull();
            saved!.OverallScore.Should().Be(85);
            saved.Recommendation.Should().Be("Proceed to Technical Interview");
            saved.CreatedBy.Should().Be("recruiter@portal.com");
            saved.JobTitleSnapshot.Should().Be("Senior .NET Developer");
            saved.Skills.Should().HaveCount(1);
            saved.Skills.First().Skill.Should().Be("ASP.NET Core");
            saved.Strengths.Should().HaveCount(1);
            saved.EvidenceExtracted.Should().HaveCount(1);
            saved.Concerns.Should().HaveCount(1);
            saved.Gaps.Should().HaveCount(1);
            saved.FollowUpQuestions.Should().HaveCount(1);
            saved.FollowUpQuestions.First().Priority.Should().Be("High");

            // Cleanup
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task Result_ShouldReturnResultView_WhenScreeningExists()
        {
            // Arrange
            using var context = CreateContext();
            SeedJobPositions(context);

            var screening = new AIResumeScreening
            {
                JobPositionId = 1,
                ResumeFileName = "JaneDoe.pdf",
                ResumeFilePath = "/uploads/resumes/test.pdf",
                OverallScore = 90,
                Recommendation = "Strong Candidate",
                RecruiterSummary = "Excellent candidate",
                JobTitleSnapshot = "Senior .NET Developer",
                JobDescriptionSnapshot = "Build APIs",
                JobRequirementsSnapshot = "C#",
                CreatedBy = "admin@portal.com",
                CreatedDate = DateTime.UtcNow,
                AIModel = "gpt-4o",
                PromptVersion = "1.0",
                Skills = new List<AIResumeSkillEvaluation>
                {
                    new()
                    {
                        Skill = "C#",
                        Score = 10,
                        MaxScore = 10,
                        Status = "Strong Match",
                        Confidence = 0.99,
                        EvidenceJson = "[\"10 years C#\"]"
                    }
                },
                FollowUpQuestions = new List<AIResumeFollowUpQuestion>
                {
                    new()
                    {
                        Category = "System Design",
                        Skill = "Architecture",
                        Priority = "High",
                        Question = "Describe your largest microservices deployment.",
                        Reason = "Need to assess system scale"
                    }
                }
            };
            context.AIResumeScreenings.Add(screening);
            await context.SaveChangesAsync();

            var parserMock = new Mock<IResumeParserService>();
            var aiServiceMock = new Mock<IAIResumeScreeningService>();
            var envMock = new Mock<IWebHostEnvironment>();
            var loggerMock = new Mock<ILogger<AIResumeScreeningController>>();

            var controller = new AIResumeScreeningController(
                context, parserMock.Object, aiServiceMock.Object, envMock.Object, loggerMock.Object);

            // Act
            var result = await controller.Result(screening.Id);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<AIResumeScreeningResultViewModel>().Subject;
            model.Id.Should().Be(screening.Id);
            model.OverallScore.Should().Be(90);
            model.Recommendation.Should().Be("Strong Candidate");
            model.Skills.Should().HaveCount(1);
            model.Skills.First().Evidence.Should().Contain("10 years C#");
            model.FollowUpQuestions.Should().HaveCount(1);
        }

        [Fact]
        public async Task Result_ShouldReturnNotFound_WhenScreeningDoesNotExist()
        {
            // Arrange
            using var context = CreateContext();

            var parserMock = new Mock<IResumeParserService>();
            var aiServiceMock = new Mock<IAIResumeScreeningService>();
            var envMock = new Mock<IWebHostEnvironment>();
            var loggerMock = new Mock<ILogger<AIResumeScreeningController>>();

            var controller = new AIResumeScreeningController(
                context, parserMock.Object, aiServiceMock.Object, envMock.Object, loggerMock.Object);

            // Act
            var result = await controller.Result(9999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task History_ShouldReturnFilteredScreenings()
        {
            // Arrange
            using var context = CreateContext();
            SeedJobPositions(context);

            context.AIResumeScreenings.AddRange(
                new AIResumeScreening
                {
                    JobPositionId = 1,
                    ResumeFileName = "Alice_Resume.pdf",
                    ResumeFilePath = "/path1",
                    OverallScore = 80,
                    Recommendation = "Proceed",
                    RecruiterSummary = "Good",
                    CreatedBy = "hr1@portal.com",
                    CreatedDate = DateTime.UtcNow
                },
                new AIResumeScreening
                {
                    JobPositionId = 1,
                    ResumeFileName = "Bob_Resume.pdf",
                    ResumeFilePath = "/path2",
                    OverallScore = 60,
                    Recommendation = "Hold",
                    RecruiterSummary = "Average",
                    CreatedBy = "hr2@portal.com",
                    CreatedDate = DateTime.UtcNow.AddHours(-1)
                });
            await context.SaveChangesAsync();

            var parserMock = new Mock<IResumeParserService>();
            var aiServiceMock = new Mock<IAIResumeScreeningService>();
            var envMock = new Mock<IWebHostEnvironment>();
            var loggerMock = new Mock<ILogger<AIResumeScreeningController>>();

            var controller = new AIResumeScreeningController(
                context, parserMock.Object, aiServiceMock.Object, envMock.Object, loggerMock.Object);

            // Act - search by "Alice"
            var result = await controller.History(1, "Alice");

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<AIResumeScreeningHistoryViewModel>().Subject;
            model.Screenings.Should().HaveCount(1);
            model.Screenings.First().ResumeFileName.Should().Be("Alice_Resume.pdf");
        }

        [Fact]
        public async Task GetJobPositionDetails_ShouldReturnPositionJson_WhenExists()
        {
            // Arrange
            using var context = CreateContext();
            SeedJobPositions(context);

            var parserMock = new Mock<IResumeParserService>();
            var aiServiceMock = new Mock<IAIResumeScreeningService>();
            var envMock = new Mock<IWebHostEnvironment>();
            var loggerMock = new Mock<ILogger<AIResumeScreeningController>>();

            var controller = new AIResumeScreeningController(
                context, parserMock.Object, aiServiceMock.Object, envMock.Object, loggerMock.Object);

            // Act
            var result = await controller.GetJobPositionDetails(1);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            jsonResult.Value.Should().NotBeNull();
        }
    }
}
