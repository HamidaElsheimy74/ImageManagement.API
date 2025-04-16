using ImageManagement.API.Validators;

namespace ImageManagement.API.DTOs;

public class UploadImagesRequest
{
    [MaxFileCount(5)]
    public List<IFormFile> Files { get; set; }
}
