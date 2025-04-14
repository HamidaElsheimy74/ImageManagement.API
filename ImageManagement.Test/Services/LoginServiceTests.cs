using ImageManagement.BLL.Models;
using ImageManagement.Common.Errors;
using ImageManagement.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ImageManagement.BLL.Services.Tests;

[TestClass()]
public class LoginServiceTests
{

    private IConfiguration _config;
    private Mock<IAuthService> _mockAuthService;
    private Mock<ILogger<LoginService>> _mockLogger;
    private const string ValidUsername = "admin@admin.com";
    private const string ValidPassword = "secure!Password123";
    private const string TestToken = "test.jwt.token";
    LoginService _loginService;
    public LoginServiceTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["LoginData:UserName"] = ValidUsername,
                ["LoginData:Password"] = ValidPassword
            })
            .Build();
        _mockAuthService = new Mock<IAuthService>();
        _mockLogger = new Mock<ILogger<LoginService>>();
        var loginData = new LoginData
        {
            UserName = ValidUsername,
            Password = ValidPassword
        };

        _mockAuthService.Setup(x => x.GenerateJwtToken(ValidUsername, _config))
                     .ReturnsAsync(TestToken);
        _loginService = new LoginService(_mockLogger.Object, _config, _mockAuthService.Object);
    }

    [TestMethod]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Act
        var result = await _loginService.LoginAsync(ValidUsername, ValidPassword);

        // Assert
        Assert.AreEqual(StatusCodes.Status200OK, result.StatusCode);
        Assert.AreEqual(TestToken, result.Data);
        Assert.IsTrue(string.IsNullOrEmpty(result.Message));
    }

    [TestMethod]
    public async Task LoginAsync_InvalidUsername_ReturnsUnauthorized()
    {
        // Act
        var result = await _loginService.LoginAsync("wrongUser", ValidPassword);

        // Assert
        Assert.AreEqual(StatusCodes.Status401Unauthorized, result.StatusCode);
        Assert.AreEqual(ErrorsHandler.Invalid_Credentials, result.Message);
        Assert.IsNull(result.Data);
    }

    [TestMethod]
    public async Task LoginAsync_InvalidPassword_ReturnsUnauthorized()
    {
        // Act
        var result = await _loginService.LoginAsync(ValidUsername, "wrongPassword");

        // Assert
        Assert.AreEqual(StatusCodes.Status401Unauthorized, result.StatusCode);
        Assert.AreEqual(ErrorsHandler.Invalid_Credentials, result.Message);
        Assert.IsNull(result.Data);
    }

    [TestMethod]
    public async Task LoginAsync_MissingLoginConfig_ReturnsServerError()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().Build();

        _loginService = new LoginService(_mockLogger.Object, emptyConfig, _mockAuthService.Object);

        // Act
        var result = await _loginService.LoginAsync(ValidUsername, ValidPassword);

        // Assert
        Assert.AreEqual(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.AreEqual(ErrorsHandler.Internal_Server_Error, result.Message);

    }

    [TestMethod]
    public async Task LoginAsync_TokenGenerationFails_ReturnsServerError()
    {
        // Arrange
        _mockAuthService.Setup(x => x.GenerateJwtToken(ValidUsername, _config))
                       .ThrowsAsync(new Exception("Token generation failed"));

        // Act
        var result = await _loginService.LoginAsync(ValidUsername, ValidPassword);

        // Assert
        Assert.AreEqual(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.AreEqual(ErrorsHandler.Internal_Server_Error, result.Message);
    }

    [TestMethod]
    public async Task LoginAsync_NullUsername_ReturnsUnauthorizedt()
    {
        // Act
        var result = await _loginService.LoginAsync(null!, ValidPassword);

        // Assert
        Assert.AreEqual(StatusCodes.Status401Unauthorized, result.StatusCode);
        Assert.IsNotNull(result.Message);
    }

    [TestMethod]
    public async Task LoginAsync_EmptyPassword_ReturnsUnauthorized()
    {
        // Act
        var result = await _loginService.LoginAsync(ValidUsername, "");

        // Assert
        Assert.AreEqual(StatusCodes.Status401Unauthorized, result.StatusCode);
        Assert.IsNotNull(result.Message);
    }

}