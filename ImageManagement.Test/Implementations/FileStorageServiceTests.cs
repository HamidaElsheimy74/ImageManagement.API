using ImageManagement.Domain.Entities;
using ImageManagement.Test.TestUtilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace ImageManagement.Infrastructure.Implementations.Tests;

[TestClass()]
public class FileStorageServiceTests
{
    private Mock<IWebHostEnvironment> _mockEnv;
    private string _testRootPath;
    string uploadFolderName;
    Mock<IConfiguration> _config;
    string exIFFileName;
    private Mock<ILogger<FileStorageService>> _logger;
    private FileStorageService _fileStorageService;
    public FileStorageServiceTests()
    {

        _mockEnv = new Mock<IWebHostEnvironment>();
        _config = new Mock<IConfiguration>();
        _logger = new Mock<ILogger<FileStorageService>>();
        _testRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _mockEnv.Setup(e => e.ContentRootPath).Returns(_testRootPath);
        uploadFolderName = "Uploads";
        exIFFileName = "ExifData.json";
        _config.Setup(c => c["UploadFolderName"]).Returns(uploadFolderName);
        _config.Setup(c => c["ExifFileName"]).Returns(exIFFileName);
        Directory.CreateDirectory(Path.Combine(_testRootPath, uploadFolderName));
        _fileStorageService = new FileStorageService(_mockEnv.Object, _config.Object, _logger.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(Path.Combine(_testRootPath, _config.Object["UploadFolderName"])))
        {
            Directory.Delete(Path.Combine(_testRootPath, _config.Object["UploadFolderName"]), true);
        }
    }

