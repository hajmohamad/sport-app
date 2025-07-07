using sport_app_backend.Models;

namespace sport_app_backend.Interface;

public interface ILiaraStorage
{
    Task<ApiResponse> RemovePhoto(string url);
    Task<ApiResponse> UploadImage(IFormFile image, string url);
}