using ImageManagement.Domain.Entities;
using ImageManagement.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ImageManagement.Infrastructure.Implementations;


public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private string uploadFolderName;
    private string exIFFileName;
    ILogger<FileStorageService> _logger;
    public FileStorageService(IWebHostEnvironment env, IConfiguration config, ILogger<FileStorageService> logger)
    {
        _env = env;
        _config = config;
        uploadFolderName = _config["UploadFolderName"]!;
        exIFFileName = _config["ExifFileName"]!;
        _logger = logger;
    }
    public async Task<string> StoreImageAsync(Stream imageStream, string imageId, string originalFileName)
    {
        if (imageStream == null)
            throw new ArgumentNullException(nameof(imageStream));

        if (!imageStream.CanRead)
            throw new ArgumentException("The provided stream is not readable", nameof(imageStream));

        var uploadsFolder = Path.Combine(_env.ContentRootPath, uploadFolderName, imageId);

        try
        {
            Directory.CreateDirectory(uploadsFolder);

            var originalPath = Path.Combine(uploadsFolder, $"original_{originalFileName}");

            var tempPath = Path.GetTempFileName();
            try
            {
                await using (var tempStream = new FileStream(
                    tempPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await imageStream.CopyToAsync(tempStream);
                }

                File.Move(tempPath, originalPath, overwrite: true);
                return originalPath;
            }
            finally
            {
                if (File.Exists(tempPath))
                    try { File.Delete(tempPath); }
                    catch
                    {
                        _logger.LogError("Failed to delete temp file: " + tempPath);
                    }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new IOException($"Access denied to path: {uploadsFolder}", ex);
        }
        catch (PathTooLongException ex)
        {
            throw new IOException("Path too long", ex);
        }
        catch (IOException ex)
        {
            throw new IOException("Failed to store image", ex);
        }
    }

    public async Task StoreExifDataAsync(string imageId, ExifData exifData)
    {
        var uploadsFolder = Path.Combine(_env.ContentRootPath, uploadFolderName, imageId);
        var exifPath = Path.Combine(uploadsFolder, exIFFileName);

        Directory.CreateDirectory(uploadsFolder);

        var tempPath = Path.GetTempFileName();
        try
        {
            await using (var tempStream = new FileStream(
                tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await JsonSerializer.SerializeAsync(tempStream, exifData);
            }

            File.Move(tempPath, exifPath, overwrite: true);
        }
        catch (Exception ex)
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); }
                catch
                {
                    _logger.LogError("Failed to delete temp file: " + tempPath);
                }
            }
            _logger.LogError(ex, $"Failed to store EXIF data for image {imageId}");
            throw new IOException($"Failed to store EXIF data for image {imageId}", ex);
        }

    }

    public async Task<ExifData> GetExifDataAsync(string imageId)
    {
        var exifPath = Path.Combine(_env.ContentRootPath, uploadFolderName, imageId, exIFFileName);

        if (!File.Exists(exifPath))
            return null!;

        try
        {
            await using var fileStream = new FileStream(
                exifPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            // Deserialize with cancellation token and options
            return await JsonSerializer.DeserializeAsync<ExifData>(
                fileStream,
                options: null!,
                cancellationToken: default);
        }
        catch (JsonException jsonEx)
        {
            throw new InvalidDataException($"Invalid EXIF data format in file: {exifPath}", jsonEx);
        }
        catch (IOException ioEx)
        {
            throw new IOException($"Could not read EXIF data from file: {exifPath}", ioEx);
        }
        catch (UnauthorizedAccessException authEx)
        {
            throw new UnauthorizedAccessException($"Access denied to EXIF file: {exifPath}", authEx);
        }
    }

    public async Task<bool> ImageExistsAsync(string imageId)
    {

        try
        {
            var imageFolder = Path.Combine(_env.ContentRootPath, uploadFolderName, imageId);

            return await Task.Run(() => Directory.Exists(imageFolder));
        }
        catch (Exception ex) when (ex is ArgumentException or
                            PathTooLongException or
                            UnauthorizedAccessException or
                            IOException)
        {
            _logger.LogError(ex, $"Error checking image existence: {imageId}");
            return false;
        }
    }

    public string? GetImagePath(string imageId, string size = "original")
    {

        try
        {
            var imageFolder = Path.Combine(_env.ContentRootPath, uploadFolderName, imageId);

            if (!Directory.Exists(imageFolder))
                return null;

            if (size == "original")
            {
                return Directory.EnumerateFiles(imageFolder, "original_*")
                              .FirstOrDefault();
            }

            var sizePath = Path.Combine(imageFolder, $"{size}.webp");
            return File.Exists(sizePath) ? sizePath : null;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or
                                 IOException or
                                 PathTooLongException)
        {
            _logger.LogError(ex, $"Error accessing image path: {imageId}, size: {size}");
            return null;
        }

    }

}
