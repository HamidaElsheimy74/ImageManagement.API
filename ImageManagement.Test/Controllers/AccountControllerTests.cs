using ImageManagement.BLL.Interfaces;
using ImageManagement.BLL.Models;
using ImageManagement.Common.DTOs;
using ImageManagement.Common.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ImageManagement.API.Controllers.Tests;

[TestClass()]
public class AccountControllerTests
{
    private readonly Mock<ILoginService> _loginService;
    private readonly Mock<ILogger<AccountController>> _logger;
    private readonly AccountController _accountController;
    string emailErrorMessage = "Invalid email format";
    string passwordErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character.";
    string userNullError = "The UserName field is required";
    string passwordNullError = "The Password field is required";
    public AccountControllerTests()
    {
        _logger = new Mock<ILogger<AccountController>>();
        _loginService = new Mock<ILoginService>();
        _accountController = new AccountController(_loginService.Object, _logger.Object);
    }

    [TestMethod()]
    public async Task Login_NullLoginModel_Return404()
    {
        // Arrange

        //Act
        var response = await _accountController.Login(null!);

        //Assert
        Assert.IsInstanceOfType(response, typeof(BadRequestObjectResult));
        Assert.AreEqual(400, ((BadRequestObjectResult)response).StatusCode);
        var ResponseResult = (ResponseResult)((BadRequestObjectResult)response).Value!;
        StringAssert.Contains(ResponseResult.Message, ErrorsHandler.Invalid_LoginModel);
    }


    [TestMethod()]
    public async Task Login_EmptyUserName_Return404()
    {

        // Arrange
        var loginModel = new LoginData
        {
            UserName = "",
            Password = "ValidPass1!"
        };
        _accountController.ModelState.AddModelError("UserName", emailErrorMessage);
        //Act
        var response = await _accountController.Login(loginModel);

        //Assert
        Assert.IsInstanceOfType(response, typeof(BadRequestObjectResult));
        Assert.AreEqual(400, ((BadRequestObjectResult)response).StatusCode);
        var ResponseResult = (ResponseResult)((BadRequestObjectResult)response).Value!;
    }


    [TestMethod]
    public async Task Login_InvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var invalidModel = new LoginData
        {
            UserName = "notanemail",
            Password = "ValidPass1!"
        };
        _accountController.ModelState.AddModelError("UserName", emailErrorMessage);

