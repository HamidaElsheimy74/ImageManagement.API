using ImageManagement.Domain.Entities;

namespace ImageManagement.Infrastructure.Interfaces;

public interface IFileStorageService
{
    Task<string> StoreImageAsync(Stream imageStream, string imageId, string originalFileName);
    Task StoreExifDataAsync(string imageId, ExifData exifData);
    Task<ExifData> GetExifDataAsync(string imageId);
    Task<bool> ImageExistsAsync(string imageId);
    string GetImagePath(string imageId, string size = "original");
}
