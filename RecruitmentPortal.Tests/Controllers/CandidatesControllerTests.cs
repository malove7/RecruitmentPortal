using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Moq;
using RecruitmentPortal.Controllers;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;
using Xunit;

namespace RecruitmentPortal.Tests.Controllers;

public class CandidatesControllerTests
{
    [Fact]
    public async Task Index_ShouldReturnAllCandidates_WhenNoFiltersProvided()
    {
        // Arrange
        using var context = CreateContext();
        SeedJobPositions(context);
        context.Candidates.AddRange(
            BuildCandidate(1, "John", "Doe", "john@acme.com", 1),
            BuildCandidate(2, "Jane", "Smith", "jane@acme.com", 2));
        await context.SaveChangesAsync();

        var controller = new CandidatesController(context);

        // Act
        var result = await controller.Index(null!, null);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<Candidate>>().Subject;
        model.Should().HaveCount(2);
        controller.ViewData["CurrentFilter"].Should().BeNull();
        controller.ViewData["CurrentJobPosition"].Should().BeNull();
        controller.ViewData["JobPositions"].Should().BeOfType<SelectList>();
    }

    [Fact]
    public async Task Index_ShouldApplySearchAndJobPositionFilters()
    {
        // Arrange
        using var context = CreateContext();
        SeedJobPositions(context);
        context.Candidates.AddRange(
            BuildCandidate(1, "John", "Doe", "john@acme.com", 1),
            BuildCandidate(2, "Johnny", "Walker", "johnny@acme.com", 2),
            BuildCandidate(3, "Mary", "Jane", "mary@acme.com", 1));
        await context.SaveChangesAsync();

        var controller = new CandidatesController(context);

        // Act
        var result = await controller.Index("John", 1);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<Candidate>>().Subject;
        model.Should().ContainSingle();
        model[0].FirstName.Should().Be("John");
        controller.ViewData["CurrentFilter"].Should().Be("John");
        controller.ViewData["CurrentJobPosition"].Should().Be(1);
    }

    [Fact]
    public async Task Details_ShouldReturnNotFound_WhenIdIsNull()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new CandidatesController(context);

        // Act
        var result = await controller.Details(null);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Details_ShouldReturnNotFound_WhenCandidateDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new CandidatesController(context);

