namespace ImageManagement.Infrastructure.Models;

public class JWT
{
    public string Secret { get; set; }
    public string Issuer { get; set; }
    public double ExpirationTime { set; get; }
    public string Audience { get; set; }

}