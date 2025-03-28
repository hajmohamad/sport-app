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
         
            var coach = await context.Coaches.Include(c => c.Coachplans).FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach is null) return new ApiResponse() { Message = "User is not a coach", Action = false };// Ensure the user is a coach
            var coachingPlane = addCoachingPlaneDto.ToCoachPlane(coach);
            coach.Coachplans ??= [];
            coach.Coachplans.Add(coachingPlane);
            context.CoachesPlan.Add(coachingPlane);
            context.SaveChanges();
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

            var coach = await context.Coaches.Include(x=>x.Coachplans).FirstOrDefaultAsync(x=>x.PhoneNumber==phoneNumber);
            if(coach is null) return new ApiResponse() { Message = "User is not a coach", Action = false };// Ensure the user is a coach
            var coachingPlane = coach.Coachplans.FirstOrDefault(x => x.Id == id);
            if (coachingPlane is null) return new ApiResponse() { Message = "Coaching plane not found", Action = false };

            var payments = context.Payments.Include(c=>c.CoachPlan).Where(c => c.Id == id).ToList();
            if(payments.Count != 0)
            {
                coachingPlane.IsDeleted = true;
                var newCoachPlan = addCoachingPlaneDto.ToCoachPlane(coach);
                coach.Coachplans ??= [];
                coach.Coachplans.Add(newCoachPlan);
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
            var coach = await context.Coaches.Include(x => x.Coachplans).FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (coach is null) return new ApiResponse() { Message = "User is not a coach", Action = false };// Ensure the user is a coach
            var coachingPlane = coach.Coachplans.FirstOrDefault(x => x.Id == id);
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
            var coach = await context.Coaches.Include(x => x.User).Include(x => x.Payments).FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (coach is null) return new ApiResponse() { Message = "User is not a coach", Action = false };// Ensure the user is a coach
            if(coach.Payments is { Count: 0 }) return new ApiResponse() { Message = "No payments found", Action = false };
            
            var payments = coach.Payments.ToList();
            return new ApiResponse()
            {
                Message = "Payments found",
                Action = true,
                Result = payments.Where(p=> p.PaymentStatus == PaymentStatus.success).Select(x=>x.ToPaymentResponseDto())
            };
        }
    }
}
