using ImageManagement.API.Controllers;
using ImageManagement.API.DTOs;
using ImageManagement.BLL.Interfaces;
using ImageManagement.Common.DTOs;
using ImageManagement.Common.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel.DataAnnotations;

namespace ImageManagement.API.Validators.Tests;

[TestClass()]
public class MaxFileCountAttributeTests
{
    private readonly Mock<IImageService> _mockImageService;
    private readonly Mock<ILogger<ImagesController>> _mockLogger;
    private readonly ImagesController _controller;

    public MaxFileCountAttributeTests()
    {
        _mockImageService = new Mock<IImageService>();
        _mockLogger = new Mock<ILogger<ImagesController>>();
        _controller = new ImagesController(_mockImageService.Object, _mockLogger.Object);
    }
    [TestMethod]
    public async Task UploadImages_ValidFileCount_ReturnsOk()
    {
        // Arrange
        var files = new List<IFormFile>
        {
            new FormFile(null, 0, 10, "file1", "file1.jpg"),
            new FormFile(null, 0, 10, "file2", "file2.jpg")
        };
        var request = new UploadImagesRequest { Files = files };

        _mockImageService
            .Setup(x => x.ProcessAndStoreImageAsync(files))
            .ReturnsAsync(new List<ResponseResult>());

        // Act
        var result = await _controller.UploadImages(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public async Task UploadImages_ExceedsMaxFileCount_ReturnsBadRequest()
    {
        // Arrange
        var files = new List<IFormFile>();
        for (int i = 0; i < 6; i++)
        {
            files.Add(new FormFile(null, 0, 10, $"file{i}", $"file{i}.jpg"));
        }
        var request = new UploadImagesRequest { Files = files };

        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

        _controller.ModelState.Clear();
        if (!isValid)
        {
            foreach (var validationResult in validationResults)
            {
                _controller.ModelState.AddModelError("Files", validationResult.ErrorMessage);
            }
        }

        // Act
        var result = await _controller.UploadImages(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = (BadRequestObjectResult)result;
        var responseResult = (ResponseResult)badRequestResult.Value;
        StringAssert.Contains(responseResult.Message, ErrorsHandler.Invalid_UploadModel);
    }

    [TestMethod]
    public async Task UploadImages_NoFiles_ReturnsBadRequest()
    {
        // Arrange
        var request = new UploadImagesRequest { Files = null };

        // Act
        var result = await _controller.UploadImages(request);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = (BadRequestObjectResult)result;
        var responseResult = (ResponseResult)badRequestResult.Value;
        Assert.AreEqual(ErrorsHandler.Empty_Files_List, responseResult.Message);
    }
}