namespace ImageManagement.BLL.Models;

public class DeviceSize
{
    public string Name { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public Size ToSize() => new Size(Width, Height);
}