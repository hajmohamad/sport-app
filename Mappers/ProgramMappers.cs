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
            ProgramLevel = workoutProgram.ProgramLevel ,
            ProgramPriorities = workoutProgram.ProgramPriorities.Select(x => x.ToString()).ToList(),
            GeneralWarmUp = workoutProgram.GeneralWarmUp,
            #pragma warning disable CS8601 // Possible null reference assignment.
            DedicatedWarmUp = workoutProgram.DedicatedWarmUp == "" ? null : workoutProgram.DedicatedWarmUp,      
            #pragma warning restore CS8601 // Possible null reference assignment.
            EndDate = workoutProgram.EndDate,
            Status = workoutProgram.Status.ToString(),
            Duration = workoutProgram.Duration,
            Description = workoutProgram.Description,
            ProgramInDays = workoutProgram.ProgramInDays.ToProgramInDayDto()

        };
    }

    public static List<ProgramInDay> ToListOfProgramInDays(this List<ProgramInDayDto> programInDays)
    {
        return programInDays.Select(x => x.ToProgramInDay()).ToList();
        
        
    }

    private static ProgramInDay ToProgramInDay(this ProgramInDayDto programInDayDto)
    {
        return new ProgramInDay
        {
            Id = programInDayDto.Id,
            ForWhichDay = programInDayDto.ForWhichDay,
            AllExerciseInDays = programInDayDto.AllExerciseInDays.Select(x=>x.ToSingleExercise()).ToList()
        };
    }

    private static SingleExercise ToSingleExercise(this SingleExerciseDto singleExerciseDto)
    {
        return new SingleExercise
        {
            Id = singleExerciseDto.Id,
            Set = singleExerciseDto.Set,
            Rep = singleExerciseDto.Rep,
            ExerciseId = singleExerciseDto.ExerciseId
        };
    }   
    
    public static List<ProgramInDayDto> ToProgramInDayDto(this List<ProgramInDay> programInDays){
        return programInDays.Select(x => x.ToProgramInDayDto()).ToList();
    }
    
    private static ProgramInDayDto ToProgramInDayDto(this ProgramInDay programInDay){
        return new ProgramInDayDto
        {
            Id = programInDay.Id,
            ForWhichDay = programInDay.ForWhichDay,
            AllExerciseInDays = programInDay.AllExerciseInDays.Select(x=>x.ToSingleExerciseDto()).ToList()
        };
    }
    
    private static SingleExerciseDto ToSingleExerciseDto(this SingleExercise singleExercise){
        return new SingleExerciseDto
        {
            Id = singleExercise.Id,
            Set = singleExercise.Set,
            Rep = singleExercise.Rep,
            ExerciseId = singleExercise.ExerciseId
        };
    }
    
}