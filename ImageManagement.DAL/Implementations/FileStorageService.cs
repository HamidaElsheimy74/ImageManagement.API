using ImageManagement.Domain.Entities;
using ImageManagement.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ImageManagement.Infrastructure.Implementations;


public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private string uploadFolderName;
    private string exIFFileName;
    public FileStorageService(IWebHostEnvironment env, IConfiguration config)
    {
        _env = env;
        _config = config;
        uploadFolderName = _config["UploadFolderName"]!;
        exIFFileName = _config["ExifFileName"]!;
    }
    public async Task<string> StoreImageAsync(Stream imageStream, string imageId, string originalFileName)
    {
        var uploadsFolder = Path.Combine(_env.ContentRootPath, uploadFolderName, imageId);
        Directory.CreateDirectory(uploadsFolder);

        var originalPath = Path.Combine(uploadsFolder, $"original_{originalFileName}");
        await using (var fileStream = new FileStream(originalPath, FileMode.Create))
        {
            await imageStream.CopyToAsync(fileStream);
        }

        return originalPath;
    }

    public async Task StoreExifDataAsync(string imageId, ExifData exifData)
    {
        var uploadsFolder = Path.Combine(_env.ContentRootPath, uploadFolderName, imageId);
        var exifPath = Path.Combine(uploadsFolder, exIFFileName);

        await JsonSerializer.SerializeAsync(
            new FileStream(exifPath, FileMode.Create),
            exifData);
    }

    public async Task<ExifData> GetExifDataAsync(string imageId)
    {
        var exifPath = Path.Combine(_env.ContentRootPath, uploadFolderName, imageId, exIFFileName);

        if (!File.Exists(exifPath))
            return null!;

        await using var fileStream = new FileStream(exifPath, FileMode.Open);
        return await JsonSerializer.DeserializeAsync<ExifData>(fileStream);
    }

    public async Task<bool> ImageExistsAsync(string imageId)
    {
        var imageFolder = Path.Combine(_env.ContentRootPath, uploadFolderName, imageId);
        return Directory.Exists(imageFolder);
    }

    public string GetImagePath(string imageId, string size = "original")
    {
        var imageFolder = Path.Combine(_env.ContentRootPath, uploadFolderName, imageId);
        if (size == "original")
        {
            var files = Directory.GetFiles(imageFolder, "original_*");
            return files.Length > 0 ? files[0] : null!;
        }

        var sizePath = Path.Combine(imageFolder, $"{size}.webp");
        return File.Exists(sizePath) ? sizePath : null!;

    }

}
