using ImageManagement.Common.DTOs;
using Microsoft.AspNetCore.Http;

namespace ImageManagement.BLL.Interfaces;
public interface IImageService
{
    Task<List<ResponseResult>> ProcessAndStoreImageAsync(List<IFormFile> files);
    Task<ResponseResult> GetImageInfoAsync(string imageId);
    Task<ResponseResult> GetResizedImageAsync(string imageId, string size);
}
