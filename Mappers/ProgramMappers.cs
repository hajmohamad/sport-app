using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
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

    public static List<ProgramInDay> ToListOfProgramInDays(this List<ProgramInDayDto> programInDays)
    {
        return programInDays.Select(x => x.ToProgramInDay()).ToList();
        
        
    }
    public static ProgramInDay ToProgramInDay(this ProgramInDayDto programInDayDto)
    {
        return new ProgramInDay
        {
            Id = programInDayDto.Id,
            ForWhichDay = programInDayDto.ForWhichDay,
            AllExerciseInDays = programInDayDto.AllExerciseInDays.Select(x=>x.ToSingleExercise()).ToList()
        };
    }

    public static SingleExercise ToSingleExercise(this SingleExerciseDto singleExerciseDto)
    {
        return new SingleExercise
        {
            Id = singleExerciseDto.Id,
            Set = singleExerciseDto.Set,
            Rep = singleExerciseDto.Rep,
            ExerciseId = singleExerciseDto.ExerciseId
        };
    }   
}