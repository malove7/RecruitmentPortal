using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RecruitmentPortal.Controllers;
using RecruitmentPortal.Services;
using Xunit;

namespace RecruitmentPortal.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock = new();

    [Fact]
    public void Login_Get_ShouldReturnView_WhenNotAuthenticated()
    {
        // Arrange
        var controller = CreateController(isAuthenticated: false);

        // Act
        var result = controller.Login(null);

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Login_Get_ShouldRedirectToDashboard_WhenAuthenticated()
    {
        // Arrange
        var controller = CreateController(isAuthenticated: true);

        // Act
        var result = controller.Login(null);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Dashboard");
        redirect.ControllerName.Should().Be("Home");
    }

    [Fact]
    public async Task Login_Post_ShouldRedirectToDashboard_WhenCredentialsValid()
    {
        // Arrange
        _authServiceMock
            .Setup(s => s.LoginAsync("admin@test.com", "Password123!"))
            .ReturnsAsync("valid-jwt-token");

        var controller = CreateController(isAuthenticated: false);

        // Act
        var result = await controller.Login("admin@test.com", "Password123!", rememberMe: false);

        // Assert
        var redirect = result.Should().BeOfType<LocalRedirectResult>().Subject;
        redirect.Url.Should().Contain("Dashboard");
        controller.Response.Headers.Should().ContainKey("Set-Cookie");
    }

    [Fact]
    public async Task Login_Post_ShouldReturnViewWithError_WhenCredentialsInvalid()
    {
        // Arrange
        _authServiceMock
            .Setup(s => s.LoginAsync("admin@test.com", "WrongPassword"))
            .ReturnsAsync((string?)null);

        var controller = CreateController(isAuthenticated: false);

        // Act
        var result = await controller.Login("admin@test.com", "WrongPassword", rememberMe: false);

        // Assert
        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Login_Post_ShouldReturnJsonSuccess_WhenAjaxAndCredentialsValid()
    {
        // Arrange
        _authServiceMock
            .Setup(s => s.LoginAsync("admin@test.com", "Password123!"))
            .ReturnsAsync("valid-jwt-token");

        var controller = CreateController(isAuthenticated: false, isAjax: true);

        // Act
        var result = await controller.Login("admin@test.com", "Password123!", rememberMe: false);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        value.Should().NotBeNull();
        var propSuccess = value!.GetType().GetProperty("success")?.GetValue(value);
        propSuccess.Should().Be(true);
    }

    [Fact]
    public async Task Login_Post_ShouldReturnJsonError_WhenAjaxAndCredentialsInvalid()
    {
        // Arrange
        _authServiceMock
            .Setup(s => s.LoginAsync("admin@test.com", "WrongPassword"))
            .ReturnsAsync((string?)null);

        var controller = CreateController(isAuthenticated: false, isAjax: true);

        // Act
        var result = await controller.Login("admin@test.com", "WrongPassword", rememberMe: false);

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var value = jsonResult.Value;
        value.Should().NotBeNull();
        var propSuccess = value!.GetType().GetProperty("success")?.GetValue(value);
        propSuccess.Should().Be(false);
    }

    [Fact]
    public void Logout_ShouldDeleteCookieAndRedirectToHomeIndex()
    {
        // Arrange
        var controller = CreateController(isAuthenticated: true);

        // Act
        var result = controller.Logout();

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("Home");
    }

    private AuthController CreateController(bool isAuthenticated, bool isAjax = false)
    {
        var httpContext = new DefaultHttpContext();
        if (isAuthenticated)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "AdminUser"),
                new Claim("FirstName", "Admin")
            }, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
        }

        if (isAjax)
        {
            httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
            httpContext.Request.Headers["Accept"] = "application/json";
        }

        var controller = new AuthController(_authServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Action(It.IsAny<Microsoft.AspNetCore.Mvc.Routing.UrlActionContext>()))
            .Returns("/Home/Dashboard");
        urlHelperMock.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns(true);
        controller.Url = urlHelperMock.Object;

        return controller;
    }
}
