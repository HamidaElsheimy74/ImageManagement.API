using ImageManagement.Domain.Entities;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.Json;

namespace ImageManagement.Test.TestUtilities;
public static class TestUtilities
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


    public static IFormFile CreateTestFormImage(string imageName, string contentType)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write("Test file content");
        writer.Flush();
        stream.Position = 0;

        return new FormFile(stream, 0, stream.Length, "file", imageName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    public static void CreateTestImageWithGpsData(string imagePath, double latitude, double longitude, string make, string model)

    {
        using (var image = new Image<Rgba32>(100, 100))
        {
            var exifProfile = new ExifProfile();
            if (make != null)
                exifProfile.SetValue(ExifTag.Make, make);

            if (model != null)
                exifProfile.SetValue(ExifTag.Model, model);
            // Set required GPS tags
            exifProfile.SetValue(ExifTag.GPSLatitudeRef, latitude >= 0 ? "N" : "S");
            exifProfile.SetValue(ExifTag.GPSLatitude, ToRationalDegrees(Math.Abs(latitude)));

            exifProfile.SetValue(ExifTag.GPSLongitudeRef, longitude >= 0 ? "E" : "W");
            exifProfile.SetValue(ExifTag.GPSLongitude, ToRationalDegrees(Math.Abs(longitude)));

            exifProfile.SetValue(ExifTag.GPSVersionID, new byte[] { 2, 3, 0, 0 });

            image.Metadata.ExifProfile = exifProfile;
            image.SaveAsJpeg(imagePath);
        }
    }



    public static string CreateTestImageWithNoExif(string testImagePath)
    {
        var imagePath = Path.Combine(testImagePath, $"{Guid.NewGuid()}.jpg");

        using (var image = new Image<Rgba32>(100, 100))
        {
            image.SaveAsJpeg(imagePath);
        }

        return imagePath;
    }

    private static Rational[] ToRationalDegrees(double decimalDegrees)
    {
        var degrees = (uint)decimalDegrees;
        var remaining = decimalDegrees - degrees;
        var minutes = (uint)(remaining * 60);
        var seconds = (remaining * 60 - minutes) * 60;

        return new Rational[]
        {
        new Rational(degrees, 1),
        new Rational(minutes, 1),
        new Rational((uint)(seconds * 100), 100)
        };
    }
}
