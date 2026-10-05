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

    [Fact]
    public async Task GenerateFeedbackLink_ShouldGenerateUniqueToken_WithFeedbackType()
    {
        using var context = CreateContext();
        var form = new CandidateEvaluationForm
        {
            Email = "candidate@example.com",
            FullName = "Jane Doe",
            PositionAppliedFor = "Senior Backend Engineer"
        };
        context.CandidateEvaluationForms.Add(form);
        await context.SaveChangesAsync();

        var controller = new CandidateEvaluationFormsController(context);
        var request = new GenerateFeedbackLinkRequest
        {
            FormId = form.Id,
            FeedbackType = "Technical"
        };

        var result = await controller.GenerateFeedbackLink(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var tokenEntity = await context.CandidateFeedbackTokens.SingleAsync();
        tokenEntity.CandidateEvaluationFormId.Should().Be(form.Id);
        tokenEntity.FeedbackType.Should().Be("Technical");
        tokenEntity.IsUsed.Should().BeFalse();
        tokenEntity.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GenerateFeedbackLink_ShouldAllowEmptyFeedbackType()
    {
        using var context = CreateContext();
        var form = new CandidateEvaluationForm
        {
            Email = "candidate@example.com",
            FullName = "John Doe",
            PositionAppliedFor = "QA Engineer"
        };
        context.CandidateEvaluationForms.Add(form);
        await context.SaveChangesAsync();

        var controller = new CandidateEvaluationFormsController(context);
        var request = new GenerateFeedbackLinkRequest
        {
            FormId = form.Id,
            FeedbackType = null
        };

        var result = await controller.GenerateFeedbackLink(request);

        result.Should().BeOfType<OkObjectResult>();
        var tokenEntity = await context.CandidateFeedbackTokens.SingleAsync();
        tokenEntity.CandidateEvaluationFormId.Should().Be(form.Id);
        tokenEntity.FeedbackType.Should().BeNull();
        tokenEntity.IsUsed.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateFeedbackLink_ShouldReturnNotFound_WhenFormDoesNotExist()
    {
        using var context = CreateContext();
        var controller = new CandidateEvaluationFormsController(context);

        var result = await controller.GenerateFeedbackLink(new GenerateFeedbackLinkRequest { FormId = 999 });

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GenerateFeedbackLink_ShouldReturnBadRequest_WhenFormIdIsInvalid()
    {
        using var context = CreateContext();
        var controller = new CandidateEvaluationFormsController(context);

        var result = await controller.GenerateFeedbackLink(new GenerateFeedbackLinkRequest { FormId = 0 });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task FeedbackGet_ShouldReturnCandidateDetails_AndPreSelectedFeedbackType()
    {
        using var context = CreateContext();
        var form = new CandidateEvaluationForm
        {
            Email = "jane@example.com",
            FullName = "Jane Doe",
            PositionAppliedFor = "Full Stack Engineer"
        };
        context.CandidateEvaluationForms.Add(form);
        await context.SaveChangesAsync();

        var token = "tech-token-123";
        context.CandidateFeedbackTokens.Add(new CandidateFeedbackToken
        {
            CandidateEvaluationFormId = form.Id,
            Token = token,
            FeedbackType = "Technical"
        });
        await context.SaveChangesAsync();

        var controller = new CandidateEvaluationFormsController(context);
        var result = await controller.Feedback(token);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<InterviewerFeedbackViewModel>().Subject;
        model.IsValid.Should().BeTrue();
        model.CandidateFullName.Should().Be("Jane Doe");
        model.CandidateEmail.Should().Be("jane@example.com");
        model.PositionAppliedFor.Should().Be("Full Stack Engineer");
        model.SelectedFeedbackType.Should().Be("Technical");
        model.IsTypePreSelected.Should().BeTrue();
    }

    [Fact]
    public async Task FeedbackGet_ShouldAllowUnspecifiedFeedbackType()
    {
        using var context = CreateContext();
        var form = new CandidateEvaluationForm
        {
            Email = "john@example.com",
            FullName = "John Doe",
            PositionAppliedFor = "Product Manager"
        };
        context.CandidateEvaluationForms.Add(form);
        await context.SaveChangesAsync();

        var token = "any-type-token-456";
        context.CandidateFeedbackTokens.Add(new CandidateFeedbackToken
        {
            CandidateEvaluationFormId = form.Id,
            Token = token,
            FeedbackType = null
        });
        await context.SaveChangesAsync();

        var controller = new CandidateEvaluationFormsController(context);
        var result = await controller.Feedback(token);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<InterviewerFeedbackViewModel>().Subject;
        model.IsValid.Should().BeTrue();
        model.CandidateFullName.Should().Be("John Doe");
        model.SelectedFeedbackType.Should().BeNull();
        model.IsTypePreSelected.Should().BeFalse();
    }

    [Fact]
    public async Task FeedbackGet_ShouldReturnError_WhenTokenNotFound()
    {
        using var context = CreateContext();
        var controller = new CandidateEvaluationFormsController(context);

        var result = await controller.Feedback("non-existent-token");

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<InterviewerFeedbackViewModel>().Subject;
        model.IsValid.Should().BeFalse();
        model.ErrorMessage.Should().Contain("invalid or has expired");
    }

    [Fact]
    public async Task FeedbackGet_ShouldReturnError_WhenTokenAlreadyUsed()
    {
        using var context = CreateContext();
        var form = new CandidateEvaluationForm
        {
            Email = "jane@example.com",
            FullName = "Jane Doe",
            PositionAppliedFor = "Engineer"
        };
        context.CandidateEvaluationForms.Add(form);
        await context.SaveChangesAsync();

        var token = "used-token-789";
        context.CandidateFeedbackTokens.Add(new CandidateFeedbackToken
        {
            CandidateEvaluationFormId = form.Id,
            Token = token,
            IsUsed = true
        });
        await context.SaveChangesAsync();

        var controller = new CandidateEvaluationFormsController(context);
        var result = await controller.Feedback(token);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<InterviewerFeedbackViewModel>().Subject;
        model.IsValid.Should().BeFalse();
        model.ErrorMessage.Should().Contain("no longer available");
    }

    [Fact]
    public async Task FeedbackPost_ShouldSaveTechnicalCommentsAndSignature_WhenTechnicalFeedback()
    {
        using var context = CreateContext();
        var form = new CandidateEvaluationForm
        {
            Email = "jane@example.com",
            FullName = "Jane Doe",
            PositionAppliedFor = "Tech Lead"
        };
        context.CandidateEvaluationForms.Add(form);
        await context.SaveChangesAsync();

        var token = "tech-post-token";
        var tokenEntity = new CandidateFeedbackToken
        {
            CandidateEvaluationFormId = form.Id,
            Token = token,
            FeedbackType = "Technical"
        };
        context.CandidateFeedbackTokens.Add(tokenEntity);
        await context.SaveChangesAsync();

        var controller = new CandidateEvaluationFormsController(context);
        var model = new InterviewerFeedbackViewModel
        {
            Token = token,
            FormId = form.Id,
            SelectedFeedbackType = "Technical",
            ReviewerSignature = "Alice Smith (Lead)",
            Comments = "Strong knowledge of system architecture, excellent problem solving."
        };

        var result = await controller.Feedback(model);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("FeedbackSuccess");

        var updatedForm = await context.CandidateEvaluationForms.FindAsync(form.Id);
        updatedForm!.TechnicalComments.Should().Be("Strong knowledge of system architecture, excellent problem solving.");
        updatedForm.TechnicalReviewerSignature.Should().Be("Alice Smith (Lead)");

        var updatedToken = await context.CandidateFeedbackTokens.SingleAsync(t => t.Token == token);
        updatedToken.IsUsed.Should().BeTrue();
        updatedToken.UsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task FeedbackPost_ShouldSaveOtherComments_WhenPracticalFeedback()
    {
        using var context = CreateContext();
        var form = new CandidateEvaluationForm
        {
            Email = "john@example.com",
            FullName = "John Doe",
            PositionAppliedFor = "Frontend Developer"
        };
        context.CandidateEvaluationForms.Add(form);
        await context.SaveChangesAsync();

        var token = "practical-post-token";
        var tokenEntity = new CandidateFeedbackToken
        {
            CandidateEvaluationFormId = form.Id,
            Token = token,
            FeedbackType = "Practical"
        };
        context.CandidateFeedbackTokens.Add(tokenEntity);
        await context.SaveChangesAsync();

        var controller = new CandidateEvaluationFormsController(context);
        var model = new InterviewerFeedbackViewModel
        {
            Token = token,
            FormId = form.Id,
            SelectedFeedbackType = "Practical",
            ReviewerSignature = "Bob Jones (Interviewer)",
            Comments = "Completed the coding assignment with high attention to UI/UX details."
        };

        var result = await controller.Feedback(model);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("FeedbackSuccess");

        var updatedForm = await context.CandidateEvaluationForms.FindAsync(form.Id);
        updatedForm!.OtherComments.Should().Contain("Bob Jones (Interviewer)");
        updatedForm.OtherComments.Should().Contain("Completed the coding assignment with high attention to UI/UX details.");

        var updatedToken = await context.CandidateFeedbackTokens.SingleAsync(t => t.Token == token);
        updatedToken.IsUsed.Should().BeTrue();
        updatedToken.UsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task FeedbackPost_ShouldAllowUnspecifiedToken_ToSaveSelectedFeedbackType()
    {
        using var context = CreateContext();
        var form = new CandidateEvaluationForm
        {
            Email = "candidate@example.com",
            FullName = "Sam Miller",
            PositionAppliedFor = "DevOps"
        };
        context.CandidateEvaluationForms.Add(form);
        await context.SaveChangesAsync();

        var token = "unspecified-type-token";
        var tokenEntity = new CandidateFeedbackToken
        {
            CandidateEvaluationFormId = form.Id,
            Token = token,
            FeedbackType = null
        };
        context.CandidateFeedbackTokens.Add(tokenEntity);
        await context.SaveChangesAsync();

        var controller = new CandidateEvaluationFormsController(context);
        var model = new InterviewerFeedbackViewModel
        {
            Token = token,
            FormId = form.Id,
            SelectedFeedbackType = "Technical",
            ReviewerSignature = "Dave DevOps",
            Comments = "Excellent Kubernetes and CI/CD understanding."
        };

        var result = await controller.Feedback(model);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("FeedbackSuccess");

        var updatedForm = await context.CandidateEvaluationForms.FindAsync(form.Id);
        updatedForm!.TechnicalComments.Should().Be("Excellent Kubernetes and CI/CD understanding.");
        updatedForm.TechnicalReviewerSignature.Should().Be("Dave DevOps");

        var updatedToken = await context.CandidateFeedbackTokens.SingleAsync(t => t.Token == token);
        updatedToken.IsUsed.Should().BeTrue();
        updatedToken.FeedbackType.Should().Be("Technical");
    }

    [Fact]
    public async Task FeedbackPost_ShouldReject_WhenRequiredFieldsMissing()
    {
        using var context = CreateContext();
        var form = new CandidateEvaluationForm
        {
            Email = "candidate@example.com",
            FullName = "Test Candidate",
            PositionAppliedFor = "QA"
        };
        context.CandidateEvaluationForms.Add(form);
        await context.SaveChangesAsync();

        var token = "missing-fields-token";
        context.CandidateFeedbackTokens.Add(new CandidateFeedbackToken
        {
            CandidateEvaluationFormId = form.Id,
            Token = token,
            FeedbackType = "Technical"
        });
        await context.SaveChangesAsync();

        var controller = new CandidateEvaluationFormsController(context);
        var model = new InterviewerFeedbackViewModel
        {
            Token = token,
            FormId = form.Id,
            SelectedFeedbackType = "Technical",
            ReviewerSignature = "", // Missing
            Comments = "" // Missing
        };

        var result = await controller.Feedback(model);

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
        controller.ModelState.ContainsKey(nameof(model.ReviewerSignature)).Should().BeTrue();
        controller.ModelState.ContainsKey(nameof(model.Comments)).Should().BeTrue();

        var notUpdatedToken = await context.CandidateFeedbackTokens.SingleAsync(t => t.Token == token);
        notUpdatedToken.IsUsed.Should().BeFalse();
    }

    [Fact]
    public async Task FeedbackPost_ShouldReject_WhenFeedbackTypeUnspecifiedAndNotSelected()
    {
        using var context = CreateContext();
        var form = new CandidateEvaluationForm
        {
            Email = "candidate@example.com",
            FullName = "Test Candidate",
            PositionAppliedFor = "QA"
        };
        context.CandidateEvaluationForms.Add(form);
        await context.SaveChangesAsync();

        var token = "no-type-selected-token";
        context.CandidateFeedbackTokens.Add(new CandidateFeedbackToken
        {
            CandidateEvaluationFormId = form.Id,
            Token = token,
            FeedbackType = null
        });
        await context.SaveChangesAsync();

        var controller = new CandidateEvaluationFormsController(context);
        var model = new InterviewerFeedbackViewModel
        {
            Token = token,
            FormId = form.Id,
            SelectedFeedbackType = null, // Neither Technical nor Practical
            ReviewerSignature = "Interviewer",
            Comments = "Some comments"
        };

        var result = await controller.Feedback(model);

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
        controller.ModelState.ContainsKey(nameof(model.SelectedFeedbackType)).Should().BeTrue();
    }

    [Fact]
    public async Task FeedbackSuccessGet_ShouldReturnCandidateDetails()
    {
        using var context = CreateContext();
        var form = new CandidateEvaluationForm
        {
            Email = "candidate@example.com",
            FullName = "Alex Smith",
            PositionAppliedFor = "Product Designer"
        };
        context.CandidateEvaluationForms.Add(form);
        await context.SaveChangesAsync();

        var token = "success-token-123";
        context.CandidateFeedbackTokens.Add(new CandidateFeedbackToken
        {
            CandidateEvaluationFormId = form.Id,
            Token = token,
            FeedbackType = "Practical",
            IsUsed = true,
            UsedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = new CandidateEvaluationFormsController(context);
        var result = await controller.FeedbackSuccess(token);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<FeedbackSuccessViewModel>().Subject;
        model.CandidateFullName.Should().Be("Alex Smith");
        model.PositionAppliedFor.Should().Be("Product Designer");
        model.FeedbackType.Should().Be("Practical");
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
