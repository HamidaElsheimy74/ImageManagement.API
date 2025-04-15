using ImageManagement.BLL.Helpers.Interfaces;
using ImageManagement.BLL.Models;
using ImageManagement.Common.DTOs;
using ImageManagement.Common.Errors;
using ImageManagement.Domain.Entities;
using ImageManagement.Infrastructure.Interfaces;
using ImageManagement.Test.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageManagement.BLL.Services.Tests;

[TestClass()]
public class ImageServiceTests
{
    private Mock<IFileStorageService> _mockFileStorageService;
    private Mock<ILogger<ImageService>> _mockLogger;
    private IConfiguration _config;
    private const string allowedExtensions = ".JPG,.PNG,.WEBP";
    private const string maxFileSize = "2097152";
    private const string conversionFormat = "WebP";
    private Mock<IImageProcessingHelpers> _mockImageProcessingHelpers;
    private ImageService _imageService;
    public ImageServiceTests()
    {
        _mockFileStorageService = new();
        _mockLogger = new();
        _mockImageProcessingHelpers = new();
        _config = new ConfigurationBuilder()
          .AddInMemoryCollection(new Dictionary<string, string>
          {
              ["AllowedExtensions"] = allowedExtensions,
              ["MaxFileSize"] = maxFileSize,
              ["ConversionFormat"] = conversionFormat,
              ["DeviceSize:0:Name"] = "phone",
              ["DeviceSize:0:Width"] = "640",
              ["DeviceSize:0:Height"] = "480",
              ["DeviceSize:1:Name"] = "tablet",
              ["DeviceSize:1:Width"] = "1024",
              ["DeviceSize:1:Height"] = "768",
              ["DeviceSize:1:Name"] = "desktop",
              ["DeviceSize:1:Width"] = "1920",
              ["DeviceSize:1:Height"] = "1080",

          })
          .Build();
        _imageService = new ImageService(_mockFileStorageService.Object, _mockLogger.Object, _config, _mockImageProcessingHelpers.Object);
    }


    #region ProcessAndStoreImage tests

