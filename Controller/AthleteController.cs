using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Controller
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class AthleteController : ControllerBase
    {
        private readonly IAthleteRepository _athleteRepository;
        private readonly ApplicationDbContext _context;


        public AthleteController(IAthleteRepository athleteRepository, ApplicationDbContext context)
        {
            _athleteRepository = athleteRepository;
            _context = context;
        }
        [HttpPost("add athlete question")]
        [Authorize(Roles = "Athlete")]

        public async Task<IActionResult> AddAthleteQuestion([FromBody] AthleteQuestionDto athleteQuestionDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");

            var result = await _athleteRepository.SubmitAthleteQuestions(phoneNumber, athleteQuestionDto);
            if (!result.Action) return BadRequest(result.Message);
            return Ok();
        }

        [HttpPost("add_water_intake")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> AddWaterIntake([FromBody] WaterInTakeDto waterInTakeDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");

            var result = await _athleteRepository.AddWaterIntake(phoneNumber, waterInTakeDto);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }


        [HttpGet("get_water_intake")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetWaterIntake()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var athlete = await _context.Athletes.Include(a => a.WaterInTake)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (athlete == null || athlete.WaterInTake == null)
            {
                return NotFound("Water intake not found");
            }
            else
            {
                return Ok(new ApiResponse()
                {
                    Action = true,
                    Message = "Water intake found",
                    Result = new WaterInTakeDto()
                    {
                        DailyCupOfWater = athlete.WaterInTake.DailyCupOfWater,
                        Reminder = athlete.WaterInTake.Reminder
                    }
                });
            }
        }



        [HttpPut("update_water_inDay")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> UpdateWaterInDay()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await _athleteRepository.UpdateWaterInDay(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }


        [HttpGet("get_water_in_day")]
        [Authorize(Roles = "Athlete")]
        
        public async Task<IActionResult> GetWaterInDayForLast7Days()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var athlete = await _context.Users
                .Include(u => u.Athlete)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            var today = DateTime.Now.Date;
            var daysSinceSaturday = (int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 6;
            var lastSaturday = today.AddDays(daysSinceSaturday);
            if (athlete == null || athlete.Athlete == null) return NotFound("Athlete not found");
            var ListOfWaterInDay = await _context.WaterInDays.Where(w => w.AthleteId == athlete.Athlete.Id&& w.Date.Date >= lastSaturday.Date)
                .ToListAsync();

            return Ok(new ApiResponse()
            {
                Action = true,
                Message = "Water inday found",
                Result = ListOfWaterInDay.Select(w => new WaterInDayDto()
                {

                    NumberOfCupsDrinked = w.NumberOfCupsDrinked,
                    Date = w.Date.ToString("yyyy-MM-dd") // Format the date as needed
                })
            });

        }

        [HttpGet("get_Athlete_profile")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetAthleteProfile()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse() { Action = false, Message = "PhoneNumber is null" });
            var user = await _context.Users
                .Include(u => u.Athlete).FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (user is null) return BadRequest(new ApiResponse() { Action = false, Message = "User not found" });

            return Ok(new ApiResponse() { Action = true, Message = "User found", Result = user.ToAthleteProfileResponseDto() });

        }

        [HttpGet("search_Coaches")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> SearchCoaches([FromBody] string FullName)
        {
            var resualt = await _context.Users.Where(c => (c.FirstName + " " + c.LastName).Contains(FullName) &&
            c.TypeOfUser == TypeOfUser.COACH).ToListAsync();
            return Ok(new ApiResponse() { Action = true, Message = "Coaches found", Result = resualt.Select(c => c.ToCoachForSearch()).ToList() });


        }
        [HttpGet("Get-Coaches")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetCoaches()
        {
            var resualt = await _context.Users.Where(c => c.TypeOfUser == TypeOfUser.COACH).ToListAsync();
            return Ok(new ApiResponse() { Action = true, Message = "Coaches found", Result = resualt.Select(c => c.ToCoachForSearch()).ToList() });
        }

        [HttpGet("get_coach_profile/{coachId}")]//need to make it with id
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetCoachProfile([FromRoute] int coachId)
        {
            var coach = await _context.Coaches.Include(c => c.Coachplans).FirstOrDefaultAsync(c => c.Id == coachId);
            if (coach == null) return BadRequest(new ApiResponse() { Action = false, Message = "Coach not found" });

            return Ok(new ApiResponse() { Action = true, Message = "Coach found", Result = coach.ToCoachProfileForAthleteDto() });
        }
        [HttpPut("update_weight")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> UpdateWeight(int weight)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await _athleteRepository.UpdateWeight(phoneNumber, weight);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("get_weight_report")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetWeightReport()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await _athleteRepository.WeightReport(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }


        [HttpPost("add_Activity")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> AddActivity([FromBody] AddActivityDto AddActivityDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await _athleteRepository.AddActivity(phoneNumber, AddActivityDto);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("get_activity_report")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetActivityReport()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await _athleteRepository.ActivityReport(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("delete_activity")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> DeleteActivity(int activityId)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await _athleteRepository.DeleteActivity(phoneNumber, activityId);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }
    

        [HttpPost("buy_plan/{planId}")]//need to make it with id
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> BuyCoachingPlan([FromRoute] int planId){
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await _athleteRepository.BuyCoachingPlan(phoneNumber,planId);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }
    }
}