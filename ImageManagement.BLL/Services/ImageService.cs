using ImageManagement.BLL.Helpers.Interfaces;
using ImageManagement.BLL.Interfaces;
using ImageManagement.BLL.Models;
using ImageManagement.Common.DTOs;
using ImageManagement.Common.Errors;
using ImageManagement.Domain.Entities;
using ImageManagement.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace ImageManagement.BLL.Services;
public class ImageService : IImageService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<ImageService> _logger;
    private readonly IConfiguration _config;
    private readonly IImageProcessingHelpers _imageProcessingHelpers;
    public ImageService(IFileStorageService fileStorageService, ILogger<ImageService> logger,
        IConfiguration config, IImageProcessingHelpers imageProcessingHelpers)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
        _config = config;
        _imageProcessingHelpers = imageProcessingHelpers;
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
                Data = exifData == null ? null : new ExifData()
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

        var results = new List<ResponseResult>();
        var processingConfig = _imageProcessingHelpers.GetProcessingConfiguration(_config);

        foreach (var file in files)
        {
            var result = await ProcessSingleFileAsync(file, processingConfig);
            results.Add(result);
        }

        return results;
    }

    public async Task<ResponseResult> GetResizedImageAsync(string imageId, string size)
    {
        try
        {
            if (!_imageProcessingHelpers.IsValidSize(size, _config))
            {
                return new ResponseResult(ErrorsHandler.Invalid_Size, null!, 400);
            }

            var filePath = _fileStorageService.GetImagePath(imageId, size);

            if (!await _imageProcessingHelpers.FileExistsAsync(filePath))
            {
                return new ResponseResult(ErrorsHandler.Image_Not_Found, null!, 404);
            }

            var (stream, contentType) = await _imageProcessingHelpers.ReadImageFileAsync(filePath, size);
            return new ResponseResult()
            {
                Data = (stream, contentType),
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

    private async Task<ResponseResult> ProcessSingleFileAsync(IFormFile file, ProcessingConfiguration config)
    {
        try
        {
            var validationResult = _imageProcessingHelpers.ValidateFile(file, config);
            if (validationResult != null)
            {
                return validationResult;
            }

            var imageId = Guid.NewGuid().ToString();
            await using var imageStream = file.OpenReadStream();

            var originalPath = await _fileStorageService.StoreImageAsync(imageStream, imageId, file.FileName);
            imageStream.Position = 0;

            await _imageProcessingHelpers.ProcessImageAndMetadata(imageStream, originalPath, imageId, config, _fileStorageService);

            return new ResponseResult
            {
                Data = imageId,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing file {file.FileName}");
            return new ResponseResult
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"{file.FileName} {ErrorsHandler.Internal_Server_Error}"
            };
        }
    }


}
