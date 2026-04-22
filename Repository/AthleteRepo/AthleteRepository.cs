using System.Globalization;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Controller;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Dtos.ZarinPal;
using sport_app_backend.Dtos.ZarinPal.Verify;
using sport_app_backend.Interface;
using sport_app_backend.Interface.Athlete;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Account.Athlete;
using sport_app_backend.Models.Actions;
using sport_app_backend.Models.Challenge_Achievement;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Question.A_Question;
using sport_app_backend.Models.TrainingPlan;

namespace sport_app_backend.Repository.AthleteRepo

{
    public class AthleteRepository(
        ApplicationDbContext context,
     
        ITokenService tokenService,
        ICalculator calculator) : IAthleteRepository
    {
        public async Task<ApiResponse> GetFaq()
        {
            var getFaq = await context.AthleteFaq.AsNoTracking().ToListAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "get CoachFaq",
                Result = getFaq
            };
        }

        public async Task<ApiResponse> WorkoutProgramFeedback(string phoneNumber, FeedbackWorkoutProgramDto feedbackWorkoutProgramDto)
        {
            var user = await context.Users.Include(a => a.Athlete)
                .ThenInclude(wp=>wp!.ActiveWorkoutProgram)
                .FirstOrDefaultAsync(a => a.PhoneNumber == phoneNumber);
            if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
            var athlete = user.Athlete;
            if (athlete is null)
                return new ApiResponse()
                    { Message = "User is not an athlete", Action = false }; 
            var workoutProgram = athlete.ActiveWorkoutProgram;
            if (workoutProgram is null)
            {
                return new ApiResponse()
                {
                    Action = false,
                    Message = "Workout program is not active",
                };
            }
            
            var feedBack = new WorkoutProgramFeedback()
            {
                AthleteId = athlete.Id,
                CouchId = workoutProgram.CoachId,
                AthleteName = athlete.User.FirstName + " " + athlete.User.LastName,
                WorkoutProgramId = workoutProgram.Id,
                WorkoutProgramName = workoutProgram.Title,
                Score = feedbackWorkoutProgramDto.Score,
                FeedBack = feedbackWorkoutProgramDto.FeedBack,
            };
            await context.WorkoutProgramFeedback.AddAsync(feedBack);
            await context.SaveChangesAsync();
            
            


            return new ApiResponse()
            {
                Action = true,
                Message = "Feedback workout program submitted successfully",
                Result = feedBack
            };
        }


