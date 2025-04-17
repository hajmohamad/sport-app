using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Interface;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;


namespace sport_app_backend.Repository
{
    public class CoachRepository(ApplicationDbContext context) : ICoachRepository
    {
        public async Task<ApiResponse> AddCoachingServices(string phoneNumber, AddCoachServiceDto addCoachingServiceDto)
        {
         
            var coach = await context.Coaches.Include(c => c.CoachingServices).FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach is null) return new ApiResponse() { Message = "User is not a coach", Action = false };// Ensure the user is a coach
            var coachingService = addCoachingServiceDto.ToCoachService(coach);
            coach.CoachingServices ??= [];
            coach.CoachingServices.Add(coachingService);
            context.CoachServices.Add(coachingService);
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Coaching Service added successfully",
                Action = true
                
            };
        }
        public async Task<ApiResponse> SubmitCoachQuestions(string phoneNumber, CoachQuestionDto coachQuestionDto)
        {  
            var user = await context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
            var coach = user.Coach;
            if (coach == null) return new ApiResponse() { Message = "User is not a coach", Action = false };// Ensure the user is a coach
            user.FirstName=coachQuestionDto.FirstName;
            user.LastName=coachQuestionDto.LastName;
            var coachQuestion = coachQuestionDto.ToCoachQuestion(user);
            coach.CoachQuestion = coachQuestion;
            await context.CoachQuestions.AddAsync(coachQuestion);
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Coach questions submitted successfully",
                Action = true,
                Result=new
                {
                    Questions=true
                }
                
            };
        }

        public async Task<ApiResponse> UpdateCoachingService(string phoneNumber,int id, AddCoachServiceDto addCoachingServices)
        {

            var coach = await context.Coaches.Include(x=>x.CoachingServices).FirstOrDefaultAsync(x=>x.PhoneNumber==phoneNumber);
            if(coach is null) return new ApiResponse() { Message = "User is not a coach", Action = false };// Ensure the user is a coach
            var coachingService = coach.CoachingServices.FirstOrDefault(x => x.Id == id);
            if (coachingService is null) return new ApiResponse() { Message = "Coaching Service not found", Action = false };

            var payments = context.Payments.Include(c=>c.CoachService).Where(c => c.Id == id).ToList();
            if(payments.Count != 0)
            {
                coachingService.IsDeleted = true;
                var newCoachService = addCoachingServices.ToCoachService(coach);
                newCoachService.NumberOfSell = coachingService.NumberOfSell;
                coach.CoachingServices.Add(newCoachService);
                context.CoachServices.Add(newCoachService);
            }else{
                coachingService.UpdateCoachServices(addCoachingServices);
                
                
            }
        
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Coaching Service updated successfully",
                Action = true,
                Result = coachingService.ToCoachingServiceResponse()
            };

        }

        public async Task<ApiResponse> DeleteCoachingService(string phoneNumber, int id)
        {
            var coach = await context.Coaches.Include(x => x.CoachingServices).FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (coach is null) return new ApiResponse() { Message = "User is not a coach", Action = false };// Ensure the user is a coach
            var coachingService = coach.CoachingServices.FirstOrDefault(x => x.Id == id);
            if (coachingService is null) return new ApiResponse() { Message = "Coaching Service not found", Action = false };
            coachingService.IsDeleted = true;
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Coaching Service deleted successfully",
                Action = true,
                Result = coachingService.ToCoachingServiceResponse()
            };
        }

        public async Task<ApiResponse> GetAllPayment(string phoneNumber)
        {
            var payments = await context.Payments
                .Include(p => p.Coach)
                .ThenInclude(c => c!.User)
                .Include(p => p.Athlete)
                .ThenInclude(a => a!.User)
                .Include(p => p.CoachService)
                .Include(p => p.WorkoutProgram)
                .Where(p => 
                    p.Coach != null &&
                    p.Coach.PhoneNumber == phoneNumber &&
                    p.PaymentStatus == PaymentStatus.SUCCESS &&
                    p.WorkoutProgram != null &&
                    (
                        p.WorkoutProgram.Status == WorkoutProgramStatus.NOTSTARTED ||
                        p.WorkoutProgram.Status == WorkoutProgramStatus.WRITING
                    )
                )
                .ToListAsync();
           
            
           
            return new ApiResponse()
            {
                Message = "Payments found",
                Action = true,
                Result = payments.Select(x=>x.ToAllPaymentResponseDto())
            };
        }

        public async Task<ApiResponse> GetPayment(string phoneNumber, int paymentId)
        {
            var payment = await context.Payments
                .Include(p => p.Coach)  // بارگذاری Coach
                .Include(p => p.Athlete)  // بارگذاری Athlete
                .ThenInclude(a => a!.User)
                .Include(a=>a.AthleteQuestion)// بارگذاری User داخل Athlete
                .ThenInclude(I=> I!.InjuryArea)
                .Include(w=>w.WorkoutProgram)
                .FirstOrDefaultAsync(p => p.Coach != null && p.Coach.PhoneNumber == phoneNumber&& p.Id==paymentId);
            if(payment is null) return new ApiResponse() { Message = "Payment not found", Action = false };
            var result = payment.ToPaymentResponseDto();
            if (result.WorkoutProgram!.ProgramInDays.Count == 0)
            {
             result.WorkoutProgram.ProgramInDays.Add(new ProgramInDayDto()
             {
                 
                ForWhichDay = 1,
                 AllExerciseInDays = []

             });   
            }
            return new ApiResponse()
            {
                Message = "Payment found",
                Action = true,
                Result = result
            };

        }



        public async Task<ApiResponse> GetExercises()
        {
            var exercise = await context.Exercises.ToListAsync();
            return new ApiResponse()
            {
                Message = "Exercises found",
                Action = true,
                Result = exercise.Select(x=>x.ToAllExerciseResponseDto())
            };
        }

        public async Task<ApiResponse> GetProfile(string phoneNumber)
        {
            var user = await context.Users
                .Include(u => u.Coach)
                .ThenInclude(c=>c.CoachingServices)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (user?.Coach == null) return new ApiResponse { Action = false, Message = "Coach not found" };
            var coachingService = user.Coach.CoachingServices.Where(x=>x.IsDeleted==false).ToList();
            var coachingServiceDto = coachingService.Select(x => x.ToCoachingServiceResponse()).ToList();
            var payments  = await context.Payments.Include(p=>p.Athlete).Include(p=>p.WorkoutProgram).
                Where(p => p.CoachId == user.Coach.Id && p.WorkoutProgram != null && p.WorkoutProgram.Status!=WorkoutProgramStatus.WRITING).ToListAsync();
            var numberOfProgram = payments.Count(p => p.WorkoutProgram != null);
            var numberOfAthlete = payments.Select(x=>x.AthleteId).Distinct().Count();
            return new ApiResponse
            {
                Action = true, Message = "Coach found",
                Result = user.ToCoachProfileResponseDto(coachingServiceDto, payments,numberOfAthlete,numberOfProgram)
            };

        }

        public async Task<ApiResponse> SaveWorkoutProgram(string phoneNumber, int paymentId, WorkoutProgramDto workoutProgramDto)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach == null) return new ApiResponse { Action = false, Message = "Coach not found" };
            var workoutProgram = await context.WorkoutPrograms.Include(x=>x.ProgramInDays)
                .ThenInclude(z=>z.AllExerciseInDays)
              .FirstOrDefaultAsync(p => p.Id == paymentId);
            if(workoutProgram is null) return new ApiResponse{ Action = false, Message = "Payment not found" };
            workoutProgram.ProgramInDays = workoutProgramDto.Days.ToListOfProgramInDays();

            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "workout program saved"
            };




        }

        public async Task<ApiResponse> GetWorkoutProgram(string phoneNumber, int paymentId)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach == null) return new ApiResponse { Action = false, Message = "Coach not found" };
            var workoutProgram = await context.WorkoutPrograms.Include(x=>x.ProgramInDays)
                .ThenInclude(z=>z.AllExerciseInDays)
                .FirstOrDefaultAsync(p => p.Id == paymentId);
            if(workoutProgram is null) return new ApiResponse{ Action = false, Message = "Payment not found" };

            return new ApiResponse()
            {
                Action = true,
                Message = "workout program found",
                Result = workoutProgram.ProgramInDays.ToProgramInDayDto()
            };
            
        }
    }
}
