namespace ImageManagement.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; }
    public string Password { get; set; }
    public bool Blocked { get; set; }
    public bool IsActive { get; set; }

    public virtual List<Images> Images { get; set; }
}
