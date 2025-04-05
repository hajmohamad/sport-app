using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.TrainingPlan;

namespace sport_app_backend.Repository
{
    public class CoachRepository(ApplicationDbContext context) : ICoachRepository
    {
        public async Task<ApiResponse> AddCoachingPlane(string phoneNumber, AddCoachingPlaneDto addCoachingPlaneDto)
        {
         
            var coach = await context.Coaches.Include(c => c.CoachingPlans).FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach is null) return new ApiResponse() { Message = "User is not a coach", Action = false };// Ensure the user is a coach
            var coachingPlane = addCoachingPlaneDto.ToCoachPlane(coach);
            coach.CoachingPlans ??= [];
            coach.CoachingPlans.Add(coachingPlane);
            context.CoachesPlan.Add(coachingPlane);
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Coaching plane added successfully",
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
            var coachQuestion = new CoachQuestion
            {
                UserId = user.Id,
                User = user,
                Disciplines = coachQuestionDto.Disciplines,
                Motivations = coachQuestionDto.Motivations,
                WorkOnlineWithAthletes = coachQuestionDto.WorkOnlineWithAthletes,
                PresentsPracticeProgram = coachQuestionDto.PresentsPracticeProgram,
                TrackAthlete = coachQuestionDto.TrackAthlete,
                ManagingRevenue = coachQuestionDto.ManagingRevenue,
                DifficultTrackAthletes = coachQuestionDto.DifficultTrackAthletes,
                HardCommunicationWithAthletes = coachQuestionDto.HardCommunicationWithAthletes
            };
            coach.CoachQuestion = coachQuestion;
            context.CoachQuestions.Add(coachQuestion);
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Coach questions submitted successfully",
                Action = true
            };
        }

        public async Task<ApiResponse> UpdateCoachingPlane(string phoneNumber,int id, AddCoachingPlaneDto addCoachingPlaneDto)
        {

            var coach = await context.Coaches.Include(x=>x.CoachingPlans).FirstOrDefaultAsync(x=>x.PhoneNumber==phoneNumber);
            if(coach is null) return new ApiResponse() { Message = "User is not a coach", Action = false };// Ensure the user is a coach
            var coachingPlane = coach.CoachingPlans.FirstOrDefault(x => x.Id == id);
            if (coachingPlane is null) return new ApiResponse() { Message = "Coaching plane not found", Action = false };

            var payments = context.Payments.Include(c=>c.CoachPlan).Where(c => c.Id == id).ToList();
            if(payments.Count != 0)
            {
                coachingPlane.IsDeleted = true;
                var newCoachPlan = addCoachingPlaneDto.ToCoachPlane(coach);
                coach.CoachingPlans ??= [];
                coach.CoachingPlans.Add(newCoachPlan);
                context.CoachesPlan.Add(newCoachPlan);
            }else{
                coachingPlane.UpdateCoachingPlane(addCoachingPlaneDto);
                
                
            }
        
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Coaching plane updated successfully",
                Action = true,
                Result = coachingPlane
            };

        }

        public async Task<ApiResponse> DeleteCoachingPlane(string phoneNumber, int id)
        {
            var coach = await context.Coaches.Include(x => x.CoachingPlans).FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (coach is null) return new ApiResponse() { Message = "User is not a coach", Action = false };// Ensure the user is a coach
            var coachingPlane = coach.CoachingPlans.FirstOrDefault(x => x.Id == id);
            if (coachingPlane is null) return new ApiResponse() { Message = "Coaching plane not found", Action = false };
            coachingPlane.IsDeleted = true;
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Coaching plane deleted successfully",
                Action = true,
                Result = coachingPlane
            };
        }

        public async Task<ApiResponse> GetAllPayment(string phoneNumber)
        {
            var payments = await context.Payments
                .Include(p => p.Coach)  
                .ThenInclude(c => c!.User)  
                .Include(p => p.Athlete)  
                .ThenInclude(a => a!.User)  
                .Where(p => p.Coach != null && p.Coach.PhoneNumber == phoneNumber).ToListAsync();;
           
           
            
           
            return new ApiResponse()
            {
                Message = "Payments found",
                Action = true,
                Result = payments.Where(p=> p.PaymentStatus == PaymentStatus.SUCCESS).Select(x=>x.ToAllPaymentResponseDto())
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
                .FirstOrDefaultAsync(p => p.Coach != null && p.Coach.PhoneNumber == phoneNumber&& p.Id==paymentId);
            if(payment is null) return new ApiResponse() { Message = "Payment not found", Action = false };
            
            return new ApiResponse()
            {
                Message = "Payment found",
                Action = true,
                Result = payment.ToPaymentResponseDto()
            };
            
            
            

        }
    }
}
