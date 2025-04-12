using ImageManagement.BLL.Interfaces;
using ImageManagement.BLL.Models;
using ImageManagement.Common.DTOs;
using ImageManagement.Common.Errors;
using ImageManagement.Domain.Entities;
using ImageManagement.Infrastructure.Interfaces;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Formats.Webp;
using System.Data;


namespace ImageManagement.BLL.Services;
public class ImageService : IImageService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<ImageService> _logger;
    private readonly IConfiguration _config;
    public ImageService(IFileStorageService fileStorageService, ILogger<ImageService> logger, IConfiguration config)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
        _config = config;
    }
    public async Task<ResponseResult> GetImageInfoAsync(string imageId)
    {
        try
        {
            if (!await _fileStorageService.ImageExistsAsync(imageId))
                return new ResponseResult
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = ErrorsHandler.Image_Not_Found
                };

            var exifData = await _fileStorageService.GetExifDataAsync(imageId);

            return new ResponseResult
            {
                Data = new ExifData()
                {
                    Latitude = exifData.Latitude,
                    Longitude = exifData.Longitude,
                    Make = exifData.Make,
                    Model = exifData.Model,

                },

            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return new ResponseResult
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = ErrorsHandler.Internal_Server_Error
            };
        }
    }

    public async Task<List<ResponseResult>> ProcessAndStoreImageAsync(List<IFormFile> files)
    {
        try
        {
            var results = new List<ResponseResult>();
            var allowedExtensions = _config["AllowedExtensions"]!.Split(',').ToArray();
            long maxSize;
            var AllowedSize = new DataTable().Compute(_config["MaxFileSize"], null).ToString();
            maxSize = long.TryParse(AllowedSize, out maxSize) ? maxSize : 2 * 1024 * 1024;
            foreach (var file in files)
            {
                try
                {
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        results.Add(new ResponseResult
                        {
                            StatusCode = StatusCodes.Status400BadRequest,
                            Message = $"{file.FileName} {ErrorsHandler.Invalid_File_Type}"
                        });
                        continue;
                    }
                    if (file.Length > maxSize)
                    {
                        results.Add(new ResponseResult
                        {
                            StatusCode = StatusCodes.Status400BadRequest,
                            Message = $"{file.FileName} {ErrorsHandler.File_Too_Large}"
                        });
                        continue;
                    }


                    var imageId = Guid.NewGuid().ToString();
                    var imageStream = file.OpenReadStream();
                    var originalFileName = file.FileName;
                    var originalPath = await _fileStorageService.StoreImageAsync(imageStream, imageId, originalFileName);

                    imageStream.Position = 0;

                    using (var image = await Image.LoadAsync(imageStream))
                    {
                        var exifData = ExtractExifData(originalPath);
                        await _fileStorageService.StoreExifDataAsync(imageId, exifData);

                        var targetSizes = _config.GetSection("TargetSizes").Get<DeviceSize[]>();

                        foreach (var target in targetSizes!)
                        {
                            var resizedImage = image.Clone(x => x.Resize(new ResizeOptions
                            {
                                Size = target.ToSize(),
                                Mode = ResizeMode.Max
                            }));

                            var outputPath = Path.Combine(
                            Path.GetDirectoryName(originalPath)!,
                                $"{target.Name}.{_config.GetSection("ConversionFormat")}");

                            await resizedImage.SaveAsync(outputPath, new WebpEncoder());
                        }
                    }
                    results.Add(new ResponseResult()
                    {
                        Data = imageId,
                        StatusCode = StatusCodes.Status200OK
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing file {file.FileName}");
                    results.Add(new ResponseResult
                    {
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Message = $"{file.FileName} {ErrorsHandler.Internal_Server_Error}"
                    });
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return new List<ResponseResult>{ new ResponseResult
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = ErrorsHandler.Internal_Server_Error
            } };
        }
    }


    public async Task<ResponseResult> GetResizedImageAsync(string imageId, string size)
    {
        try
        {
            var AllowedSizes = _config["AllowedSizes"]!.Split(',').ToArray();
            if (!AllowedSizes.Contains(size.ToLower()))
            {
                return new ResponseResult(ErrorsHandler.Invalid_Size, null!, 400);
            }

            var filePath = _fileStorageService.GetImagePath(imageId, size);

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return new ResponseResult(ErrorsHandler.Image_Not_Found, null!, 404);
            }

            var contentType = size == "original"
                ? GetContentType(Path.GetExtension(filePath))
                : "image/webp";

            var memoryStream = new MemoryStream();
            await using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                await fileStream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            return new ResponseResult()
            {
                Data = (memoryStream, contentType),
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return new ResponseResult
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = ErrorsHandler.Internal_Server_Error
            };
        }
    }

    private string GetContentType(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }


    private ExifData ExtractExifData(string imagePath)
    {
        var exifData = new ExifData();

        try
        {
            var directories = ImageMetadataReader.ReadMetadata(imagePath);

            foreach (var directory in directories)
            {
                if (directory is ExifIfd0Directory exifDir)
                {
                    exifData.Make = exifDir.GetDescription((int)ExifDirectoryBase.TagMake)!;
                    exifData.Model = exifDir.GetDescription((int)ExifDirectoryBase.TagModel)!;
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
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error extracting EXIF data: {ex.Message}");
        }

        return exifData;
    }
}