        public async Task<ApiResponse> AthleteFirstQuestions(string phoneNumber,
            AthleteFirstQuestionsDto athleteFirstQuestionsDto)
        {
            var user = await context.Users.Include(a => a.Athlete)
                .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
            var athlete = user.Athlete;
            if (athlete is null)
                return new ApiResponse()
                    { Message = "User is not an athlete", Action = false }; // Ensure the user is an athlete
            athlete.Height = athleteFirstQuestionsDto.Height;
            athlete.CurrentWeight = athleteFirstQuestionsDto.CurrentWeight;
            var weightEntry = new WeightEntry()
            {
                Athlete = athlete,
                AthleteId = athlete.Id,
                CurrentDate = DateTime.Now,
                Weight = athleteFirstQuestionsDto.CurrentWeight
            };
            await context.WeightEntries.AddAsync(weightEntry);
            user.LastName = athleteFirstQuestionsDto.LastName;
            user.FirstName = athleteFirstQuestionsDto.FirstName;
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Athlete first questions submitted successfully",
                Action = true,
                Result = new
                {
                    Questions = true
                }
            };
        }

  


        public async Task<ApiResponse> GetAllPayments(string phoneNumber)
        {
            var paymentDtos = await context.WorkoutPrograms
                .AsNoTracking()
                .Where(wp => wp.Athlete.PhoneNumber == phoneNumber && wp.Status != WorkoutProgramStatus.REFUND)
                .OrderByDescending(wp => wp.Payment.PaymentDate)
                .Select(wp => new AllPaymentResponseDto
                {
                    PaymentId = wp.PaymentId,
                    PaymentStatus = wp.Payment.PaymentStatus.ToString(),
                    Name = wp.Coach.User.FirstName + " " + wp.Coach.User.LastName,
                    Amount = wp.Payment.Amount.ToString(),
                    DateTime = wp.Payment.PaymentDate.ToString("yyyy-MM-dd"),
                    ImageProfile = wp.Coach.User.ImageProfile,
                    CoachServiceTitle = wp.Payment.CoachService.Title,
                    WorkoutProgramStatus = wp.Status.ToString(),
                    WpKey = tokenService.HashEncode(wp.Id)
                })
                .ToListAsync();

            if (!paymentDtos.Any())
            {
                return new ApiResponse()
                {
                    Action = true,
                    Message = "No payment history found",
                    Result = new List<AllPaymentResponseDto>()
                };
            }
            
            return new ApiResponse()
            {
                Action = true,
                Message = "Payments found",
                Result = paymentDtos
            };
        }

        public async Task<ApiResponse> GetPayment(string phoneNumber, int paymentId)
        {
            var paymentData = await context.Payments
                .AsNoTracking()
                .Where(p => p.Id == paymentId && p.Athlete.PhoneNumber == phoneNumber)
                .Select(payment => new
                {
                    Payment = payment,
                    CoachUser = payment.Coach.User,
                    AthleteUser = payment.Athlete.User,
                    Athlete = payment.Athlete,
                    AthleteQuestionId = payment.AthleteQuestionId
                })
                .FirstOrDefaultAsync();

            if (paymentData == null)
            {
                return new ApiResponse { Message = "Payment not found for this user", Action = false };
            }

            var athleteQuestion = await context.AthleteQuestions.Where(aq => aq.Id == paymentData.AthleteQuestionId)
                .Include(aq => aq.InjuryArea).Include(im=>im.AthleteBodyImage).FirstOrDefaultAsync();
            var workoutProgram = await context.WorkoutPrograms.Where(wp => wp.PaymentId == paymentData.Payment.Id)
                .Include(z => z.ProgramInDays)
                .ThenInclude(z => z.AllExerciseInDays)
                .ThenInclude(e => e.Exercise)
                .FirstOrDefaultAsync();
            if (athleteQuestion == null)
            {
                return new ApiResponse { Message = "athleteQuestion not found for this user", Action = false };
            }

            if (workoutProgram == null)
            {
                return new ApiResponse { Message = "workoutProgram not found for this user", Action = false };
            }
            var Ear = calculator.BmrCalculator(new BmrRequestDto()
            {
                ActivityLevel = athleteQuestion.ActivityLevel,
                Age = DateTime.Today.Year - paymentData.AthleteUser.BirthDate.Year
                                          - (paymentData.AthleteUser.BirthDate.Date > DateTime.Today.AddYears(
                                              -(DateTime.Today.Year - paymentData.AthleteUser.BirthDate.Year))
                                              ? 1
                                              : 0),
                Gender = paymentData.AthleteUser.Gender,
                HeightCm = paymentData.Athlete.Height,
                WeightKg = paymentData.Athlete.CurrentWeight
            });

            var paymentResponseDto = new PaymentResponseDto
            {
                PaymentId = paymentData.Payment.Id,
                TransactionId = paymentData.Payment.Authority,
                PaymentStatus = paymentData.Payment.PaymentStatus.ToString(),
                Name = paymentData.CoachUser.FirstName + " " + paymentData.CoachUser.LastName,
                Amount = paymentData.Payment.Amount.ToString(CultureInfo.CurrentCulture),
                DateTime = paymentData.Payment.PaymentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Height = paymentData.Athlete.Height,
                ImageProfile = paymentData.CoachUser.ImageProfile ?? "",
                Gender = paymentData.AthleteUser.Gender.ToString(),
                BirthDate = paymentData.AthleteUser.BirthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                AthleteQuestion = athleteQuestion?.AthleteQuestionResponseWithBirthdayDto( paymentData.AthleteUser.BirthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),Ear)?? new AthleteQuestionResponseDto(),
                WorkoutProgram = workoutProgram.ToProgramResponseDto(),
                PdfLink= $"chaarset.ir/program/{tokenService.HashEncode(workoutProgram.Id)}",
                WpKey = tokenService.HashEncode(workoutProgram.Id),
            };

            return new ApiResponse { Message = "Payment details found", Action = true, Result = paymentResponseDto };
        }
        

        public async Task<ApiResponse> ActiveProgram(string phoneNumber, int paymentId)
        {
            var athlete = await context.Athletes.Include(a => a.WorkoutPrograms)
                .FirstOrDefaultAsync(a => a.PhoneNumber == phoneNumber&&a.WorkoutPrograms.Any(wp => wp.PaymentId == paymentId));

            if (athlete == null)
            {
                return new ApiResponse { Action = false, Message = "Athlete not found" };
            }

            var targetProgram = athlete.WorkoutPrograms.FirstOrDefault(w => w.PaymentId == paymentId);

            if (targetProgram == null)
            {
                return new ApiResponse { Action = false, Message = "Workout program not found" };
            }

            var allTrainingSessions = await context.TrainingSessions
                .Where(t => t.WorkoutProgramId ==  targetProgram.Id)
                .ToListAsync();

            Action<TrainingSession> resetTrainingSession = ts =>
            {
                ts.TrainingSessionStatus = TrainingSessionStatus.NOTSTARTED;
                var arrayBitMap = ts.ExerciseCompletionBitmap.ToArray();
                Array.Clear(arrayBitMap, 0, arrayBitMap.Length);
                ts.ExerciseCompletionBitmap = arrayBitMap;
            };

            if (targetProgram.Status == WorkoutProgramStatus.ACTIVE)
            {
                athlete.ActiveWorkoutProgramId = targetProgram.Id;
                allTrainingSessions.ForEach(resetTrainingSession);
                targetProgram.CompletedSessionCount = 0;
                await context.SaveChangesAsync();
                return new ApiResponse { Action = true, Message = "Program already active and reset." };
            }

            foreach (var program in athlete.WorkoutPrograms.Where(x => x.Status == WorkoutProgramStatus.ACTIVE))
            {
                program.Status = WorkoutProgramStatus.STOPPED;
            }

            switch (targetProgram.Status)
            {
                case WorkoutProgramStatus.WRITING:
                case WorkoutProgramStatus.NOTSTARTED:
                    return new ApiResponse
                        { Action = false, Message = "Workout program status is not acceptable for activation." };

                case WorkoutProgramStatus.STOPPED:
                case WorkoutProgramStatus.FINISHED:
                    allTrainingSessions.ForEach(resetTrainingSession);
                    break;

                case WorkoutProgramStatus.NOTACTIVE:
                    await AddTrainingSession(targetProgram.PaymentId);
                    break;
            }

            targetProgram.Status = WorkoutProgramStatus.ACTIVE;
            athlete.ActiveWorkoutProgramId = targetProgram.Id;
            targetProgram.StartDate = DateTime.Now;

            await context.SaveChangesAsync();
            return new ApiResponse { Action = true, Message = "Program activated successfully." };
        }

        private async Task AddTrainingSession(int paymentId)
        {
            var workoutProgram = await context.WorkoutPrograms
                .Include(p => p.Payment)
                .ThenInclude(p => p.AthleteQuestion)
                .Include(p => p.ProgramInDays)
                .ThenInclude(d => d.AllExerciseInDays)
                .FirstAsync(p => p.PaymentId == paymentId);

            var numberOfDay = workoutProgram.ProgramDuration *
                              workoutProgram.Payment.AthleteQuestion.DaysPerWeekToExercise;
            var programInDayList = workoutProgram.ProgramInDays;
            var programInDayCount = programInDayList.Count;
            workoutProgram.TotalSessionCount = numberOfDay;

            var sessions = new List<TrainingSession>();

            for (var day = 1; day <= numberOfDay; day++)
            {
                var index = day % programInDayCount;
                sessions.Add(new TrainingSession
                {
                    ProgramInDayId = programInDayList[index].Id,
                    ProgramInDay = programInDayList[index],
                    ExerciseCompletionBitmap = new byte[programInDayList[index].AllExerciseInDays.Count],
                    TrainingSessionStatus = TrainingSessionStatus.NOTSTARTED,
                    DayNumber = day,
                    WorkoutProgram = workoutProgram,
                    WorkoutProgramId = workoutProgram.Id
                });
            }

            await context.TrainingSessions.AddRangeAsync(sessions);
            await context.SaveChangesAsync();
        }



        public async Task<ApiResponse> ExerciseFeedBack(string phoneNumber, ExerciseFeedbackDto feedbackDto)
        {
            var athlete = await context.Athletes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);

            if (athlete is null)
            {
                return new ApiResponse() { Message = "ورزشکار یافت نشد!", Action = false };
            }


            var trainingSession = await context.TrainingSessions
                .AsNoTracking()
                .Include(ts => ts.WorkoutProgram)
                .Include(ts => ts.ProgramInDay)
                .ThenInclude(pid => pid.AllExerciseInDays)
                .FirstOrDefaultAsync(ts => ts.Id == feedbackDto.TrainingSessionId);


            if (trainingSession is null)
            {
                return new ApiResponse() { Message = "جلسه تمرینی یافت نشد!", Action = false };
            }

            if (trainingSession.WorkoutProgram?.AthleteId != athlete.Id)
            {
                return new ApiResponse() { Message = "شما به این جلسه تمرینی دسترسی ندارید.", Action = false };
            }

            if (trainingSession.ProgramInDay.AllExerciseInDays.All(se => se.Id != feedbackDto.SingleExerciseId))
            {
                return new ApiResponse() { Message = "تمرین مشخص شده در این جلسه وجود ندارد.", Action = false };
            }


            var feedback = feedbackDto.ToExerciseFeedback();
            feedback.AthleteId = athlete.Id;
            feedback.CoachId = trainingSession.WorkoutProgram.CoachId;

            await context.ExerciseFeedbacks.AddAsync(feedback);
            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "فیدبک شما با موفقیت ثبت شد.",
            };
        }


        public async Task<ApiResponse> ChangeExercise(string phoneNumber, ExerciseChangeDto dto)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "Athlete not found", Action = false };
            var singleExercise = await context.SingleExercises.Include(p => p.ProgramInDay)
                .ThenInclude(w => w.WorkoutProgram)
                .FirstOrDefaultAsync(s => s.Id == dto.SingleExerciseId);
            var exerciseChangeRequest = dto.ToExerciseChangeRequest();
            exerciseChangeRequest.AthleteId = athlete.Id;
            exerciseChangeRequest.SingleExerciseId = dto.SingleExerciseId;
            exerciseChangeRequest.TrainingSessionId = dto.TrainingSessionId;
            if (singleExercise?.ProgramInDay?.WorkoutProgram != null)
                exerciseChangeRequest.CoachId = singleExercise.ProgramInDay.WorkoutProgram.CoachId;

            await context.ExerciseChangeRequests.AddAsync(exerciseChangeRequest);
            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "Exercise change request saved.",
            };
        }


        public async Task<ApiResponse> GetAllTrainingSession(string phoneNumber)
{
    var resultData = await context.WorkoutPrograms
        .AsNoTracking()
        .Where(wp => wp.Athlete.PhoneNumber == phoneNumber && wp.Status == WorkoutProgramStatus.ACTIVE)
        .Select(wp => new
        {
            ProgramName = wp.Title,
            wp.StartDate,
            wp.ProgramDuration,
            wp.TotalSessionCount,
            wp.CompletedSessionCount,
            CoachWebsite = wp.Coach.WebSiteUrl ?? "chaarset.ir",
            wp.WorkoutProgramFeedbackId,
            TrainingSessions = wp.TrainingSessions.Select(ts => new AllTrainingSessionDto
            {
                Id = ts.Id,
                DayNumber = ts.DayNumber,
                TrainingSessionStatus = ts.TrainingSessionStatus.ToString(),
                ExersiceCount = ts.ExerciseCompletionBitmap.GetExerciseStatusArray().Length
            }).ToList()
        })
        .FirstOrDefaultAsync();

    if (resultData == null)
    {
        return new ApiResponse() { Message = "Active workout program not found", Action = true, Result = null };
    }
    double completionPercent = 0;
    if (resultData.TotalSessionCount > 0)
    {
        completionPercent = (double)resultData.CompletedSessionCount / resultData.TotalSessionCount;
    }

    var shouldGetFeedback = completionPercent >= 0.30 && resultData.WorkoutProgramFeedbackId == null;

    string? renewalMessage = null;
    var now = DateTime.Now;

    if (resultData.StartDate != null)
    {
        var programEndDate = resultData.StartDate.Value.AddDays(resultData.ProgramDuration * 7);
        var daysSinceEnd = (now - programEndDate).Days;

        var remainingSessions = resultData.TotalSessionCount - resultData.CompletedSessionCount;
        var isProgramExpired = now >= programEndDate;
        var isSeventyPercentCompleted = completionPercent >= 0.70;

        if (isProgramExpired)
        {
            renewalMessage =
                $"{daysSinceEnd} روز از آخرین برنامه تمرینی که دریافت کردی گذشته. ";
        }
        else if (isSeventyPercentCompleted)
        {
            renewalMessage =
                $"کمتر از {remainingSessions} جلسه از برنامه تمرینیت باقی مونده. ";
        }
    }

    return new ApiResponse()
    {
        Action = true,
        Message = "Training sessions retrieved successfully",
        Result = new
        {
            ToAllTrainingSession = resultData.TrainingSessions,
            resultData.ProgramName,
            RenewalMessage = renewalMessage,
            resultData.CoachWebsite,
            shouldGetFeedback
        }
    };
}
        public async Task<ApiResponse> GetTrainingSession(string phoneNumber, int trainingSessionId)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(a => a.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "Athlete not found", Action = false };

            var trainingSession = await context.TrainingSessions
                .Include(ts => ts.WorkoutProgram)
                .Include(p => p.ProgramInDay)
                .ThenInclude(a => a.AllExerciseInDays).ThenInclude(e => e.Exercise)
                .FirstOrDefaultAsync(z => z.Id == trainingSessionId);

            if (trainingSession is null)
                return new ApiResponse() { Message = "trainingSession not found", Action = false };

            var finalCalories = _CalculateCaloriesInternal(trainingSession, athlete.CurrentWeight, false);
            var time = trainingSession.ProgramInDay.AllExerciseInDays.Sum(st => st.Reps.Count)*60;
            
            


            return new ApiResponse()
            {
                Action = true,
                Message = "get TrainingSession",
                Result = trainingSession.ToTrainingSessionDto(finalCalories,time)
            };
        }

        public async Task<ApiResponse> DoTrainingSession(string phoneNumber, int trainingSessionId, int exerciseNumber)
        {
            var trainingSession = await context.TrainingSessions
                .FirstOrDefaultAsync(z => z.Id == trainingSessionId);
            if (trainingSession is null)
                return new ApiResponse() { Message = "trainingSession not found", Action = false };

            trainingSession.TrainingSessionStatus = TrainingSessionStatus.INPROGRESS;

            var bitmap = trainingSession.ExerciseCompletionBitmap.ToArray();
            bitmap[exerciseNumber] = 0xFF;
            trainingSession.ExerciseCompletionBitmap = bitmap;
            var allCompleted = trainingSession.ExerciseCompletionBitmap.All(b => b == 0xFF);
            trainingSession.TrainingSessionStatus =
                allCompleted ? TrainingSessionStatus.COMPLETED : TrainingSessionStatus.INPROGRESS;

            await context.SaveChangesAsync();

            return new ApiResponse()
            {
                Action = true,
                Message = "Do Training session",
                Result = trainingSession.ToAllTrainingSessionDto()
            };
        }

        public async Task<ApiResponse> FinishTrainingSession(string phoneNumber,
            FinishTrainingSessionDto finishTrainingSessionDto)
        {
            try
            {
                var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
                if (athlete is null) return new ApiResponse() { Message = "Athlete not found", Action = false };

                var trainingSession = await context.TrainingSessions
                    .Include(ts => ts.WorkoutProgram)
                    .Include(ts => ts.ProgramInDay)
                    .ThenInclude(pid => pid.AllExerciseInDays)
                    .ThenInclude(se => se.Exercise) // اطمینان از بارگذاری اطلاعات هر حرکت
                    .FirstOrDefaultAsync(z => z.Id == finishTrainingSessionDto.TrainingSessionId);

                if (trainingSession is null)
                    return new ApiResponse() { Message = "trainingSession not found", Action = false };
                var athleteWeight = athlete.CurrentWeight;

                // var finalCalories = _CalculateCaloriesInternal(trainingSession, athleteWeight, false);


                trainingSession.TrainingSessionStatus = TrainingSessionStatus.COMPLETED;
                trainingSession.WorkoutProgram.LastExerciseDate = DateTime.Now;
                trainingSession.WorkoutProgram.CompletedSessionCount++;

                var activity = new Activity()
                {
                    AthleteId = athlete.Id,
                    Duration = finishTrainingSessionDto.Duration,
                    CaloriesLost = finishTrainingSessionDto.CaloriesLost,
                    ActivityCategory = ActivityCategory.EXERCISE,
                    Name = finishTrainingSessionDto.TrainingSessionName,
                    Date = DateTime.Now.Date
                };

                await context.Activities.AddAsync(activity);
                await context.SaveChangesAsync();

                return new ApiResponse()
                {
                    Action = true,
                    Message = "Finish Training session",
                    Result = activity.ToActivityDto()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = $"Error finishing: {ex.Message}"
                };
            }
        }


        public async Task<ApiResponse> FeedbackTrainingSession(string phoneNumber,
            FeedbackTrainingSessionDto feedbackTrainingSessionDto)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "Athlete not found", Action = false };
            var trainingSession = await context.TrainingSessions
                .FirstOrDefaultAsync(z => z.Id == feedbackTrainingSessionDto.TrainingSessionId);
            if (trainingSession is null)
                return new ApiResponse() { Message = "trainingSession not found", Action = false };
            if (trainingSession.TrainingSessionStatus != TrainingSessionStatus.COMPLETED)
                return new ApiResponse() { Message = "trainingSession not completed", Action = false };
            trainingSession.ExerciseFeeling =
                Enum.Parse<ExerciseFeeling>(feedbackTrainingSessionDto.ExerciseFeeling ?? string.Empty);
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "Feedback Training session"
            };
        }


        public async Task<ApiResponse> ResetTrainingSession(string phoneNumber, int trainingSessionId)
        {
            var trainingSession = await context.TrainingSessions
                .FirstOrDefaultAsync(z => z.Id == trainingSessionId);
            if (trainingSession is null)
                return new ApiResponse() { Message = "trainingSession not found", Action = false };

            trainingSession.TrainingSessionStatus = TrainingSessionStatus.NOTSTARTED;

            var arrayBitMap = trainingSession.ExerciseCompletionBitmap.ToArray();
            Array.Clear(arrayBitMap, 0, arrayBitMap.Length);
            trainingSession.ExerciseCompletionBitmap = arrayBitMap;
            await context.SaveChangesAsync();

            return new ApiResponse()
            {
                Action = true,
                Message = "Training session Retested",
                Result = trainingSession.ToAllTrainingSessionDto()
            };
        }


        public async Task<ApiResponse> CalculateCalories(string phoneNumber, int trainingSessionId)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null) return new ApiResponse() { Message = "Athlete not found", Action = false };


            var trainingSession = await context.TrainingSessions
                .Include(ts => ts.WorkoutProgram)
                .Include(ts => ts.ProgramInDay)
                .ThenInclude(pid => pid.AllExerciseInDays)
                .ThenInclude(se => se.Exercise)
                .FirstOrDefaultAsync(z => z.Id == trainingSessionId);

            if (trainingSession is null)
                return new ApiResponse() { Message = "trainingSession not found", Action = false };

            var athleteWeight = athlete.CurrentWeight;

            var finalCalories = _CalculateCaloriesInternal(trainingSession, athleteWeight, true);
            return new ApiResponse()
            {
                Action = true,
                Message = "Calories calculated successfully for completed exercises.",
                Result = finalCalories
            };
        }



        private static double _CalculateCaloriesInternal(
            TrainingSession trainingSession, double athleteWeight, bool completedExercisesOnly)
        {
            const double restMet = 1.3;


            // 1. میانگین‌گیری از پارامترها بر اساس اهداف برنامه
            var priorities = trainingSession.WorkoutProgram.ProgramPriorities;
            var avgParams = new TrainingGoalParameter
            {
                RestBetweenSetsSec = priorities.Average(p => TrainingGoalParameters.Parameters[p].RestBetweenSetsSec),
                RestBetweenMovesSec = priorities.Average(p => TrainingGoalParameters.Parameters[p].RestBetweenMovesSec),
                TimePerRepSec = priorities.Average(p => TrainingGoalParameters.Parameters[p].TimePerRepSec),
                EpocPercent = priorities.Average(p => TrainingGoalParameters.Parameters[p].EpocPercent)
            };

            double totalCaloriesActiveAndRestSets = 0;
            var exercisesCountInCalculation = 0;
            var allExercises = trainingSession.ProgramInDay.AllExerciseInDays;

            // ۲. محاسبه کالری
            for (var i = 0; i < allExercises.Count; i++)
            {
                // اگر فقط حرکات انجام شده مد نظر است، بیت‌مپ را چک کن
                if (completedExercisesOnly)
                {
                    if (i >= trainingSession.ExerciseCompletionBitmap.Length ||
                        trainingSession.ExerciseCompletionBitmap[i] != 0xFF)
                    {
                        continue; // اگر حرکت انجام نشده، از آن بگذر
                    }
                }

                var exercise = allExercises[i];
                if (exercise.Exercise == null) continue;

                exercisesCountInCalculation++;
                var numberOfRep = exercise.Reps.Sum();

                var workTimeSec = numberOfRep* avgParams.TimePerRepSec;
                var restBetweenSetsSec = (exercise.Reps.Count > 1) ? (exercise.Reps.Count - 1) * avgParams.RestBetweenSetsSec : 0;

                // اگر MET صفر بود، یک مقدار پیش‌فرض در نظر می‌گیریم
                var exerciseMet = (exercise.Exercise.Met == 0) ? 3.0 : exercise.Exercise.Met;
                var caloriesActive = (exerciseMet * athleteWeight * workTimeSec) / 3600.0;
                var caloriesRestSets = (restMet * athleteWeight * restBetweenSetsSec) / 3600.0;

                totalCaloriesActiveAndRestSets += caloriesActive + caloriesRestSets;
            }

            if (exercisesCountInCalculation == 0 && completedExercisesOnly)
            {
                return 0.0;
            }


            var totalRestBetweenMovesSec = (exercisesCountInCalculation > 1)
                ? (exercisesCountInCalculation - 1) * avgParams.RestBetweenMovesSec
                : 0;
            var caloriesRestMoves = (restMet * athleteWeight * totalRestBetweenMovesSec) / 3600.0;


            var totalCaloriesBeforeEpoc = totalCaloriesActiveAndRestSets + caloriesRestMoves;


            var finalCalories = totalCaloriesBeforeEpoc * (1 + avgParams.EpocPercent);

            return Math.Round(finalCalories, 2);
        }
    }
}