using sport_app_backend.Models;

namespace sport_app_backend.Interface;

public interface IStorage
{
    Task<ApiResponse> RemovePhoto(string url);
    Task<ApiResponse> UploadImage(IFormFile image, string url,string folderName);
    Task<ApiResponse> UploadFile(IFormFile file, string url, string? folderName);
}