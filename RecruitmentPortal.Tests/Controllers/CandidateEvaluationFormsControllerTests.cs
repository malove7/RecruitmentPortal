using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPortal.Controllers;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models.ViewModels;
using Xunit;

namespace RecruitmentPortal.Tests.Controllers;

public class CandidateEvaluationFormsControllerTests
{
    [Fact]
    public void CreateGet_ShouldIssueSubmissionToken()
    {
        using var context = CreateContext();
        var controller = new CandidateEvaluationFormsController(context);

        var result = controller.Create();

        var model = result.Should().BeOfType<ViewResult>().Subject.Model
            .Should().BeOfType<CandidateEvaluationFormViewModel>().Subject;
        model.SubmissionToken.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreatePost_ShouldInsertOnlyOnce_WhenSubmissionTokenIsReused()
    {
        using var context = CreateContext();
        var controller = new CandidateEvaluationFormsController(context);
        var token = Guid.NewGuid();

        await controller.Create(BuildValidModel(token));
        var secondResult = await controller.Create(BuildValidModel(token));

        secondResult.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Success");
        (await context.CandidateEvaluationForms.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreatePost_ShouldRejectMissingSubmissionToken()
    {
        using var context = CreateContext();
        var controller = new CandidateEvaluationFormsController(context);

        var result = await controller.Create(BuildValidModel(Guid.Empty));

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
        (await context.CandidateEvaluationForms.CountAsync()).Should().Be(0);
    }

    private static CandidateEvaluationFormViewModel BuildValidModel(Guid token) => new()
    {
        SubmissionToken = token,
        Email = "candidate@example.com",
        FullName = "Test Candidate",
        PositionAppliedFor = "Developer"
    };

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
