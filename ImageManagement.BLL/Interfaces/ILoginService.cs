using ImageManagement.Common.DTOs;

namespace ImageManagement.BLL.Interfaces;
public interface ILoginService
{
    Task<ResponseResult> LoginAsync(string username, string password);

}
