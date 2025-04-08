using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class AdminRepository : IAdminRepository
    {
        public readonly ApplicationDbContext _context;

        public AdminRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<ApiResponse> AddExercises(List<AddExercisesRequestDto> exercises)
        {
            var exercisesList = exercises.Select(x => x.ToExercises()).ToList();
            _context.Exercises.AddRange(exercisesList);
            _context.SaveChanges();
            return Task.FromResult(new ApiResponse()
            {
                Message = "Exercises added successfully",
                Action = true
            });

      
        }

        public async Task<ApiResponse> ConfirmTransitionId(string transitionId)
        {
            var payment = await _context.Payments.Include(p => p.Coach)
                .Include(p => p.Athlete).Include(payment => payment.WorkoutProgram).FirstOrDefaultAsync(x => x.TransitionId == transitionId);
            if (payment is null) return new ApiResponse() { Message = "Payment not found", Action = false };
            var workoutProgram = new WorkoutProgram()
            {
                Coach = payment.Coach,
                Athlete = payment.Athlete,
                AthleteId = payment.Athlete!.Id,
                CoachId = payment.Coach!.Id,
                Payment = payment,
                PaymentId = payment.Id

            };
            payment.WorkoutProgram = workoutProgram;
            await _context.WorkoutPrograms.AddAsync(workoutProgram);
            payment.PaymentStatus = PaymentStatus.SUCCESS;
            await _context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Payment confirmed successfully",
                Action = true
            };

          
        }
    }
}