using ImageManagement.API.DTOs;
using ImageManagement.BLL.Interfaces;
using ImageManagement.Common.DTOs;
using ImageManagement.Common.Errors;
using ImageManagement.Domain.Entities;
using ImageManagement.Test.TestUtilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;

namespace ImageManagement.API.Controllers.Tests;

[TestClass()]
public class ImagesControllerTests
{
    private Mock<IImageService> _imageService = new();
    private Mock<ILogger<ImagesController>> _logger = new();
    private UploadImagesRequest _uploadImagesRequest = new();
    private List<IFormFile> _testFiles;
    private ImagesController _controller;
    public ImagesControllerTests()
    {
        _controller = new ImagesController(_imageService.Object, _logger.Object);

        _testFiles = new List<IFormFile>
        {
            TestUtilities.CreateTestFormImage("test1.jpg", "image/jpg"),
            TestUtilities.CreateTestFormImage("test2.png", "image/png")
        };

        _uploadImagesRequest.Files = _testFiles;
    }

    #region UpladoadImages test


    [TestMethod]
    public async Task UploadImages_WithValidFiles_ReturnsOkResult()
    {
        // Arrange
        var expectedResults = new List<ResponseResult>
        {
            new() { Data = "12345678", StatusCode = 200 },
            new() { Data = "123432111", StatusCode = 200 }
        };

        _imageService.Setup(x => x.ProcessAndStoreImageAsync(_testFiles))
                        .ReturnsAsync(expectedResults);

        // Act
        var result = await _controller.UploadImages(_uploadImagesRequest);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreEqual(expectedResults, okResult.Value);
    }