        // Act
        var result = await _accountController.Login(invalidModel);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.AreEqual(400, badRequestResult.StatusCode);
        var response = badRequestResult.Value as ResponseResult;
        StringAssert.Contains(response.Message, ErrorsHandler.Invalid_LoginModel);
    }

    [TestMethod]
    public async Task Login_EmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var invalidModel = new LoginData
        {
            UserName = "hhh@hhh.hh",
            Password = ""
        };
        _accountController.ModelState.AddModelError("Password", passwordErrorMessage);

        // Act
        var result = await _accountController.Login(invalidModel);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.AreEqual(400, badRequestResult.StatusCode);

    }

    [TestMethod]
    public async Task Login_NullUserName_ReturnsBadRequest()
    {
        // Arrange
        var invalidModel = new LoginData
        {
            UserName = null,
            Password = "ValidPass1!"
        };
        _accountController.ModelState.AddModelError("UserName", userNullError);

        // Act
        var result = await _accountController.Login(invalidModel);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.AreEqual(400, badRequestResult.StatusCode);
    }

    [TestMethod]
    public async Task Login_UserNameWithSpaces_ReturnsBadRequest()
    {
        // Arrange
        var invalidModel = new LoginData
        {
            UserName = "  ",
            Password = "ValidPass1!"
        };
        _accountController.ModelState.AddModelError("UserName", userNullError);

        // Act
        var result = await _accountController.Login(invalidModel);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.AreEqual(400, badRequestResult.StatusCode);
    }

    [TestMethod]
    public async Task Login_InvalidEmailRegex_ReturnsBadRequest()
    {
        // Arrange
        var invalidModels = new[]
        {
            new LoginData { UserName = "missing@domain", Password = "ValidPass1!" },
            new LoginData { UserName = "missingat.com", Password = "ValidPass1!" },
            new LoginData { UserName = "@missinglocal.com", Password = "ValidPass1!" },
            new LoginData { UserName = "invalid@.com", Password = "ValidPass1!" }
        };

        foreach (var model in invalidModels)
        {
            _accountController.ModelState.Clear();
            _accountController.ModelState.AddModelError("UserName", emailErrorMessage);

            // Act
            var result = await _accountController.Login(model);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.AreEqual(400, badRequestResult.StatusCode);
        }
    }

    [TestMethod]
    public async Task Login_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var invalidModel = new LoginData();
        _accountController.ModelState.AddModelError("UserName", userNullError);
        _accountController.ModelState.AddModelError("Password", passwordNullError);

        // Act
        var result = await _accountController.Login(invalidModel);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.AreEqual(400, badRequestResult.StatusCode);

    }

    [TestMethod]
    public async Task Login_InvalidPasswordFormat_ReturnsBadRequest()
    {
        // Arrange
        var invalidModel = new LoginData
        {
            UserName = "hh@hh.hh",
            Password = "notvalidpass"
        };
        _accountController.ModelState.AddModelError("Password", passwordErrorMessage);

        // Act
        var result = await _accountController.Login(invalidModel);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.AreEqual(400, badRequestResult.StatusCode);
        var response = badRequestResult.Value as ResponseResult;
        StringAssert.Contains(response.Message, ErrorsHandler.Invalid_LoginModel);
    }

    [TestMethod]
    public async Task Login_NullPassword_ReturnsBadRequest()
    {
        // Arrange
        var invalidModel = new LoginData
        {
            UserName = "hh@hh.hh",
            Password = null
        };
        _accountController.ModelState.AddModelError("Password", passwordNullError);

        // Act
        var result = await _accountController.Login(invalidModel);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.AreEqual(400, badRequestResult.StatusCode);
    }

    [TestMethod]
    public async Task Login_PasswordWithSpaces_ReturnsBadRequest()
    {
        // Arrange
        var invalidModel = new LoginData
        {
            UserName = "hhh@hh.hh",
            Password = "  "
        };
        _accountController.ModelState.AddModelError("Password", passwordNullError);

        // Act
        var result = await _accountController.Login(invalidModel);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.AreEqual(400, badRequestResult.StatusCode);
    }

    [TestMethod]
    public async Task Login_InvalidPasswordRegex_ReturnsBadRequest()
    {
        // Arrange
        var invalidModels = new[]
        {
            new LoginData { UserName = "hhh@domain.com", Password = "invalidpass!" },
            new LoginData { UserName = "hhh@hh.com", Password = "ValidPass1" },
            new LoginData { UserName = "hh@hhhhh.com", Password = "ValidPass!" },
        };

        foreach (var model in invalidModels)
        {
            _accountController.ModelState.Clear();
            _accountController.ModelState.AddModelError("UserName", emailErrorMessage);

            // Act
            var result = await _accountController.Login(model);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.AreEqual(400, badRequestResult.StatusCode);
        }
    }



    [TestMethod]
    public async Task Login_whenLoginPasses_ReturnsSuccessWithToken()
    {
        // Arrange
        var validModel = new LoginData
        {
            UserName = "hhh@hh.hh",
            Password = "P@ssw0rd"
        };
        _loginService.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(new ResponseResult
        {
            StatusCode = 200,
            Data = "token"
        });

        // Act
        var result = await _accountController.Login(validModel);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var okResult = (OkObjectResult)result;
        Assert.AreEqual(200, okResult.StatusCode);
        var ResponseResult = (ResponseResult)((OkObjectResult)result).Value!;
        StringAssert.Contains(ResponseResult.Data.ToString(), "token");

    }
}