using System.ComponentModel.DataAnnotations;

namespace ImageManagement.API.Validators;

public class MaxFileCountAttribute : ValidationAttribute
{
    private readonly int _maxFiles;

    public MaxFileCountAttribute(int maxFiles)
    {
        _maxFiles = maxFiles;
    }

    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        if (value is List<IFormFile> files && files.Count > _maxFiles)
        {
            return new ValidationResult($"Maximum {_maxFiles} files allowed per request.");
        }
        return ValidationResult.Success;
    }
}
