using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.TrainingPlan;

namespace sport_app_backend.Repository
{
    public class CoachRepository : ICoachRepository
    {
        private readonly ApplicationDbContext _context;
    

        public CoachRepository(ApplicationDbContext context)
        {
            _context = context;
         
        }

        public async Task<ApiResponse> AddCoachingPlane(string phoneNumber, AddCoachingPlaneDto addCoachingPlaneDto)
        {
         
            var coach = await _context.Coaches.Include(c => c.Coachplans).FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach is null) return new ApiResponse() { Message = "User is not a coach", Action = false };// Ensure the user is a coach
            var coachingPlane = addCoachingPlaneDto.ToCoachPlane(coach);
            coach.Coachplans ??= [];
            coach.Coachplans.Add(coachingPlane);
            _context.CoachesPlan.Add(coachingPlane);
            _context.SaveChanges();
            return new ApiResponse()
            {
                Message = "Coaching plane added successfully",
                Action = true
                
            };
        }

        public async Task<ApiResponse> SubmitCoachQuestions(string phoneNumber, CoachQuestionDto coachQuestionDto)
        {  
            var user = await _context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
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
            _context.CoachQuestions.Add(coachQuestion);
            await _context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Coach questions submitted successfully",
                Action = true
            };
        }

        public async Task<ApiResponse> UpdateCoachingPlane(string phoneNumber,int id, AddCoachingPlaneDto addCoachingPlaneDto)
        {

            var coach = await _context.Coaches.Include(x=>x.Coachplans).FirstOrDefaultAsync(x=>x.PhoneNumber==phoneNumber);
            if(coach is null) return new ApiResponse() { Message = "User is not a coach", Action = false };// Ensure the user is a coach
            var coachingPlane = coach.Coachplans.FirstOrDefault(x => x.Id == id);
            if (coachingPlane is null) return new ApiResponse() { Message = "Coaching plane not found", Action = false };

            var payments = _context.Payments.Include(c=>c.CoachPlan).Where(c => c.Id == id).ToList();
            if(payments.Count != 0)
            {
                coachingPlane.IsDeleted = true;
                var newCoachPlan = addCoachingPlaneDto.ToCoachPlane(coach);
                coach.Coachplans ??= [];
                coach.Coachplans.Add(newCoachPlan);
                _context.CoachesPlan.Add(newCoachPlan);
            }else{
                coachingPlane.UpdateCoachingPlane(addCoachingPlaneDto);
                
                
            }
        
            await _context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Coaching plane updated successfully",
                Action = true,
                Result = coachingPlane
            };

        }
    }
}
