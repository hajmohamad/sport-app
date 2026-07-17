using sport_app_backend.Dtos;
using sport_app_backend.Models;

namespace sport_app_backend.Interface.Coach;

public interface IChangePhotosRepository
{
    Task<ApiResponse> GetAllChangePhotos(int coachId);
    Task<ApiResponse> GetChangePhotoById(int coachId, int id);
    Task<ApiResponse> AddChangePhoto(int coachId, IFormFile file, AddAthleteChangePhotoDto dto);
    Task<ApiResponse> EditChangePhoto(int coachId, IFormFile? file, EditAthleteChangePhotoDto dto);
    Task<ApiResponse> DeleteChangePhoto(int coachId, int id);

}