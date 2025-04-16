using ImageManagement.API.DTOs;
using ImageManagement.BLL.Interfaces;
using ImageManagement.Common.DTOs;
using ImageManagement.Common.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net;

namespace ImageManagement.API.Controllers;

public class ImagesController : BaseAPIController
{
    private readonly IImageService _imageService;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(
        IImageService imageService,
        ILogger<ImagesController> logger)
    {
        _imageService = imageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload images
    /// </summary>
    /// <param name="files"></param>
    /// <returns></returns>
    [HttpPost("Upload")]
    [Authorize]
    [EnableRateLimiting("strict")]
    public async Task<IActionResult> UploadImages([FromForm] UploadImagesRequest request)
    {
        try
        {
            if (!ModelState.IsValid || request is null)
            {

                return BadRequest(new ResponseResult
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = ErrorsHandler.Invalid_UploadModel
                });
            }
            if (request.Files == null || request.Files.Count == 0)
            {
                return BadRequest(new ResponseResult
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = ErrorsHandler.Empty_Files_List
                });
            }

            var results = await _imageService.ProcessAndStoreImageAsync(request.Files);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading images");
            return StatusCode(200, new ResponseResult
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = ErrorsHandler.Internal_Server_Error
            });
        }
    }
    /// <summary>
    /// Get image info by imageId
    /// </summary>
    /// <param name="imageId"></param>
    /// <returns></returns>

    [HttpGet("ImageMetadata/{imageId}")]
    [Authorize]
    [EnableRateLimiting("strict")]
    public async Task<IActionResult> GetImageInfo(string imageId)
    {
        try
        {
            if (string.IsNullOrEmpty(imageId) || string.IsNullOrWhiteSpace(imageId))
            {
                return BadRequest(new ResponseResult
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = ErrorsHandler.Empty_image_ID
                });
            }
            var result = await _imageService.GetImageInfoAsync(imageId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting image info for {imageId}");
            return StatusCode(200, new ResponseResult
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = ErrorsHandler.Internal_Server_Error
            });
        }
    }

    /// <summary>
    /// download image by imageId and size
    /// </summary>
    /// <param name="imageId"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    [HttpGet("{imageId}/Download/{size}")]
    [Authorize]
    [EnableRateLimiting("strict")]
    public async Task<IActionResult> DownloadImage(string imageId, string size)
    {
        try
        {

            if (string.IsNullOrEmpty(imageId) || string.IsNullOrWhiteSpace(imageId)
                || string.IsNullOrEmpty(size) || string.IsNullOrWhiteSpace(size))
                return BadRequest(new ResponseResult
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = ErrorsHandler.Empty_image_ID + " or " + ErrorsHandler.Empty_image_Size
                });


            var result = await _imageService.GetResizedImageAsync(imageId, size);

            if (result.StatusCode == StatusCodes.Status200OK)
            {
                var tuple = result.Data as (MemoryStream, string)?;
                var imageStream = tuple!.Value.Item1;
                var contentType = tuple!.Value.Item2;
                return File(imageStream, contentType, $"{imageId}_{size}");

            }
            else
                return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading image {imageId}");
            return StatusCode(200, new ResponseResult
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = ErrorsHandler.Internal_Server_Error
            });
        }

    }
}
