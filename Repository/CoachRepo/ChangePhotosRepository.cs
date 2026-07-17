using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Interface.Coach;
using sport_app_backend.Models;

namespace sport_app_backend.Repository.CoachRepo;

public class ChangePhotosRepository (
    ApplicationDbContext context,
    ISmsService smsService,
    IStorage storage,
    ITokenService token,
    ICalculator calculator,
    IExerciseCacheService exerciseCache,
    IZarinPal zarinPal
) : IChangePhotosRepository
{
    #region changePhoto
        public async Task<ApiResponse> GetAllChangePhotos(int coachId)
        {
        
            var photos = await context.AthleteChangePhotos
                .Where(x => x.CoachId == coachId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.Title, x.Description, x.PhotoUrl, x.CreatedAt })
                .ToListAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "Change photos fetched successfully",
                Result = photos
            };
        }

        public async Task<ApiResponse> GetChangePhotoById(int coachId, int id)
        {
       

            var photo = await context.AthleteChangePhotos
                .Where(x => x.CoachId == coachId && x.Id == id)
                .Select(x => new { x.Id, x.Title, x.Description, x.PhotoUrl, x.CreatedAt })
                .FirstOrDefaultAsync();

            if (photo is null) return new ApiResponse { Message = "Change photo not found", Action = false };

            return new ApiResponse
            {
                Action = true,
                Message = "Change photo fetched successfully",
                Result = photo
            };
        }

        public async Task<ApiResponse> AddChangePhoto(int coachId, IFormFile file, AddAthleteChangePhotoDto dto)
        {
            if (file.Length == 0)
                return new ApiResponse { Message = "File is required", Action = false };

        

            var upload = await storage.UploadImage(file, "","change-photos"); 
            // upload.Url
            if (!upload.Action)
            {
                return new ApiResponse()
                {
                    Action = false,
                    Message = "File upload failed",
                };

            }

            var entity = new AthleteChangePhoto
            {
                Title = dto.Title,
                Description = dto.Description,
                PhotoUrl = upload.Result.ToString(),
                CoachId = coachId
            };

            await context.AthleteChangePhotos.AddAsync(entity);
            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "Change photo added successfully",
                Result = new { entity.Id }
            };
        }

        public async Task<ApiResponse> EditChangePhoto(int coachId, IFormFile? file, EditAthleteChangePhotoDto dto)
        {
        
            var entity = await context.AthleteChangePhotos
                .FirstOrDefaultAsync(x => x.CoachId == coachId && x.Id == dto.Id);

            if (entity is null) return new ApiResponse { Message = "Change photo not found", Action = false };

            entity.Title = dto.Title;
            entity.Description = dto.Description;

            if (file != null && file.Length > 0)
            {
            
                var upload = await storage.UploadImage(file,entity.PhotoUrl, "change-photos");
                if (!upload.Action)
                {
                    return new ApiResponse()
                    {
                        Action = false,
                        Message = "File upload failed",
                    };

                }
                entity.PhotoUrl = upload.Result.ToString();

            }

            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "Change photo updated successfully",
                Result = new { entity.Id }
            };
        }

        public async Task<ApiResponse> DeleteChangePhoto(int coachId, int id)
        {
            var entity = await context.AthleteChangePhotos
                .FirstOrDefaultAsync(x => x.CoachId == coachId && x.Id == id);

            if (entity is null) return new ApiResponse { Message = "Change photo not found", Action = false };
            await storage.RemovePhoto(entity.PhotoUrl);

            context.AthleteChangePhotos.Remove(entity);
            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "Change photo deleted successfully"
            };
        }
        

        #endregion
}