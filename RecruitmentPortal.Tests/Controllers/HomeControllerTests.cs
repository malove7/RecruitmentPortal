using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RecruitmentPortal.Controllers;
using RecruitmentPortal.Data;
using RecruitmentPortal.Models;
using RecruitmentPortal.Models.ViewModels;
using Xunit;

namespace RecruitmentPortal.Tests.Controllers;

public class HomeControllerTests
{
    private readonly Mock<ILogger<HomeController>> _loggerMock = new();

    [Fact]
    public void Index_ShouldReturnView()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new HomeController(_loggerMock.Object, context);

        // Act
        var result = controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Dashboard_ShouldReturnViewWithDashboardViewModel()
    {
        // Arrange
        using var context = CreateContext();
        context.JobPositions.Add(new JobPosition { Id = 1, Title = "Software Engineer", Description = "Desc", Requirements = "Req", IsActive = true });
        context.Candidates.Add(new Candidate { Id = 1, FirstName = "Alice", LastName = "Smith", Email = "alice@example.com", Phone = "1234567890", JobPositionId = 1, AppliedDate = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = new HomeController(_loggerMock.Object, context);

        // Act
        var result = await controller.Dashboard();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<DashboardViewModel>().Subject;
        model.TotalCandidates.Should().Be(1);
        model.TotalOpenPositions.Should().Be(1);
        model.RecentCandidates.Should().HaveCount(1);
    }

    [Fact]
    public void Privacy_ShouldReturnView()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new HomeController(_loggerMock.Object, context);

        // Act
        var result = controller.Privacy();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
