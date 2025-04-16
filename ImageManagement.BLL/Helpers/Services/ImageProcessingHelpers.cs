using ImageManagement.BLL.Helpers.Interfaces;
using ImageManagement.BLL.Models;
using ImageManagement.Common.DTOs;
using ImageManagement.Common.Errors;
using ImageManagement.Domain.Entities;
using ImageManagement.Infrastructure.Interfaces;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp.Formats.Webp;
using System.Data;

namespace ImageManagement.BLL.Helpers.Services;
public class ImageProcessingHelpers : IImageProcessingHelpers
{
    public ResponseResult? ValidateFile(IFormFile file, ProcessingConfiguration config)
    {
        var extension = Path.GetExtension(file.FileName).ToUpperInvariant();
        if (!config.AllowedExtensions.Contains(extension))
        {
            return new ResponseResult
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = $"{file.FileName} {ErrorsHandler.Invalid_File_Type}"
            };
        }

        if (file.Length > config.MaxFileSize)
        {
            return new ResponseResult
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = $"{file.FileName} {ErrorsHandler.File_Too_Large}"
            };
        }

        return null;
    }

    public async Task ProcessImageAndMetadata(Stream imageStream, string originalPath, string imageId, ProcessingConfiguration config, IFileStorageService fileStorageService)
    {
        using var image = await Image.LoadAsync(imageStream);

        var exifData = ExtractExifData(originalPath);
        await fileStorageService.StoreExifDataAsync(imageId, exifData);

        foreach (var target in config.TargetSizes)
        {
            var resizedImage = image.Clone(x => x.Resize(new ResizeOptions
            {
                Size = target.ToSize(),
                Mode = ResizeMode.Max
            }));

            var outputPath = Path.Combine(
                Path.GetDirectoryName(originalPath)!,
                $"{target.Name}.{config.ConversionFormat}");

            await resizedImage.SaveAsync(outputPath, new WebpEncoder());
        }
    }

    public ProcessingConfiguration GetProcessingConfiguration(IConfiguration config)
    {
        return new ProcessingConfiguration
        {
            AllowedExtensions = config["AllowedExtensions"]?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .ToArray() ?? Array.Empty<string>(),
            MaxFileSize = long.TryParse(new DataTable().Compute(config["MaxFileSize"], null)?.ToString(),
                         out var maxSize) ? maxSize : 2 * 1024 * 1024,
            TargetSizes = config.GetSection("TargetSizes").Get<DeviceSize[]>() ?? Array.Empty<DeviceSize>(),
            ConversionFormat = config["ConversionFormat"] ?? "WebP"
        };
    }

    public ExifData ExtractExifData(string imagePath)
    {
        var exifData = new ExifData();

        var directories = ImageMetadataReader.ReadMetadata(imagePath);

        foreach (var directory in directories)
        {
            if (directory is ExifIfd0Directory exifDir)
            {
                exifData.Make = exifDir.GetDescription(ExifDirectoryBase.TagMake)!;
                exifData.Model = exifDir.GetDescription(ExifDirectoryBase.TagModel)!;
            }

            if (directory is GpsDirectory gpsDir)
            {
                var location = gpsDir.GetGeoLocation();
                if (location != null)
                {
                    exifData.Latitude = location.Latitude.ToString();
                    exifData.Longitude = location.Longitude.ToString();
                }
            }
        }

        return exifData;
    }

    public bool IsValidSize(string size, IConfiguration config)
    {
        var allowedSizes = config["AllowedSizes"]?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  ?? Array.Empty<string>();
        return allowedSizes.Contains(size.ToLower());
    }

    public async Task<(MemoryStream Stream, string ContentType)> ReadImageFileAsync(string filePath, string size)
    {
        var memoryStream = new MemoryStream();
        await using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
        {
            await fileStream.CopyToAsync(memoryStream);
        }
        memoryStream.Position = 0;

        var contentType = "image/webp";

        return (memoryStream, contentType);
    }

    public async Task<bool> FileExistsAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return await Task.Run(() => File.Exists(path));
    }

}
