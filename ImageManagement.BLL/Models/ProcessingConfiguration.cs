namespace ImageManagement.BLL.Models;
public class ProcessingConfiguration
{
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    public long MaxFileSize { get; set; }
    public DeviceSize[] TargetSizes { get; set; } = Array.Empty<DeviceSize>();
    public string ConversionFormat { get; set; } = "webp";

}