    [TestMethod]
    public async Task UploadImages_WithEmptyFileList_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UploadImages(new UploadImagesRequest() { Files = new List<IFormFile>() });

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequestResult);
        Assert.IsInstanceOfType(badRequestResult.Value, typeof(ResponseResult));

        var response = badRequestResult.Value as ResponseResult;
        Assert.AreEqual((int)HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual(ErrorsHandler.Empty_Files_List, response.Message);
    }

    [TestMethod]
    public async Task UploadImages_WithNullFileList_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UploadImages(null);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ObjectResult));
        var Result = result as ObjectResult;
        Assert.IsNotNull(Result);

        var response = Result.Value as ResponseResult;
        Assert.AreEqual((int)HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual(ErrorsHandler.Invalid_UploadModel, response.Message);
    }

    [TestMethod]
    public async Task UploadImages_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _imageService.Setup(x => x.ProcessAndStoreImageAsync(_testFiles))
                        .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.UploadImages(_uploadImagesRequest);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ObjectResult));
        var objectResult = result as ObjectResult;
        Assert.AreEqual(StatusCodes.Status200OK, objectResult.StatusCode);

        ResponseResult response = (ResponseResult)objectResult.Value;
        Assert.AreEqual(500, response.StatusCode);
        Assert.AreEqual(ErrorsHandler.Internal_Server_Error, response.Message);

    }

    [TestMethod]
    public async Task UploadImages_WithPartialSuccess_ReturnsMixedResults()
    {
        // Arrange
        var expectedResults = new List<ResponseResult>
        {
            new() { Data = "12340678", StatusCode = 200 },
            new() { Message = ErrorsHandler.Invalid_File_Type, StatusCode = 404 }
        };


        _imageService.Setup(x => x.ProcessAndStoreImageAsync(_testFiles))
                        .ReturnsAsync(expectedResults);

        // Act
        var result = await _controller.UploadImages(_uploadImagesRequest);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var okResult = result as OkObjectResult;
        var results = okResult.Value as List<ResponseResult>;
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(200, results[0].StatusCode);
        Assert.AreEqual(404, results[1].StatusCode);
        Assert.AreEqual(ErrorsHandler.Invalid_File_Type, results[1].Message);
    }

    [TestMethod]
    public async Task UploadImages_AuthorizedAttribute_Exists()
    {
        // Arrange
        var method = typeof(ImagesController).GetMethod("UploadImages");
        var attributes = method.GetCustomAttributes(typeof(AuthorizeAttribute), true);

        // Assert
        Assert.IsTrue(attributes.Any(), "Authorize attribute should be present");
    }

    [TestMethod]
    public async Task UploadImages_RateLimitingEnabled_Exists()
    {
        // Arrange
        var method = typeof(ImagesController).GetMethod("UploadImages");
        var attributes = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true);

        // Assert
        Assert.IsTrue(attributes.Any(), "EnableRateLimiting attribute should be present");

        var rateLimitAttribute = attributes.First() as EnableRateLimitingAttribute;
        Assert.AreEqual("strict", rateLimitAttribute.PolicyName);
    }

    #endregion


    #region GetImageInfo test

    [TestMethod]
    public async Task GetImageInfo_ValidImageId_ReturnsOkWithMetadata()
    {
        // Arrange
        var imageId = "img123";
        var expectedResult = new ResponseResult()
        {
            Data = new ExifData
            {
                Latitude = "12.345678",
                Longitude = "98.765432",
                Make = "Canon",
                Model = "EOS 5D Mark IV",
            },
            StatusCode = StatusCodes.Status200OK

        };

        _imageService.Setup(x => x.GetImageInfoAsync(imageId))
                        .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetImageInfo(imageId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var okResult = result as OkObjectResult;
        Assert.AreEqual(expectedResult, okResult.Value);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow(" ")]
    public async Task GetImageInfo_NullOrEmptyImageId_ReturnsBadRequest(string imageId)
    {

        // Act
        var result = await _controller.GetImageInfo(imageId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequestResult = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequestResult);

        var response = badRequestResult.Value as ResponseResult;
        Assert.AreEqual(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.AreEqual(ErrorsHandler.Empty_image_ID, response.Message);

    }

    [TestMethod]
    public async Task GetImageInfo_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var imageId = "img123";
        var exception = new Exception("Test exception");

        _imageService.Setup(x => x.GetImageInfoAsync(imageId))
                        .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetImageInfo(imageId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ObjectResult));
        var objectResult = result as ObjectResult;
        Assert.AreEqual(StatusCodes.Status200OK, objectResult.StatusCode);

        ResponseResult response = (ResponseResult)objectResult.Value;
        Assert.AreEqual(500, response.StatusCode);
        Assert.AreEqual(ErrorsHandler.Internal_Server_Error, response.Message);

        // Verify logging
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error getting image info for {imageId}")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task GetImageInfo_ImageNotFound_ReturnsNotFound()
    {
        // Arrange
        var imageId = "nonexistent";
        _imageService.Setup(x => x.GetImageInfoAsync(imageId))
                        .ReturnsAsync(new ResponseResult
                        {
                            StatusCode = StatusCodes.Status404NotFound,
                            Message = ErrorsHandler.Image_Not_Found
                        });

        // Act
        var result = await _controller.GetImageInfo(imageId);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var okResult = result as OkObjectResult;
        Assert.IsNotNull(okResult);

        var response = okResult.Value as ResponseResult;
        Assert.AreEqual(StatusCodes.Status404NotFound, response.StatusCode);
        StringAssert.Contains(response.Message, ErrorsHandler.Image_Not_Found);
    }

    [TestMethod]
    public async Task GetImageInfo_AuthorizedAttribute_Exists()
    {
        // Arrange
        var method = typeof(ImagesController).GetMethod("GetImageInfo");
        var attributes = method.GetCustomAttributes(typeof(AuthorizeAttribute), true);

        // Assert
        Assert.IsTrue(attributes.Any(), "Authorize attribute should be present");
    }

    [TestMethod]
    public async Task GetImageInfo_RateLimitingEnabled_Exists()
    {
        // Arrange
        var method = typeof(ImagesController).GetMethod("GetImageInfo");
        var attributes = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true);

        // Assert
        Assert.IsTrue(attributes.Any(), "EnableRateLimiting attribute should be present");

        var rateLimitAttribute = attributes.First() as EnableRateLimitingAttribute;
        Assert.AreEqual("strict", rateLimitAttribute.PolicyName);
    }
    #endregion

    #region  Download image test
    [TestMethod]
    public async Task DownloadImage_ValidRequest_ReturnsFileResult()
    {
        // Arrange
        var imageId = "img123";
        var size = "phone";
        var testStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var contentType = "image/jpeg";

        var serviceResponse = new ResponseResult
        {
            StatusCode = StatusCodes.Status200OK,
            Data = (testStream, contentType)
        };

        _imageService.Setup(x => x.GetResizedImageAsync(imageId, size))
                       .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.DownloadImage(imageId, size);

        // Assert
        Assert.IsInstanceOfType(result, typeof(FileStreamResult));
        var fileResult = result as FileStreamResult;
        Assert.AreEqual(contentType, fileResult.ContentType);
        Assert.AreEqual($"{imageId}_{size}", fileResult.FileDownloadName);
        Assert.AreEqual(testStream, fileResult.FileStream);
    }

    [TestMethod]
    [DataRow(null, "phone")]
    [DataRow("", "tablet")]
    [DataRow(" ", "phone")]
    [DataRow("img123", null)]
    [DataRow("img123", "")]
    [DataRow("img123", " ")
]
    public async Task DownloadImage_InvalidImageId_ReturnsBadRequest(string imageId, string size)
    {
        // Arrange

        // Act
        var result = await _controller.DownloadImage(imageId, size);

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequest = result as BadRequestObjectResult;
        var response = badRequest.Value as ResponseResult;

        Assert.AreEqual(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.IsTrue(response.Message.Contains(ErrorsHandler.Empty_image_ID));
        Assert.IsTrue(response.Message.Contains(ErrorsHandler.Empty_image_Size));
    }

    [TestMethod]
    public async Task DownloadImage_ImageNotFound_ReturnsServiceResult()
    {
        // Arrange
        var imageId = "img123";
        var size = "tablet";
        var expectedResponse = new ResponseResult
        {
            StatusCode = StatusCodes.Status404NotFound,
            Message = ErrorsHandler.Image_Not_Found
        };

        _imageService.Setup(x => x.GetResizedImageAsync(imageId, size))
                       .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DownloadImage(imageId, size);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var okResult = result as OkObjectResult;
        Assert.AreEqual(expectedResponse, okResult.Value);
    }

    [TestMethod]
    public async Task DownloadImage_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var imageId = "img123";
        var size = "phone";
        var exception = new Exception("Test exception");

        _imageService.Setup(x => x.GetResizedImageAsync(imageId, size))
                       .ThrowsAsync(exception);

        // Act
        var result = await _controller.DownloadImage(imageId, size);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ObjectResult));
        var objectResult = result as ObjectResult;
        Assert.AreEqual(StatusCodes.Status200OK, objectResult.StatusCode);

        ResponseResult response = (ResponseResult)objectResult.Value;
        Assert.AreEqual(500, response.StatusCode);
        Assert.AreEqual(ErrorsHandler.Internal_Server_Error, response.Message);

        // Verify logging
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error downloading image {imageId}")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task DownloadImage_InvalidDataFormat_ReturnsInternalServerError()
    {
        // Arrange
        var imageId = "img123";
        var size = "Phone";
        var invalidResponse = new ResponseResult
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "invalid data format"
        };

        _imageService.Setup(x => x.GetResizedImageAsync(imageId, size))
                       .ReturnsAsync(invalidResponse);

        // Act
        var result = await _controller.DownloadImage(imageId, size);

        // Assert
        Assert.IsInstanceOfType(result, typeof(ObjectResult));
        var objectResult = result as ObjectResult;
        Assert.AreEqual(StatusCodes.Status200OK, objectResult.StatusCode);

        ResponseResult response = (ResponseResult)objectResult.Value;
        Assert.AreEqual(500, response.StatusCode);
        Assert.AreEqual(ErrorsHandler.Internal_Server_Error, response.Message);
    }

    [TestMethod]
    public async Task DownloadImage_AuthorizedAttribute_Exists()
    {
        // Arrange
        var method = typeof(ImagesController).GetMethod("DownloadImage");
        var attributes = method.GetCustomAttributes(typeof(AuthorizeAttribute), true);

        // Assert
        Assert.IsTrue(attributes.Any(), "Authorize attribute should be present");
    }

    [TestMethod]
    public async Task DownloadImage_RateLimitingEnabled_Exists()
    {
        // Arrange
        var method = typeof(ImagesController).GetMethod("DownloadImage");
        var attributes = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true);

        // Assert
        Assert.IsTrue(attributes.Any(), "EnableRateLimiting attribute should be present");
        var rateLimitAttribute = attributes.First() as EnableRateLimitingAttribute;
        Assert.AreEqual("strict", rateLimitAttribute.PolicyName);
    }
    #endregion
}