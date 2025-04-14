using ImageManagement.Domain.Entities;
using System.Text.Json;

namespace ImageManagement.Test.TestUtilities;
public static class TestUtitities
{
    public static Stream CreateTestStream(string content)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(content);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }


    public static void CreateTestExifFile(string imageId, ExifData data, string testContentRoot,
                                            string uploadFolderName, string exifFileName)
    {
        var dirPath = Path.Combine(testContentRoot, uploadFolderName, imageId);
        Directory.CreateDirectory(dirPath);

        var filePath = Path.Combine(dirPath, exifFileName);
        File.WriteAllText(filePath, JsonSerializer.Serialize(data));
    }

    public static void CreateTestImage(string imageId, string size, string uploadFolderName, string testContentRoot, string extension = "webp")
    {
        var imageFolder = Path.Combine(testContentRoot, uploadFolderName, imageId);
        Directory.CreateDirectory(imageFolder);

        var fileName = size == "original" ? $"original_test.{extension}" : $"{size}.{extension}";
        File.WriteAllText(Path.Combine(imageFolder, fileName), "test content");
    }
}