        // Act
        var result = await controller.Details(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Details_ShouldReturnView_WhenCandidateExists()
    {
        // Arrange
        using var context = CreateContext();
        SeedJobPositions(context);
        var candidate = BuildCandidate(1, "John", "Doe", "john@acme.com", 1);
        candidate.Notes = new List<CandidateNote> { new() { NoteText = "Initial note" } };
        context.Candidates.Add(candidate);
        await context.SaveChangesAsync();

        var controller = new CandidatesController(context);

        // Act
        var result = await controller.Details(1);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<Candidate>().Subject;
        model.Id.Should().Be(1);
        model.Notes.Should().NotBeNull();
    }

    [Fact]
    public void Create_Get_ShouldReturnViewAndSetJobPositions()
    {
        // Arrange
        using var context = CreateContext();
        SeedJobPositions(context);
        var controller = new CandidatesController(context);

        // Act
        var result = controller.Create();

        // Assert
        result.Should().BeOfType<ViewResult>();
        controller.ViewData["JobPositionId"].Should().BeOfType<SelectList>();
    }

    [Fact]
    public async Task Create_Post_ShouldReturnView_WhenModelStateIsInvalid()
    {
        // Arrange
        using var context = CreateContext();
        SeedJobPositions(context);
        var controller = new CandidatesController(context);
        controller.ModelState.AddModelError("Email", "Email is required");

        var candidate = BuildCandidate(0, "John", "Doe", "bad-email", 1);

        // Act
        var result = await controller.Create(candidate, null);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(candidate);
        context.Candidates.Should().BeEmpty();
        controller.ViewData["JobPositionId"].Should().BeOfType<SelectList>();
    }

    [Fact]
    public async Task Create_Post_ShouldPersistCandidateAndRedirect_WhenModelIsValid()
    {
        // Arrange
        using var context = CreateContext();
        SeedJobPositions(context);
        var controller = new CandidatesController(context);

        var candidate = BuildCandidate(0, "John", "Doe", "john@acme.com", 1);
        candidate.NewNote = "Strong communication";

        // Act
        var result = await controller.Create(candidate, null);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be(nameof(CandidatesController.Index));

        var created = context.Candidates.Include(c => c.Notes).Single();
        created.Status.Should().Be(CandidateStatus.Applied);
        created.AppliedDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        created.Notes.Should().ContainSingle();
        created.Notes!.Single().NoteText.Should().Be("Strong communication");
    }

    [Fact]
    public async Task Create_Post_ShouldStoreResumePath_WhenResumeFileProvided()
    {
        // Arrange
        using var context = CreateContext();
        SeedJobPositions(context);
        var controller = new CandidatesController(context);
        var candidate = BuildCandidate(0, "John", "Doe", "john@acme.com", 1);

        var previousCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), $"rp-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        Directory.SetCurrentDirectory(tempRoot);

        var fileMock = new Mock<IFormFile>();
        fileMock.SetupGet(x => x.Length).Returns(32);
        fileMock.SetupGet(x => x.FileName).Returns("resume.pdf");
        fileMock
            .Setup(x => x.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, CancellationToken>((stream, _) => stream.WriteAsync(new byte[] { 1, 2, 3, 4 }, 0, 4));

        try
        {
            // Act
            var result = await controller.Create(candidate, fileMock.Object);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var persisted = context.Candidates.Single();
            persisted.ResumePath.Should().NotBeNullOrWhiteSpace();
            persisted.ResumePath.Should().StartWith("/uploads/");

            var fileName = persisted.ResumePath!.Replace("/uploads/", string.Empty);
            var savedPath = Path.Combine(tempRoot, "wwwroot", "uploads", fileName);
            File.Exists(savedPath).Should().BeTrue();
        }
        finally
        {
            Directory.SetCurrentDirectory(previousCurrentDirectory);
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    [Fact]
    public async Task Edit_Get_ShouldReturnNotFound_WhenIdIsNull()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new CandidatesController(context);

        // Act
        var result = await controller.Edit(null);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Get_ShouldReturnNotFound_WhenCandidateDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new CandidatesController(context);

        // Act
        var result = await controller.Edit(55);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Get_ShouldReturnView_WhenCandidateExists()
    {
        // Arrange
        using var context = CreateContext();
        SeedJobPositions(context);
        context.Candidates.Add(BuildCandidate(1, "John", "Doe", "john@acme.com", 1));
        await context.SaveChangesAsync();
        var controller = new CandidatesController(context);

        // Act
        var result = await controller.Edit(1);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<Candidate>().Subject;
        model.Id.Should().Be(1);
        controller.ViewData["JobPositionId"].Should().BeOfType<SelectList>();
    }

    [Fact]
    public async Task Edit_Post_ShouldReturnNotFound_WhenRouteIdDoesNotMatchModelId()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new CandidatesController(context);
        var candidate = BuildCandidate(2, "John", "Doe", "john@acme.com", 1);

        // Act
        var result = await controller.Edit(1, candidate);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Post_ShouldReturnView_WhenModelStateIsInvalid()
    {
        // Arrange
        using var context = CreateContext();
        SeedJobPositions(context);
        var controller = new CandidatesController(context);
        controller.ModelState.AddModelError("FirstName", "First Name is required");
        var candidate = BuildCandidate(1, "John", "Doe", "john@acme.com", 1);

        // Act
        var result = await controller.Edit(1, candidate);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(candidate);
        controller.ViewData["JobPositionId"].Should().BeOfType<SelectList>();
    }

    [Fact]
    public async Task Edit_Post_ShouldUpdateCandidateAndRedirect_WhenValid()
    {
        // Arrange
        using var context = CreateContext();
        SeedJobPositions(context);
        context.Candidates.Add(BuildCandidate(1, "Old", "Name", "old@acme.com", 1));
        await context.SaveChangesAsync();
        var controller = new CandidatesController(context);

        var updated = BuildCandidate(1, "New", "Name", "new@acme.com", 2);
        updated.CurrentLocation = "Remote";
        updated.NewNote = "Updated profile";

        // Act
        var result = await controller.Edit(1, updated);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be(nameof(CandidatesController.Index));

        var persisted = context.Candidates.Single(c => c.Id == 1);
        persisted.FirstName.Should().Be("New");
        persisted.LastName.Should().Be("Name");
        persisted.Email.Should().Be("new@acme.com");
        persisted.JobPositionId.Should().Be(2);
        persisted.CurrentLocation.Should().Be("Remote");

        context.CandidateNotes.Should().ContainSingle(n => n.CandidateId == 1 && n.NoteText == "Updated profile");
    }

    [Fact]
    public async Task Edit_Post_ShouldReturnNotFound_WhenCandidateIsMissingInDatabase()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new CandidatesController(context);
        var candidate = BuildCandidate(1, "John", "Doe", "john@acme.com", 1);

        // Act
        var result = await controller.Edit(1, candidate);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Post_ShouldReturnNotFound_WhenConcurrencyExceptionAndCandidateNoLongerExists()
    {
        // Arrange
        var options = BuildOptions();
        using (var seedContext = new ApplicationDbContext(options))
        {
            SeedJobPositions(seedContext);
            seedContext.Candidates.Add(BuildCandidate(1, "John", "Doe", "john@acme.com", 1));
            await seedContext.SaveChangesAsync();
        }

        await using var context = new ThrowingSaveChangesContext(options, removeCandidatesBeforeThrow: true);
        var controller = new CandidatesController(context);
        var candidate = BuildCandidate(1, "Updated", "Candidate", "updated@acme.com", 1);

        // Act
        var result = await controller.Edit(1, candidate);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edit_Post_ShouldRethrow_WhenConcurrencyExceptionAndCandidateStillExists()
    {
        // Arrange
        var options = BuildOptions();
        using (var seedContext = new ApplicationDbContext(options))
        {
            SeedJobPositions(seedContext);
            seedContext.Candidates.Add(BuildCandidate(1, "John", "Doe", "john@acme.com", 1));
            await seedContext.SaveChangesAsync();
        }

        await using var context = new ThrowingSaveChangesContext(options, removeCandidatesBeforeThrow: false);
        var controller = new CandidatesController(context);
        var candidate = BuildCandidate(1, "Updated", "Candidate", "updated@acme.com", 1);

        // Act
        Func<Task> act = async () => await controller.Edit(1, candidate);

        // Assert
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task Delete_Get_ShouldReturnNotFound_WhenIdIsNull()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new CandidatesController(context);

        // Act
        var result = await controller.Delete(null);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_Get_ShouldReturnNotFound_WhenCandidateDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new CandidatesController(context);

        // Act
        var result = await controller.Delete(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_Get_ShouldReturnView_WhenCandidateExists()
    {
        // Arrange
        using var context = CreateContext();
        SeedJobPositions(context);
        context.Candidates.Add(BuildCandidate(1, "John", "Doe", "john@acme.com", 1));
        await context.SaveChangesAsync();
        var controller = new CandidatesController(context);

        // Act
        var result = await controller.Delete(1);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<Candidate>().Subject;
        model.Id.Should().Be(1);
    }

    [Fact]
    public async Task DeleteConfirmed_ShouldRemoveCandidateAndRedirect_WhenCandidateExists()
    {
        // Arrange
        using var context = CreateContext();
        SeedJobPositions(context);
        context.Candidates.Add(BuildCandidate(1, "John", "Doe", "john@acme.com", 1));
        await context.SaveChangesAsync();
        var controller = new CandidatesController(context);

        // Act
        var result = await controller.DeleteConfirmed(1);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be(nameof(CandidatesController.Index));
        context.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteConfirmed_ShouldStillRedirect_WhenCandidateDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new CandidatesController(context);

        // Act
        var result = await controller.DeleteConfirmed(123);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be(nameof(CandidatesController.Index));
        context.Candidates.Should().BeEmpty();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = BuildOptions();
        return new ApplicationDbContext(options);
    }

    private static DbContextOptions<ApplicationDbContext> BuildOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private static void SeedJobPositions(ApplicationDbContext context)
    {
        if (context.JobPositions.Any())
        {
            return;
        }

        context.JobPositions.AddRange(
            new JobPosition { Id = 1, Title = "Backend Engineer", Description = "Build APIs", Requirements = ".NET" },
            new JobPosition { Id = 2, Title = "Frontend Engineer", Description = "Build UI", Requirements = "React" });
        context.SaveChanges();
    }

    private static Candidate BuildCandidate(int id, string firstName, string lastName, string email, int jobPositionId)
    {
        return new Candidate
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = "9999999999",
            JobPositionId = jobPositionId
        };
    }

    private sealed class ThrowingSaveChangesContext : ApplicationDbContext
    {
        private readonly bool _removeCandidatesBeforeThrow;

        public ThrowingSaveChangesContext(
            DbContextOptions<ApplicationDbContext> options,
            bool removeCandidatesBeforeThrow)
            : base(options)
        {
            _removeCandidatesBeforeThrow = removeCandidatesBeforeThrow;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_removeCandidatesBeforeThrow)
            {
                Candidates.RemoveRange(Candidates.ToList());
                base.SaveChanges();
            }

            throw new DbUpdateConcurrencyException("Simulated concurrency conflict");
        }
    }
}
