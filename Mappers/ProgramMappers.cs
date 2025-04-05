using sport_app_backend.Dtos;
using sport_app_backend.Models.Program;

namespace sport_app_backend.Mappers;

public static class ProgramMappers
{
    public static WorkoutProgramResponseDto ToProgramResponseDto(this WorkoutProgram workoutProgram)
    {
        return new WorkoutProgramResponseDto
        {
            Id = workoutProgram.Id,
            Title = workoutProgram.Title,
            PaymentId = workoutProgram.PaymentId,
            StartDate = workoutProgram.StartDate,
            ProgramDuration = workoutProgram.ProgramDuration,
            ProgramLevel = workoutProgram.ProgramLevel,
            ProgramPriorities = workoutProgram.ProgramPriorities.Select(x => x.ToString()).ToList(),
            GeneralWarmUp = workoutProgram.GeneralWarmUp,
            DedicatedWarmUp = workoutProgram.DedicatedWarmUp,
            EndDate = workoutProgram.EndDate,
            Status = workoutProgram.Status.ToString(),
            Duration = workoutProgram.Duration,
            Description = workoutProgram.Description,
            ProgramInDays = workoutProgram.ProgramInDays ?? [],
        };
    }
}