    #region StoreImageAsync Tests
    [TestMethod]
    public async Task StoreImageAsync_ValidStream_CreatesFileInCorrectLocation()
    {
        // Arrange
        var testStream = TestUtitities.CreateTestStream("test image content");
        var imageId = "img123";
        var fileName = "test.png";

        // Act
        var result = await _fileStorageService.StoreImageAsync(testStream, imageId, fileName);

        // Assert
        var expectedPath = Path.Combine(_testRootPath, _config.Object["UploadFolderName"], imageId, $"original_{fileName}");
        Assert.AreEqual(expectedPath, result);
        Assert.IsTrue(File.Exists(result), "File should exist at the target path");

        // Cleanup
        if (File.Exists(result))
        {
            Directory.Delete(Path.GetDirectoryName(result), true);
        }
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task StoreImageAsync_NullStream_ThrowsArgumentNullException()
    {
        Stream testStream = null;
        var imageId = "img123";
        var fileName = "test.png";
        //Act
        await _fileStorageService.StoreImageAsync(testStream, imageId, fileName);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task StoreImageAsync_UnreadableStream_ThrowsArgumentException()
    {
        // Arrange
        var unreadableStream = new Mock<Stream>();
        unreadableStream.Setup(s => s.CanRead).Returns(false);

        //Act
        await _fileStorageService.StoreImageAsync(unreadableStream.Object, "img123", "test.png");
    }

    [TestMethod]
    [ExpectedException(typeof(IOException))]
    public async Task StoreImageAsync_PathTooLong_ThrowsWrappedException()
    {
        // Arrange
        var longFileName = new string('a', 300) + ".png";
        var testStream = TestUtitities.CreateTestStream("content");

        //Act
        await _fileStorageService.StoreImageAsync(testStream, "img123", longFileName);
    }


    [TestMethod]
    [ExpectedException(typeof(IOException))]
    public async Task StoreImageAsync_UnauthorizedPath_ThrowsWrappedException()
    {
        // Arrange
        _mockEnv.Setup(e => e.ContentRootPath).Returns("C:\\Windows\\System32");
        var testStream = TestUtitities.CreateTestStream("content");

        // Act
        await _fileStorageService.StoreImageAsync(testStream, "img123", "test.png");
    }

    #endregion

    #region StoreExifDataAsync Tests
    [TestMethod]
    public async Task StoreExifDataAsync_ValidData_CreatesExifFile()
    {
        // Arrange
        var imageId = "img123";
        var exifData = new ExifData { Longitude = "1", Latitude = "1", Make = "1", Model = "1" };

        // Act
        await _fileStorageService.StoreExifDataAsync(imageId, exifData);

        // Assert
        var expectedPath = Path.Combine(_testRootPath, _config.Object["UploadFolderName"], imageId, _config.Object["ExifFileName"]);
        Assert.IsTrue(File.Exists(expectedPath), "EXIF file should be created");

        await using var fileStream = File.OpenRead(expectedPath);
        var deserializedData = await JsonSerializer.DeserializeAsync<ExifData>(fileStream);
        Assert.AreEqual(exifData.Longitude, deserializedData.Longitude);
    }

    [TestMethod]
    public async Task StoreExifDataAsync_ConcurrentWrites_OverwritesFile()
    {
        // Arrange
        var imageId = "img123";
        var exifData1 = new ExifData { Longitude = "1", Latitude = "1", Make = "1", Model = "1" };
        var exifData2 = new ExifData { Longitude = "2", Latitude = "2", Make = "2", Model = "2" };

        // Act
        await Task.WhenAll(
            _fileStorageService.StoreExifDataAsync(imageId, exifData1),
            _fileStorageService.StoreExifDataAsync(imageId, exifData2)
        );

        // Assert
        var filePath = Path.Combine(_testRootPath, _config.Object["UploadFolderName"], imageId, _config.Object["ExifFileName"]);
        await using var fileStream = File.OpenRead(filePath);
        var finalData = await JsonSerializer.DeserializeAsync<ExifData>(fileStream);

        Assert.AreEqual(exifData2.Longitude, finalData.Longitude);
    }
    #endregion


    #region GetExifDataAsync Tests
    [TestMethod]
    public async Task GetExifDataAsync_ValidFile_ReturnsDeserializedData()
    {
        // Arrange
        var imageId = "img123";
        var expectedData = new ExifData { Longitude = "1", Latitude = "1", Make = "1", Model = "1" };
        TestUtitities.CreateTestExifFile(imageId, expectedData, _testRootPath, _config.Object["UploadFolderName"], _config.Object["ExifFileName"]);

        // Act
        var result = await _fileStorageService.GetExifDataAsync(imageId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedData.Make, result.Make);
    }
    [TestMethod]
    public async Task GetExifDataAsync_MissingFile_ReturnsNull()
    {
        // Act
        var result = await _fileStorageService.GetExifDataAsync("nonexistent");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetExifDataAsync_CorruptJson_ThrowsInvalidDataException()
    {
        // Arrange
        var imageId = "corrupt";
        var dirPath = Path.Combine(_testRootPath, _config.Object["UploadFolderName"], imageId);
        Directory.CreateDirectory(dirPath);
        File.WriteAllText(Path.Combine(dirPath, _config.Object["ExifFileName"]), "{invalid json}");

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidDataException>(
            () => _fileStorageService.GetExifDataAsync(imageId));
    }


    [TestMethod]
    public async Task GetExifDataAsync_LockedFile_ThrowsIOException()
    {
        // Arrange
        var imageId = "locked";
        TestUtitities.CreateTestExifFile(imageId, new ExifData(), _testRootPath, _config.Object["UploadFolderName"], _config.Object["ExifFileName"]);

        var filePath = Path.Combine(_testRootPath, _config.Object["UploadFolderName"], imageId, _config.Object["ExifFileName"]);
        using (var lockedStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<IOException>(
                () => _fileStorageService.GetExifDataAsync(imageId));
        }
    }

    [TestMethod]

    public async Task GetExifDataAsync_EmptyImageIdNoImageExists_returnNull()
    {
        //Act
        var result = await _fileStorageService.GetExifDataAsync("");

        //Assert
        Assert.IsNull(result);
    }


    #endregion


    #region ImageExistsAsync Tests
    [TestMethod]
    public async Task ImageExistsAsync_ExistingImage_ReturnsTrue()
    {
        // Arrange
        var imageId = "img123";
        var dirPath = Path.Combine(_testRootPath, _config.Object["UploadFolderName"], imageId);
        Directory.CreateDirectory(dirPath);
        // Act
        var result = await _fileStorageService.ImageExistsAsync(imageId);
        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ImageExistsAsync_NonExistingImage_ReturnsFalse()
    {
        // Act
        var result = await _fileStorageService.ImageExistsAsync("nonexistent");
        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region GetImagePath Tests

    [TestMethod]
    public void GetImagePath_OriginalSize_ReturnsOriginalFile()
    {
        // Arrange
        var imageId = "img1";
        TestUtitities.CreateTestImage(imageId, "original", _config.Object["UploadFolderName"], _testRootPath, "jpg");

        // Act
        var result = _fileStorageService.GetImagePath(imageId);

        // Assert
        var expectedPath = Path.Combine(_testRootPath, _config.Object["UploadFolderName"], imageId, "original_test.jpg");
        Assert.AreEqual(expectedPath, result);
    }


    [TestMethod]
    public void GetImagePath_CustomSize_ReturnsSizeSpecificFile()
    {
        // Arrange
        var imageId = "img2";
        TestUtitities.CreateTestImage(imageId, "thumbnail", _config.Object["UploadFolderName"], _testRootPath);

        // Act
        var result = _fileStorageService.GetImagePath(imageId, "thumbnail");

        // Assert
        var expectedPath = Path.Combine(_testRootPath, _config.Object["UploadFolderName"], imageId, "thumbnail.webp");
        Assert.AreEqual(expectedPath, result);
    }

    [TestMethod]
    public void GetImagePath_MissingImage_ReturnsNull()
    {
        // Act
        var result = _fileStorageService.GetImagePath("nonexistent");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetImagePath_MissingSize_ReturnsNull()
    {
        // Arrange
        var imageId = "img3";
        TestUtitities.CreateTestImage(imageId, "original", _config.Object["UploadFolderName"], _testRootPath);

        // Act
        var result = _fileStorageService.GetImagePath(imageId, "nonexistent_size");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetImagePath_ProtectedDirectory_ReturnsNullAndLogsError()
    {
        // Arrange
        var imageId = "protected";
        var imageFolder = Path.Combine(_testRootPath, _config.Object["UploadFolderName"], imageId);
        Directory.CreateDirectory(imageFolder);
        File.SetAttributes(imageFolder, FileAttributes.ReadOnly);

        try
        {
            // Act
            var result = _fileStorageService.GetImagePath(imageId);

            // Assert
            Assert.IsNull(result);
        }
        finally
        {
            File.SetAttributes(imageFolder, FileAttributes.Normal);
        }
    }

    [TestMethod]
    public void GetImagePath_PathTooLong_ReturnsNullAndLogsError()
    {
        // Arrange
        var longImageId = new string('a', 300);

        // Act
        var result = _fileStorageService.GetImagePath(longImageId);

        // Assert
        Assert.IsNull(result);

    }

    [TestMethod]
    public void GetImagePath_MultipleOriginalFiles_ReturnsFirstMatch()
    {
        // Arrange
        var imageId = "multi_original";
        var imageFolder = Path.Combine(_testRootPath, _config.Object["UploadFolderName"], imageId);
        Directory.CreateDirectory(imageFolder);
        File.WriteAllText(Path.Combine(imageFolder, "original_1.jpg"), "test1");
        File.WriteAllText(Path.Combine(imageFolder, "original_2.jpg"), "test2");

        // Act
        var result = _fileStorageService.GetImagePath(imageId);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.StartsWith(result, Path.Combine(_testRootPath, _config.Object["UploadFolderName"], imageId, "original_"));
    }

    [TestMethod]
    public void GetImagePath_DifferentExtensions_ReturnsCorrectFile()
    {
        // Arrange
        var imageId = "different_ext";
        TestUtitities.CreateTestImage(imageId, "original", _config.Object["UploadFolderName"], _testRootPath, "png");
        TestUtitities.CreateTestImage(imageId, "thumbnail", _config.Object["UploadFolderName"], _testRootPath);

        // Act
        var originalResult = _fileStorageService.GetImagePath(imageId);
        var thumbResult = _fileStorageService.GetImagePath(imageId, "thumbnail");

        // Assert
        Assert.IsTrue(originalResult.EndsWith(".png"));
        Assert.IsTrue(thumbResult.EndsWith(".webp"));
    }

    #endregion


}