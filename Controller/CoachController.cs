using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Repository;
using System.Security.Claims;

namespace sport_app_backend.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoachController(ICoachRepository coachRepository, ApplicationDbContext dbContext) : ControllerBase
    {
        private readonly ApplicationDbContext _context = dbContext;
        private readonly ICoachRepository _coachRepository = coachRepository;


        [HttpPost("CoachQuastions")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> SubmitCoachQuastions([FromBody] CoachQuestionDto coachQuestionDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");

            var result = await _coachRepository.SubmitCoachQuestions(phoneNumber, coachQuestionDto);
            if (!result.Action) return BadRequest("Failed to submit coach questions.");

            return Ok(new { Message = "Coach questions submitted successfully" });
        }


        [HttpGet("get_coach_profile")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetCoachProfile()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });

            var user = await _context.Users
                .Include(u => u.Coach)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (user == null || user.Coach == null) return NotFound(new ApiResponse { Action = false, Message = "Coach not found" });

            return Ok(new ApiResponse { Action = true, Message = "Coach found", Result = user.ToCoachProfileResponseDto() });
        }

        [HttpPost("add_coaching_plane")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> AddCoachingPlane(AddCoachingPlaneDto coachingPlaneDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await _coachRepository.AddCoachingPlane(phoneNumber, coachingPlaneDto);

            if (result.Action) return Ok(result);
            else return BadRequest(result);
        }

        ///edit coaching plane
        [HttpPut("edit_coaching_plane/{id}")]
        [Authorize(Roles = "Coach")]

        public async Task<IActionResult> EditCoachingPlane([FromRoute] int id, AddCoachingPlaneDto coachingPlaneDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await _coachRepository.UpdateCoachingPlane(phoneNumber, id, coachingPlaneDto);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);
        }
        
        //delete coaching plane
        [HttpDelete("delete_coaching_plane/{id}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> DeleteCoachingPlane([FromRoute] int id)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await _coachRepository.DeleteCoachingPlane(phoneNumber, id);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);
        }

        /// get coaching plane
        [HttpGet("get_coaching_plane")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetCoachingPlane()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var coach = await _context.Coaches.Include(c => c.Coachplans).FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach == null) return NotFound(new ApiResponse { Action = false, Message = "Coach not found" });
            var coachingPlane = coach.Coachplans.Where(x=>x.IsDeleted==false).ToList();
            var coachingPlaneDto = coachingPlane.Select(x => x.ToCoachingPlanResponse()).ToList();
            return Ok(new ApiResponse { Action = true, Message = "Coaching plane found", Result = coachingPlaneDto });

        }
        
        
        [HttpGet("get_all_payment")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetAllPayment()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await _coachRepository.GetAllPayment(phoneNumber);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);
        }


    }
}
