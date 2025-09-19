using System.Globalization;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Actions;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Question.A_Question;

namespace sport_app_backend.Mappers;

public static class ProgramMappers
{
    public static ActivityDto ToActivityDto(this Activity activity)
    {
        return new ActivityDto()
        {
            ActivityCategory = activity.ActivityCategory.ToString(),
            CaloriesLost = activity.CaloriesLost,
            Date = activity.Date.ToString(CultureInfo.InvariantCulture),
            Duration = activity.Duration,
            Distance = activity.Distance,
            Name = activity.Name

        };
    }
    
    public static WorkoutProgramResponseDto ToProgramResponseDto(this WorkoutProgram workoutProgram)
    {
        return new WorkoutProgramResponseDto
        {
            Id = workoutProgram.Id,
            Title = workoutProgram.Title,
            PaymentId = workoutProgram.PaymentId,
            StartDate = workoutProgram.StartDate,
            ProgramDuration = workoutProgram.ProgramDuration,
            ProgramLevel = workoutProgram.ProgramLevel.ToString() ,
            ProgramPriorities = workoutProgram.ProgramPriorities.Select(x => x.ToString()).ToList(),
            GeneralWarmUp = workoutProgram.GeneralWarmUp.Select(c=>c.ToString()).ToList(),
            #pragma warning disable CS8601 // Possible null reference assignment.
            DedicatedWarmUp = workoutProgram.DedicatedWarmUp.ToString()??"",      
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

    public static AllPaymentResponseDto ToAllWorkoutProgramResponseDto(this WorkoutProgram workoutProgram)
    {
        return new AllPaymentResponseDto
        {   PaymentId = workoutProgram.PaymentId,
            PaymentStatus = workoutProgram.Payment.PaymentStatus.ToString(),
            Name = workoutProgram.Coach.User.FirstName + " " + workoutProgram.Coach.User.LastName,
            Amount = workoutProgram.Payment.Amount.ToString(CultureInfo.CurrentCulture),
            DateTime = workoutProgram.Payment.PaymentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ImageProfile = workoutProgram.Coach.User.ImageProfile,
            CoachServiceTitle = workoutProgram.Payment.CoachService.Title,
            WorkoutProgramStatus = workoutProgram.Status.ToString()
        };

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



    public static ExerciseFeedback ToExerciseFeedback(this ExerciseFeedbackDto feedbackDto)
    {
        var exerciseFeedback = new ExerciseFeedback
        {
            SingleExerciseId = feedbackDto.SingleExerciseId,
            IsPositive = feedbackDto.IsPositive,
            TrainingSessionId = feedbackDto.TrainingSessionId
        };

        if (!string.IsNullOrEmpty(feedbackDto.NegativeReason))
        {
            exerciseFeedback.NegativeReason = Enum.Parse<NegativeFeedbackReason>(feedbackDto.NegativeReason);
        }

        return exerciseFeedback;
    }


    public static ExerciseChangeRequest ToExerciseChangeRequest(this ExerciseChangeDto requestDto)
    {
        var exerciseChangeRequest =  new ExerciseChangeRequest
        {
            SingleExerciseId = requestDto.SingleExerciseId,
            TrainingSessionId = requestDto.TrainingSessionId,
        };
        if (!string.IsNullOrEmpty(requestDto.Reason))
        {
            exerciseChangeRequest.Reason = Enum.Parse<ExerciseChangeReason>(requestDto.Reason );

        }

        return exerciseChangeRequest;

    }
    public static AllTrainingSessionDto ToAllTrainingSessionDto(this TrainingSession trainingSession)
    {
        return new AllTrainingSessionDto()
        {
            Id = trainingSession.Id,
            DayNumber = trainingSession.DayNumber,
            TrainingSessionStatus = trainingSession.TrainingSessionStatus.ToString(),
            ExerciseCompletionBitmap = trainingSession.ExerciseCompletionBitmap.GetExerciseStatusArray()
        };
    }
    public static TrainingSessionDto ToTrainingSessionDto(this TrainingSession trainingSession,double finalCalories)
    {
        return new TrainingSessionDto
        {

            Id = trainingSession.Id,
            DayNumber = trainingSession.DayNumber,
            TrainingSessionStatus = trainingSession.TrainingSessionStatus.ToString(),
            ExerciseCompletionBitmap = trainingSession.ExerciseCompletionBitmap.GetExerciseStatusArray(),
            ProgramInDayId = trainingSession.ProgramInDayId,
            ProgramInDay = trainingSession.ProgramInDay.ToTrainingSessionProgramInDayDto(),
            CaloriesLost = finalCalories
        };
    }
    private static TrainingSessionProgramInDayDto ToTrainingSessionProgramInDayDto(this ProgramInDay programInDay){
        return new TrainingSessionProgramInDayDto
        {
            Id = programInDay.Id,
            ForWhichDay = programInDay.ForWhichDay,
            AllExerciseInDays = programInDay.AllExerciseInDays.Select(x=>x.ToTrainingSessionSingleExerciseDto()).ToList()
        };
    }
    
    private static TrainingSessionSingleExerciseDto ToTrainingSessionSingleExerciseDto(this SingleExercise singleExercise){
        return new TrainingSessionSingleExerciseDto
        {
            Id = singleExercise.Id,
            Set = singleExercise.Set,
            Rep = singleExercise.Rep,
            ExerciseId = singleExercise.ExerciseId,
            Exercise = singleExercise.Exercise.ToExerciseDto()
        };
    }
    public static int[] GetExerciseStatusArray(this byte[] bitmap)
    {
        int[] result = new int[bitmap.Length];

        for (int i = 0; i < bitmap.Length; i++)
        {
            if (bitmap[i] == 0xFF)
                result[i] = 1; // حرکت انجام شده
            else if (bitmap[i] == 0x00)
                result[i] = 0; // حرکت انجام نشده
            else
                result[i] = 0; // یا می‌تونیم مقدار نادرست رو نادیده بگیریم
        }

        return result;
        
    }
    public static string ToShamsiDateString(this DateTime date)
    {
        var pc = new PersianCalendar();

        var year = pc.GetYear(date);
        var month = pc.GetMonth(date);
        var day = pc.GetDayOfMonth(date);

      
        return $"{year}/{month:D2}/{day:D2}";
    }
    public static string ToPersianString(this ProgramLevel level)
    {
        return level switch
        {
            ProgramLevel.Beginner => "مبتدی",
            ProgramLevel.Intermediate => "متوسط",
            ProgramLevel.SemiAdvanced => "نیمه پیشرفته",
            ProgramLevel.Advanced => "پیشرفته",
            ProgramLevel.UltraAdvanced => "فوق پیشرفته",
            _ => level.ToString() // اگر ترجمه‌ای وجود نداشت، نام انگلیسی را برمی‌گرداند
        };
    }
    public static string GetFatPercentageRange( this Gender gender ,int bodyFormNumber)
    {
        if (gender == Gender.MALE)
        {
            // محدوده‌ها برای مردان
            return bodyFormNumber switch
            {
                1 => "4-6%",
                2 => "7-10%",
                3 => "11-15%",
                4 => "16-23%",
                5 => "31-40%",
                6 => ">40%",

                _ => "نامشخص" // برای مقادیر ورودی نامعتبر
            };
        }
        else // if (gender == Gender.FEMALE)
        {
            // محدوده‌ها برای زنان
            return bodyFormNumber switch
            {
                1 => "10-15%",
                2 => "16-20%",
                3 => "21-25%",
                4 => "26-30%",
                5 => "31-35%",
                6 => "36-45%",
                7 => ">40%",


                _ => "نامشخص" // برای مقادیر ورودی نامعتبر
            };
        }
    }

}
