using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos.WorkoutProgramTemplateDto;
using sport_app_backend.Interface.Coach;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Program.WorkoutProgramTemplate;

namespace sport_app_backend.Repository;

public class WorkoutProgramTemplateRepository(ApplicationDbContext context) : IWorkoutProgramTemplateRepository
{
    public async Task<ApiResponse> CreateTemplate(int coachId, WorkoutProgramTemplateCreateDto dto)
    {
        try
        {
            var coachExists = await context.Coaches.AnyAsync(x => x.Id == coachId);
            if (!coachExists)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = "مربی یافت نشد."
                };
            }

            var template = new WorkoutProgramTemplate
            {
                CoachId = coachId,
                Title = dto.Title
            };

            await context.WorkoutProgramTemplates.AddAsync(template);
            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Result = template.ToDetailDto(),
                Message = "قالب با موفقیت ایجاد شد."
            };
        }
        catch (Exception e)
        {
            return new ApiResponse
            {
                Action = false,
                Message = e.Message
            };
        }
    }

    public async Task<ApiResponse> UpdateTemplate(int coachId, int templateId, WorkoutProgramTemplateUpdateDto dto)
    {
        try
        {
            var template = await context.WorkoutProgramTemplates
                .Include(x => x.ProgramInDays)
                    .ThenInclude(x => x.AllExerciseInDays)
                .FirstOrDefaultAsync(x => x.Id == templateId && x.CoachId == coachId);

            if (template is null)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = "قالب یافت نشد."
                };
            }

            // حذف کامل ساختار قبلی
            if (template.ProgramInDays.Count > 0)
            {
                var oldExercises = template.ProgramInDays.SelectMany(x => x.AllExerciseInDays).ToList();
                context.TemplateSingleExercises.RemoveRange(oldExercises);
                context.TemplateProgramInDays.RemoveRange(template.ProgramInDays);
            }

            template.Description = dto.Description.Trim();
            template.ProgramDuration = dto.ProgramDuration;
            template.ProgramLevel = Enum.Parse<ProgramLevel>(dto.ProgramLevel, ignoreCase: true);
            template.ProgramPriority = Enum.Parse<ProgramPriority>(dto.ProgramPriority, ignoreCase: true);
            template.IsCompleted = dto.IsCompleted;
            template.ProgramInDays = dto.Days.ToTemplateDays();

            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Result = template.ToDetailDto(),
                Message = "قالب با موفقیت بروزرسانی شد."
            };
        }
        catch (Exception e)
        {
            return new ApiResponse
            {
                Action = false,
                Message = e.Message
            };
        }
    }

    public async Task<ApiResponse> DeleteTemplate(int coachId, int templateId)
    {
        try
        {
            var template = await context.WorkoutProgramTemplates
                .Include(x => x.ProgramInDays)
                    .ThenInclude(x => x.AllExerciseInDays)
                .FirstOrDefaultAsync(x => x.Id == templateId && x.CoachId == coachId);

            if (template is null)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = "قالب یافت نشد."
                };
            }

            var exercises = template.ProgramInDays.SelectMany(x => x.AllExerciseInDays).ToList();
            context.TemplateSingleExercises.RemoveRange(exercises);
            context.TemplateProgramInDays.RemoveRange(template.ProgramInDays);
            context.WorkoutProgramTemplates.Remove(template);

            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "قالب با موفقیت حذف شد."
            };
        }
        catch (Exception e)
        {
            return new ApiResponse
            {
                Action = false,
                Message = e.Message
            };
        }
    }

    public async Task<ApiResponse> GetTemplateById(int coachId, int templateId)
    {
        try
        {
            var template = await context.WorkoutProgramTemplates
                .AsNoTracking()
                .Include(x => x.ProgramInDays)
                    .ThenInclude(x => x.AllExerciseInDays)
                .FirstOrDefaultAsync(x => x.Id == templateId && x.CoachId == coachId);

            if (template is null)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = "قالب یافت نشد."
                };
            }

            return new ApiResponse
            {
                Action = true,
                Result = template.ToDetailDto(),
                Message = "دیتا پیدا شد"
            };
        }
        catch (Exception e)
        {
            return new ApiResponse
            {
                Action = false,
                Message = e.Message
            };
        }
    }

    public async Task<ApiResponse> GetAllTemplates(int coachId)
    {
        try
        {
            var query = context.WorkoutProgramTemplates
                .AsNoTracking()
                .Where(x => x.CoachId == coachId)
                .Include(x => x.ProgramInDays);

           

            var result = await query
                .OrderByDescending(x => x.Id)
                .Select(x => x.ToListDto())
                .ToListAsync();

            return new ApiResponse
            {
                Action = true,
                Result = result,
                Message = "دیتا پیدا شد"
            };
        }
        catch (Exception e)
        {
            return new ApiResponse
            {
                Action = false,
                Message = e.Message
            };
        }
    }

    public async Task<ApiResponse> ApplyTemplateToProgram(int coachId, int templateId, int paymentId)
    {
        try
        {
            var template = await context.WorkoutProgramTemplates
                .Include(x => x.ProgramInDays)
                    .ThenInclude(x => x.AllExerciseInDays)
                .FirstOrDefaultAsync(x => x.Id == templateId && x.CoachId == coachId);

            if (template is null)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = "قالب یافت نشد."
                };
            }

            var program = await context.WorkoutPrograms
                .Include(x => x.ProgramInDays)
                    .ThenInclude(x => x.AllExerciseInDays)
                .FirstOrDefaultAsync(x => x.PaymentId == paymentId && x.CoachId == coachId);

            if (program is null)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = "برنامه یافت نشد."
                };
            }

            // پاک کردن ساختار قبلی برنامه
            if (program.ProgramInDays.Count > 0)
            {
                var oldExercises = program.ProgramInDays.SelectMany(x => x.AllExerciseInDays).ToList();
                context.SingleExercises.RemoveRange(oldExercises);
                context.ProgramInDays.RemoveRange(program.ProgramInDays);
            }

            // کپی عمیق از Template به Program
            program.Title = template.Title;
            program.Description = template.Description;
            program.ProgramDuration = template.ProgramDuration;
            program.ProgramLevel = template.ProgramLevel;
            program.ProgramPriority = template.ProgramPriority;
            program.ProgramInDays = template.ProgramInDays.Select(day => new ProgramInDay
            {
                ForWhichDay = day.ForWhichDay,
                AllExerciseInDays = day.AllExerciseInDays.Select(ex => new SingleExercise
                {
                    ExerciseId = ex.ExerciseId,
                    RepType = ex.RepType,
                    Description = ex.Description,
                    Reps = ex.Reps.ToList()
                }).ToList()
            }).ToList();

            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "قالب با موفقیت روی برنامه اعمال شد."
            };
        }
        catch (Exception e)
        {
            return new ApiResponse
            {
                Action = false,
                Message = e.Message
            };
        }
    }
}
