using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Actions;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;

namespace sport_app_backend.Repository
{
    public class AdminRepository(ApplicationDbContext context) : IAdminRepository
    {
        public Task<ApiResponse> AddExercises(List<AddExercisesRequestDto> exercises)
        {
            var exercisesList = exercises.Select(x => x.ToExercises()).ToList();
            context.Exercises.AddRange(exercisesList);
            context.SaveChanges();
            return Task.FromResult(new ApiResponse()
            {
                Message = "Exercises added successfully",
                Action = true
            });

      
        }

        public async Task<ApiResponse> ConfirmTransactionId(string TransactionId)
        {
            var payment = await context.Payments.Include(p => p.Coach).Include(z=>z
                    .CoachService)
                .Include(p => p.Athlete).Include(payment => payment.WorkoutProgram).FirstOrDefaultAsync(x => x.Authority == TransactionId);
            if (payment is null) return new ApiResponse() { Message = "Payment not found", Action = false };
            payment.CoachService.NumberOfSell += 1;
            var workoutProgram = new WorkoutProgram()
            {
                Title = payment.CoachService.Title,
                Coach = payment.Coach,
                Athlete = payment.Athlete,
                AthleteId = payment.Athlete!.Id,
                CoachId = payment.Coach!.Id,
                Payment = payment,
                PaymentId = payment.Id

            };
            payment.WorkoutProgram = workoutProgram;
            await context.WorkoutPrograms.AddAsync(workoutProgram);
            payment.PaymentStatus = PaymentStatus.SUCCESS;
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Payment confirmed successfully",
                Action = true
            };

          
        }
        public async Task<ApiResponse> BackfillWorkoutProgramStats()
        {
            var allPrograms = await context.WorkoutPrograms
                .Include(p => p.TrainingSessions)
                .ToListAsync();

            int updatedProgramsCount = 0;

            foreach (var program in allPrograms)
            {
                program.TotalSessionCount = program.TrainingSessions.Count;

                program.CompletedSessionCount = program.TrainingSessions
                    .Count(ts => ts.TrainingSessionStatus == TrainingSessionStatus.COMPLETED);

              
                var lastExerciseActivity = await context.Activities
                    .Where(a => a.AthleteId == program.AthleteId && a.ActivityCategory == ActivityCategory.EXERCISE)
                    .OrderByDescending(a => a.Date)
                    .FirstOrDefaultAsync();
                
                if (lastExerciseActivity != null)
                {
                    program.LastExerciseDate = lastExerciseActivity.Date;
                }
                
                updatedProgramsCount++;
            }

            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = $"{updatedProgramsCount} برنامه تمرینی با موفقیت به‌روزرسانی و پر شد."
            };
        }

        public async Task<ApiResponse> VerifiedCoach(string coachPhoneNumber)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == coachPhoneNumber);
            if (coach is null) return new ApiResponse() { Message = "coach not found", Action = false };
            coach.Verified = true;
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "coach Verified successfully",
                Action = true
            }; 

        }
    }
    
}