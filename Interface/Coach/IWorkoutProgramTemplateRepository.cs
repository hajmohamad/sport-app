using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Dtos.WorkoutProgramTemplateDto;
using sport_app_backend.Models;

namespace sport_app_backend.Interface.Coach;


public interface IWorkoutProgramTemplateRepository
{
    Task<ApiResponse> CreateTemplate(int coachId, WorkoutProgramTemplateCreateDto dto);
    Task<ApiResponse> UpdateTemplate(int coachId, int templateId, WorkoutProgramTemplateUpdateDto dto);
    Task<ApiResponse> DeleteTemplate(int coachId, int templateId);
    Task<ApiResponse> GetTemplateById(int coachId, int templateId);
    Task<ApiResponse> GetAllTemplates(int coachId);

    Task<ApiResponse> ApplyTemplateToProgram(int coachId, int dtoTemplateId, int dtoPaymentId);
    Task<ApiResponse> RemoveTemplateFromProgram(int coachId, int paymentId);

}