    [TestMethod]
    public async Task ProcessAndStoreImageAsync_InvalidFileType_ReturnsBadRequest()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.pdf");
        mockFile.Setup(f => f.Length).Returns(1024);
        _mockImageProcessingHelpers.Setup(x => x.ValidateFile(It.IsAny<IFormFile>(), It.IsAny<ProcessingConfiguration>()))
            .Returns(new ResponseResult
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = ErrorsHandler.Invalid_File_Type
            });
        // Act
        var result = await _imageService.ProcessAndStoreImageAsync(new List<IFormFile> { mockFile.Object });

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(StatusCodes.Status400BadRequest, result[0].StatusCode);
        Assert.IsTrue(result[0].Message.Contains(ErrorsHandler.Invalid_File_Type));
    }

    [TestMethod]
    public async Task ProcessAndStoreImageAsync_FileTooLarge_ReturnsBadRequest()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.jpg");
        mockFile.Setup(f => f.Length).Returns(3 * 1024 * 1024);
        _mockImageProcessingHelpers.Setup(x => x.ValidateFile(It.IsAny<IFormFile>(), It.IsAny<ProcessingConfiguration>()))
            .Returns(new ResponseResult
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = ErrorsHandler.File_Too_Large
            });

        // Act
        var result = await _imageService.ProcessAndStoreImageAsync(new List<IFormFile> { mockFile.Object });

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(StatusCodes.Status400BadRequest, result[0].StatusCode);
        Assert.IsTrue(result[0].Message.Contains(ErrorsHandler.File_Too_Large));
    }

    [TestMethod]
    public async Task ProcessAndStoreImageAsync_ValidFile_ProcessesSuccessfully()
    {
        // Arrange
        var fileName = "test.jpg";

        using var image = new Image<Rgba32>(10, 10);
        var imageStream = new MemoryStream();
        await image.SaveAsJpegAsync(imageStream);
        imageStream.Position = 0;

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns("image/jpg");
        mockFile.Setup(f => f.Length).Returns(imageStream.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(imageStream);

        _mockFileStorageService.Setup(s => s.StoreImageAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync("mock_storage_path");

        _mockFileStorageService.Setup(s => s.StoreExifDataAsync(
            It.IsAny<string>(),
            It.IsAny<ExifData>()))
            .Returns(Task.CompletedTask);

        var mockExifData = new ExifData();
        _mockImageProcessingHelpers.Setup(x => x.ExtractExifData(It.IsAny<string>()))
                         .Returns(mockExifData);

        // Act
        var result = await _imageService.ProcessAndStoreImageAsync(
            new List<IFormFile> { mockFile.Object });

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(StatusCodes.Status200OK, result[0].StatusCode);
        Assert.IsNotNull(result[0].Data);

    }

    [TestMethod]
    public async Task ProcessAndStoreImageAsync_StorageFails_ReturnsInternalError()
    {
        // Arrange
        var mockFile = TestUtilities.CreateTestFormImage("test1.jpg", "image/jpg");
        _mockFileStorageService.Setup(s => s.StoreImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                       .ThrowsAsync(new IOException("Storage failed"));

        // Act
        var result = await _imageService.ProcessAndStoreImageAsync(new List<IFormFile> { mockFile });

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(StatusCodes.Status500InternalServerError, result[0].StatusCode);
        Assert.IsTrue(result[0].Message.Contains(ErrorsHandler.Internal_Server_Error));
    }

    #endregion

    #region GetImageInfo tests

    [TestMethod]
    public async Task GetImageInfoAsync_ImageNotFound_ReturnsNotFound()
    {
        // Arrange
        var imageId = "nonexistent123";
        _mockFileStorageService.Setup(x => x.ImageExistsAsync(imageId))
                       .ReturnsAsync(false);

        // Act
        var result = await _imageService.GetImageInfoAsync(imageId);

        // Assert
        Assert.AreEqual(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.AreEqual(ErrorsHandler.Image_Not_Found, result.Message);
        _mockFileStorageService.Verify(x => x.GetExifDataAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task GetImageInfoAsync_ExistingImage_ReturnsExifData()
    {
        // Arrange
        var imageId = "existing123";
        var testExif = new ExifData
        {
            Latitude = "40.7128",
            Longitude = "-74.0060",
            Make = "TestCam",
            Model = "X100"
        };

        _mockFileStorageService.Setup(x => x.ImageExistsAsync(imageId))
                       .ReturnsAsync(true);
        _mockFileStorageService.Setup(x => x.GetExifDataAsync(imageId))
                       .ReturnsAsync(testExif);

        // Act
        var result = await _imageService.GetImageInfoAsync(imageId);

        // Assert
        Assert.AreEqual(StatusCodes.Status200OK, result.StatusCode);
        Assert.IsNotNull(result.Data);
        var returnedExif = result.Data as ExifData;
        Assert.AreEqual(testExif.Latitude, returnedExif.Latitude);
        Assert.AreEqual(testExif.Make, returnedExif.Make);
        _mockFileStorageService.Verify(x => x.GetExifDataAsync(imageId), Times.Once);
    }

    [TestMethod]
    public async Task GetImageInfoAsync_NullExifData_ReturnsSuccessWithNull()
    {
        // Arrange
        var imageId = "noexif123";
        _mockFileStorageService.Setup(x => x.ImageExistsAsync(imageId))
                       .ReturnsAsync(true);
        _mockFileStorageService.Setup(x => x.GetExifDataAsync(imageId))
                       .ReturnsAsync((ExifData)null);

        // Act
        var result = await _imageService.GetImageInfoAsync(imageId);

        // Assert
        Assert.AreEqual(StatusCodes.Status200OK, result.StatusCode);
        Assert.IsNull(result.Data);
    }

    [TestMethod]
    public async Task GetImageInfoAsync_StorageThrows_ReturnsInternalError()
    {
        // Arrange
        var imageId = "error123";
        _mockFileStorageService.Setup(x => x.ImageExistsAsync(imageId))
                       .ThrowsAsync(new IOException("Storage error"));

        // Act
        var result = await _imageService.GetImageInfoAsync(imageId);

        // Assert
        Assert.AreEqual(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.AreEqual(ErrorsHandler.Internal_Server_Error, result.Message);

    }

    [TestMethod]
    public async Task GetImageInfoAsync_PartialExifData_ReturnsOnlyAvailableFields()
    {
        // Arrange
        var imageId = "partial123";
        var partialExif = new ExifData { Make = "PartialCam", Model = null };
        _mockFileStorageService.Setup(x => x.ImageExistsAsync(imageId))
                      .ReturnsAsync(true);
        _mockFileStorageService.Setup(x => x.GetExifDataAsync(imageId))
                      .ReturnsAsync(partialExif);

        // Act
        var result = await _imageService.GetImageInfoAsync(imageId);

        // Assert
        Assert.AreEqual(StatusCodes.Status200OK, result.StatusCode);
        var returnedExif = result.Data as ExifData;
        Assert.AreEqual("PartialCam", returnedExif.Make);
        Assert.IsNull(returnedExif.Model);
    }
    #endregion


    #region GetResizedImageAsync tests
    [TestMethod]
    public async Task GetResizedImageAsync_InvalidImageId_ReturnsNotFoundRequest()
    {

        //Arrange
        _mockImageProcessingHelpers.Setup(x => x.IsValidSize(It.IsAny<string>(), It.IsAny<IConfiguration>()))
            .Returns(true);
        // Act
        var nullResult = await _imageService.GetResizedImageAsync(null, "phone");
        var emptyResult = await _imageService.GetResizedImageAsync("", "phone");
        var whitespaceResult = await _imageService.GetResizedImageAsync("   ", "phone");

        // Assert
        Assert.AreEqual(StatusCodes.Status404NotFound, nullResult.StatusCode);
        Assert.AreEqual(StatusCodes.Status404NotFound, emptyResult.StatusCode);
        Assert.AreEqual(StatusCodes.Status404NotFound, whitespaceResult.StatusCode);
        Assert.AreEqual(ErrorsHandler.Image_Not_Found, nullResult.Message);
    }

    [TestMethod]
    public async Task GetResizedImageAsync_InvalidSize_ReturnsBadRequest()
    {
        // Arrange
        var imageId = "test123";

        // Act
        var result = await _imageService.GetResizedImageAsync(imageId, "invalid_size");

        // Assert
        Assert.AreEqual(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.AreEqual(ErrorsHandler.Invalid_Size, result.Message);
        _mockFileStorageService.Verify(x => x.GetImagePath(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }


    [TestMethod]
    public async Task GetResizedImageAsync_ResizedImage_ReturnsWebP()
    {
        // Arrange
        var imageId = "test123";
        var size = "tablet";
        var testPath = "path/to/tablet.webp";
        _mockImageProcessingHelpers.Setup(x => x.IsValidSize(size, _config)).Returns(true);
        _mockFileStorageService.Setup(x => x.GetImagePath(imageId, size)).Returns(testPath);
        _mockImageProcessingHelpers.Setup(x => x.FileExistsAsync(testPath)).ReturnsAsync(true);
        _mockImageProcessingHelpers.Setup(x => x.ReadImageFileAsync(testPath, size))
            .ReturnsAsync((new MemoryStream(), "image/webp"));

        // Act
        var result = await _imageService.GetResizedImageAsync(imageId, size);

        // Assert
        Assert.AreEqual(StatusCodes.Status200OK, result.StatusCode);
        var (stream, contentType) = ((MemoryStream, string))result.Data;
        Assert.AreEqual("image/webp", contentType);
    }

    [TestMethod]
    public async Task GetResizedImageAsync_StorageError_ReturnsInternalError()
    {
        // Arrange
        var imageId = "test123";
        var size = "phone";
        var testPath = "path/to/phone.webp";

        _mockImageProcessingHelpers.Setup(x => x.IsValidSize(size, _config)).Returns(true);
        _mockFileStorageService.Setup(x => x.GetImagePath(imageId, size)).Returns(testPath);
        _mockImageProcessingHelpers.Setup(x => x.FileExistsAsync(testPath)).ThrowsAsync
            (new Exception("exception"));
        _mockImageProcessingHelpers.Setup(x => x.ReadImageFileAsync(testPath, size))
            .ReturnsAsync((new MemoryStream(), "image/webp"));
        // Act
        var result = await _imageService.GetResizedImageAsync(imageId, size);

        // Assert
        Assert.AreEqual(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.AreEqual(ErrorsHandler.Internal_Server_Error, result.Message);
    }

    [TestMethod]
    public async Task GetResizedImageAsync_ValidRequest_ReturnsImageStream()
    {
        // Arrange
        var imageId = "test123";
        var size = "large";
        var testPath = "path/to/large.webp";
        var testContent = new byte[] { 0x01, 0x02, 0x03 };
        var testStream = new MemoryStream(testContent);
        var expectedResult = (testStream, "image/webp");

        _mockImageProcessingHelpers.Setup(x => x.IsValidSize(size, _config)).Returns(true);
        _mockImageProcessingHelpers.Setup(x => x.FileExistsAsync(testPath)).ReturnsAsync(true);
        _mockFileStorageService.Setup(x => x.GetImagePath(imageId, size)).Returns(testPath);
        _mockImageProcessingHelpers.Setup(x => x.ReadImageFileAsync(testPath, size))
                      .ReturnsAsync(expectedResult);

        // Act
        var result = await _imageService.GetResizedImageAsync(imageId, size);

        // Assert
        Assert.AreEqual(StatusCodes.Status200OK, result.StatusCode);
        var (stream, _) = ((MemoryStream, string))result.Data;
        var resultContent = new byte[3];
        stream.Read(resultContent, 0, 3);
        CollectionAssert.AreEqual(testContent, resultContent);
    }
    #endregion
}