using ImageManagement.BLL.Models;
using ImageManagement.Common.DTOs;
using ImageManagement.Domain.Entities;
using ImageManagement.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace ImageManagement.BLL.Helpers.Interfaces;
public interface IImageProcessingHelpers
{
    ResponseResult? ValidateFile(IFormFile file, ProcessingConfiguration config);
    Task ProcessImageAndMetadata(Stream imageStream, string originalPath, string imageId, ProcessingConfiguration config, IFileStorageService fileStorageService);
    ProcessingConfiguration GetProcessingConfiguration(IConfiguration config);
    ExifData ExtractExifData(string filePath);
    bool IsValidSize(string size, IConfiguration config);
    Task<(MemoryStream Stream, string ContentType)> ReadImageFileAsync(string filePath, string size);
    Task<bool> FileExistsAsync(string path);
}
