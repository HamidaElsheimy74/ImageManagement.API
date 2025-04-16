using ImageManagement.BLL.Models;
using ImageManagement.Common.Errors;
using ImageManagement.Domain.Entities;
using ImageManagement.Infrastructure.Interfaces;
using ImageManagement.Test.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection;

namespace ImageManagement.BLL.Helpers.Services.Tests;

[TestClass()]
public class ImageProcessingHelpersTests
{
    private ImageProcessingHelpers _imageProcessingHelpers;
    private string _testImagesPath;
    public ImageProcessingHelpersTests()
    {
        _imageProcessingHelpers = new ImageProcessingHelpers();

        _testImagesPath = Path.Combine(
           Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
           "TestImages");

        Directory.CreateDirectory(_testImagesPath);

    }


    #region ValidateFile tests
    [TestMethod]
    public void ValidateFile_ValidFile_ReturnsNull()
    {
        // Arrange
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns("test.jpg");
        file.Setup(f => f.Length).Returns(1024);
        var config = new ProcessingConfiguration
        {
            AllowedExtensions = new string[] { ".JPG", ".PNG" },
            MaxFileSize = 2048
        };
        // Act
        var result = _imageProcessingHelpers.ValidateFile(file.Object, config);
        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ValidateFile_InvalidFileType_ReturnsBadRequest()
    {
        // Arrange
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns("test.pdf");
        file.Setup(f => f.Length).Returns(1024);
        var config = new ProcessingConfiguration
        {
            AllowedExtensions = new string[] { ".JPG", ".PNG" },
            MaxFileSize = 2048
        };
        // Act
        var result = _imageProcessingHelpers.ValidateFile(file.Object, config);
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.AreEqual($"{file.Object.FileName} {ErrorsHandler.Invalid_File_Type}", result.Message);
    }
    [TestMethod]
    public void ValidateFile_FileTooLarge_ReturnsBadRequest()
    {
        // Arrange
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns("test.jpg");
        file.Setup(f => f.Length).Returns(4096);
        var config = new ProcessingConfiguration
        {
            AllowedExtensions = new string[] { ".JPG", ".PNG" },
            MaxFileSize = 2048
        };
        // Act
        var result = _imageProcessingHelpers.ValidateFile(file.Object, config);
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.AreEqual($"{file.Object.FileName} {ErrorsHandler.File_Too_Large}", result.Message);
    }
    #endregion

    #region GetProcessingConfiguration tests
    [TestMethod]
    public void GetProcessingConfiguration_ValidConfig_ReturnsProcessingConfiguration()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AllowedExtensions"] = ".JPG,.PNG",
                ["MaxFileSize"] = "2048",
                ["ConversionFormat"] = "WebP"
            })
            .Build();
        // Act
        var result = _imageProcessingHelpers.GetProcessingConfiguration(config);
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.AllowedExtensions.Length);
        Assert.AreEqual(2048, result.MaxFileSize);
        Assert.AreEqual("WebP", result.ConversionFormat);
    }

    [TestMethod]
    public void GetProcessingConfiguration_InvalidMaxFileSize_ReturnsDefault()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AllowedExtensions"] = ".JPG,.PNG",
                ["ConversionFormat"] = "WebP"
            })
            .Build();
        // Act
        var result = _imageProcessingHelpers.GetProcessingConfiguration(config);
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2 * 1024 * 1024, result.MaxFileSize);
    }
    [TestMethod]
    public void GetProcessingConfiguration_EmptyConfig_ReturnsDefault()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>())
            .Build();
        // Act
        var result = _imageProcessingHelpers.GetProcessingConfiguration(config);
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2 * 1024 * 1024, result.MaxFileSize);
    }
    [TestMethod]
    public void GetProcessingConfiguration_NullConfig_ReturnsDefault()
    {
        // Arrange
        IConfiguration config = null;

        // Act
        var result = Assert.ThrowsException<NullReferenceException>(() => _imageProcessingHelpers.GetProcessingConfiguration(config));

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result.Message, "Object reference not set to an instance of an object.");
    }
    #endregion


    #region ProcessImageAndMetadata tests
    [TestMethod]
    public async Task ProcessImageAndMetadata_ValidImage_SavesResizedImages()
    {
        // Arrange
        var imageId = "12345";
        var config = new ProcessingConfiguration
        {
            TargetSizes = new DeviceSize[]
            {
                new DeviceSize { Name = "phone", Width = 640, Height = 480 },
                new DeviceSize { Name = "tablet", Width = 1024, Height = 768 },
                new DeviceSize { Name = "desktop", Width = 1920, Height = 1080 }

            },
            ConversionFormat = "WebP"
        };
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


        var mockFileStorageService = new Mock<IFileStorageService>();
        mockFileStorageService.Setup(x => x.StoreExifDataAsync(It.IsAny<string>(), It.IsAny<ExifData>())).Returns(Task.CompletedTask);

        var mockExifData = new ExifData();


        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var originalPath = Path.Combine(tempDir, "original.jpg");

        try
        {
            using (var image1 = new Image<Rgba32>(100, 100))
            {
                await image.SaveAsJpegAsync(originalPath);
            }

            imageStream.Position = 0;

            // Act
            await _imageProcessingHelpers.ProcessImageAndMetadata(
                imageStream,
                originalPath,
                imageId,
                config,
                mockFileStorageService.Object);

            // Assert
            foreach (var target in config.TargetSizes)
            {
                var expectedPath = Path.Combine(tempDir, $"{target.Name}.webp");
                Assert.IsTrue(File.Exists(expectedPath), $"Resized image not found: {expectedPath}");

                using var resizedImage = await Image.LoadAsync(expectedPath);
                Assert.IsTrue(resizedImage.Width <= target.Width);
                Assert.IsTrue(resizedImage.Height <= target.Height);

                Assert.AreEqual("webp", resizedImage.Metadata.DecodedImageFormat!.DefaultMimeType.Split('/')[1]);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                }
            }
        }
    }

    [TestMethod]
    public async Task ProcessImageAndMetadata_InvalidImage_ThrowsException()
    {
        // Arrange
        var imageId = "12345";
        var config = new ProcessingConfiguration
        {
            TargetSizes = new DeviceSize[]
            {
                new DeviceSize { Name = "phone", Width = 640, Height = 480 },
                new DeviceSize { Name = "tablet", Width = 1024, Height = 768 },
                new DeviceSize { Name = "desktop", Width = 1920, Height = 1080 }
            },
            ConversionFormat = "WebP"
        };
        var imageStream = new MemoryStream();
        var mockFileStorageService = new Mock<IFileStorageService>();
        mockFileStorageService.Setup(x => x.StoreExifDataAsync(It.IsAny<string>(), It.IsAny<ExifData>())).Returns(Task.CompletedTask);
        // Act & Assert
        await Assert.ThrowsExceptionAsync<UnknownImageFormatException>(async () =>
        {
            await _imageProcessingHelpers.ProcessImageAndMetadata(
                imageStream,
                null,
                imageId,
                config,
                mockFileStorageService.Object);
        });
    }

    [TestMethod]
    public async Task ProcessImageAndMetadata_EmptyStream_ThrowsException()
    {
        // Arrange
        var imageId = "12345";
        var config = new ProcessingConfiguration
        {
            TargetSizes = new DeviceSize[]
            {
                new DeviceSize { Name = "phone", Width = 640, Height = 480 },
                new DeviceSize { Name = "tablet", Width = 1024, Height = 768 },
                new DeviceSize { Name = "desktop", Width = 1920, Height = 1080 }
            },
            ConversionFormat = "WebP"
        };
        var imageStream = new MemoryStream();
        var mockFileStorageService = new Mock<IFileStorageService>();
        mockFileStorageService.Setup(x => x.StoreExifDataAsync(It.IsAny<string>(), It.IsAny<ExifData>())).Returns(Task.CompletedTask);
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
        {
            await _imageProcessingHelpers.ProcessImageAndMetadata(
                null,
                null,
                imageId,
                config,
                mockFileStorageService.Object);
        });
    }
    #endregion

    #region IsValidSize tests
    [TestMethod]
    public void IsValidSize_ValidSize_ReturnsTrue()
    {
        // Arrange
        var size = "phone";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AllowedSizes"] = "phone,tablet,desktop"
            })
            .Build();
        // Act
        var result = _imageProcessingHelpers.IsValidSize(size, config);
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsValidSize_InvalidSize_ReturnsFalse()
    {
        // Arrange
        var size = "invalid";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AllowedSizes"] = "phone,tablet,desktop"
            })
            .Build();
        // Act
        var result = _imageProcessingHelpers.IsValidSize(size, config);
        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region FileExistsAsync tests
    [TestMethod]
    public async Task FileExistsAsync_FileExists_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(filePath);
        var fileName = "test.jpg";
        var fullPath = Path.Combine(filePath, fileName);
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await stream.WriteAsync(new byte[0], 0, 0);
        }
        // Act
        var result = await _imageProcessingHelpers.FileExistsAsync(fullPath);
        // Assert
        Assert.IsTrue(result);
    }
    [TestMethod]
    public async Task FileExistsAsync_FileDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        // Act
        var result = await _imageProcessingHelpers.FileExistsAsync(filePath);
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task FileExistsAsync_EmptPath_ReturnFalse()
    {
        // Arrange
        string filePath = string.Empty;

        // Act 
        var result = await _imageProcessingHelpers.FileExistsAsync(filePath);

        //Assert
        Assert.IsFalse(result);

    }

    #endregion

    #region ReadImageFileAsync tests

    [TestMethod]
    public async Task ReadImageFileAsync_ValidFile_ReturnsStreamAndContentType()
    {
        // Arrange
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(filePath);
        var fileName = "test.jpg";
        var fullPath = Path.Combine(filePath, fileName);
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await stream.WriteAsync(new byte[0], 0, 0);
        }
        // Act
        var result = await _imageProcessingHelpers.ReadImageFileAsync(fullPath, "phone");
        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result.Stream, typeof(MemoryStream));
        Assert.AreEqual("image/webp", result.ContentType);
    }

    [TestMethod]
    public async Task ReadImageFileAsync_FileDoesNotExist_ThrowsException()
    {
        // Arrange
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        // Act & Assert
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () =>
        {
            await _imageProcessingHelpers.ReadImageFileAsync(filePath, "phone");
        });
    }

    [TestMethod]
    public async Task ReadImageFileAsync_EmptyPath_ThrowsException()
    {
        // Arrange
        string filePath = string.Empty;
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
        {
            await _imageProcessingHelpers.ReadImageFileAsync(filePath, "phone");
        });
    }

    #endregion

    #region ExtractExifData tests

    [TestMethod]
    public void ExtractExifData_WithFullExifData_ReturnsCompleteInfo()
    {
        // Arrange
        var imagePath = Path.Combine(_testImagesPath, "test_with_exif.jpg");
        TestUtilities.CreateTestImageWithGpsData(imagePath, 40.7128, -74.006, "TestMake", "TestModel");

        // Act
        var result = _imageProcessingHelpers.ExtractExifData(imagePath);

        // Assert
        Assert.AreEqual("TestMake", result.Make);
        Assert.AreEqual("TestModel", result.Model);
        Assert.AreEqual("40.7128", result.Latitude);
        Assert.AreEqual("-74.006", result.Longitude);
        TestCleanUp(_testImagesPath);
    }


    [TestMethod]
    [ExpectedException(typeof(MetadataExtractor.ImageProcessingException))]
    public void ExtractExifData_WithCorruptedImage_ThrowsException()
    {
        // Arrange
        var corruptedPath = Path.Combine(_testImagesPath, "corrupted.jpg");
        File.WriteAllText(corruptedPath, "This is not an image file");

        // Act
        _imageProcessingHelpers.ExtractExifData(corruptedPath);
    }


    [TestMethod]
    public void ExtractExifData_WithNoExifData_ReturnsEmptyObject()
    {
        // Arrange
        var imagePath = TestUtilities.CreateTestImageWithNoExif(_testImagesPath);

        // Act
        var result = _imageProcessingHelpers.ExtractExifData(imagePath);

        // Assert
        Assert.IsNull(result.Make);
        Assert.IsNull(result.Model);
        Assert.IsNull(result.Latitude);
        Assert.IsNull(result.Longitude);
        TestCleanUp(_testImagesPath);
    }


    private void TestCleanUp(string testImagePath)
    {
        if (Directory.Exists(testImagePath))
        {
            try
            {
                Directory.Delete(testImagePath, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
    #endregion





}
