using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Dtos.WorkoutProgramTemplateDto;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Program.WorkoutProgramTemplate;

namespace sport_app_backend.Mappers;

public static class TemplateMappers
{
    public static List<TemplateProgramInDay> ToTemplateDays(this List<ProgramInDayDto> days)
    {
        return days.Select(d => new TemplateProgramInDay
        {
            Id = d.Id,
            ForWhichDay = d.ForWhichDay,
            AllExerciseInDays = d.AllExerciseInDays.Select(e => new TemplateSingleExercise
            {
                Id = e.Id,
                ExerciseId = e.ExerciseId,
                RepType = Enum.Parse<RepType>(e.RepType, ignoreCase: true),
                Description = e.Description,
                Reps = e.Reps
            }).ToList()
        }).ToList();
    }

    public static List<ProgramInDayDto> ToProgramInDayDtos(this List<TemplateProgramInDay> days)
    {
        return days.Select(d => new ProgramInDayDto
        {
            Id = d.Id,
            ForWhichDay = d.ForWhichDay,
            AllExerciseInDays = d.AllExerciseInDays.Select(e => new SingleExerciseDto
            {
                Id = e.Id,
                ExerciseId = e.ExerciseId,
                RepType = e.RepType.ToString(),
                Description = e.Description,
                Reps = e.Reps
            }).ToList()
        }).ToList();
    }

    public static WorkoutProgramTemplateListDto ToListDto(this WorkoutProgramTemplate template)
    {
        return new WorkoutProgramTemplateListDto
        {
            Id = template.Id,
            Title = template.Title,
            Description = template.Description,
            ProgramDuration = template.ProgramDuration,
            ProgramLevel = template.ProgramLevel.ToString(),
            ProgramPriority = template.ProgramPriority.ToString(),
            IsCompleted = template.IsCompleted,
            DaysCount = template.ProgramInDays?.Count ?? 0
        };
    }

    public static WorkoutProgramTemplateDetailDto ToDetailDto(this WorkoutProgramTemplate template)
    {
        return new WorkoutProgramTemplateDetailDto
        {
            Id = template.Id,
            Title = template.Title,
            Description = template.Description,
            ProgramDuration = template.ProgramDuration,
            ProgramLevel = template.ProgramLevel.ToString(),
            ProgramPriority = template.ProgramPriority.ToString(),
            IsCompleted = template.IsCompleted,
            Days = template.ProgramInDays.ToProgramInDayDtos()
        };
    }
}
