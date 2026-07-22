using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPortal.Controllers;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;
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

    [Fact]
    public async Task CreatePost_ShouldPersistOptionalMarriedAndChildValues()
    {
        using var context = CreateContext();
        var controller = new CandidateEvaluationFormsController(context);
        var model = BuildValidModel(Guid.NewGuid());
        model.Married = true;
        model.Child = false;

        await controller.Create(model);

        var form = await context.CandidateEvaluationForms.SingleAsync();
        form.Married.Should().BeTrue();
        form.Child.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePost_ShouldAllowMarriedAndChildToBeUnanswered()
    {
        using var context = CreateContext();
        var controller = new CandidateEvaluationFormsController(context);

        await controller.Create(BuildValidModel(Guid.NewGuid()));

        var form = await context.CandidateEvaluationForms.SingleAsync();
        form.Married.Should().BeNull();
        form.Child.Should().BeNull();
    }

    [Fact]
    public async Task EditPost_ShouldUpdateMarriedAndChildValues()
    {
        using var context = CreateContext();
        var form = new CandidateEvaluationForm
        {
            Email = "candidate@example.com",
            FullName = "Test Candidate",
            PositionAppliedFor = "Developer",
            Married = false,
            Child = true
        };
        context.CandidateEvaluationForms.Add(form);
        await context.SaveChangesAsync();
        var controller = new CandidateEvaluationFormsController(context);
        var model = BuildValidModel(Guid.NewGuid());
        model.Id = form.Id;
        model.Married = true;
        model.Child = false;

        await controller.Edit(form.Id, model);

        form.Married.Should().BeTrue();
        form.Child.Should().BeFalse();
    }

    [Fact]
    public async Task EditGet_ShouldReturnStoredMarriedAndChildValues()
    {
        using var context = CreateContext();
        var form = new CandidateEvaluationForm
        {
            Email = "candidate@example.com",
            FullName = "Test Candidate",
            PositionAppliedFor = "Developer",
            Married = true,
            Child = false
        };
        context.CandidateEvaluationForms.Add(form);
        await context.SaveChangesAsync();
        var controller = new CandidateEvaluationFormsController(context);

        var result = await controller.Edit(form.Id);

        var model = result.Should().BeOfType<ViewResult>().Subject.Model
            .Should().BeOfType<CandidateEvaluationFormViewModel>().Subject;
        model.Married.Should().BeTrue();
        model.Child.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteConfirmed_ShouldDeleteFormWithMarriedAndChildValues()
    {
        using var context = CreateContext();
        var form = new CandidateEvaluationForm
        {
            Email = "candidate@example.com",
            FullName = "Test Candidate",
            PositionAppliedFor = "Developer",
            Married = true,
            Child = true
        };
        context.CandidateEvaluationForms.Add(form);
        await context.SaveChangesAsync();
        var controller = new CandidateEvaluationFormsController(context);

        await controller.DeleteConfirmed(form.Id);

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
