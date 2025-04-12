namespace ImageManagement.Domain.Entities;

public class Images : BaseEntity
{
    public string UniqueID { get; set; }
    public Guid UserID { get; set; }
    public ExifData ExifData { get; set; }
    public virtual User User { get; set; }
}
