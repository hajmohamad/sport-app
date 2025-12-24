
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Dtos.ZarinPal.Verify;
using sport_app_backend.Interface;
using sport_app_backend.Interface.Athlete;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Program;

namespace sport_app_backend.Controller;
[Route("api/[controller]")]
[ApiController]
public class WaterAndWeightController(IWaterAndWeight athleteRepository, ApplicationDbContext context)
    : ControllerBase
{
    [HttpPut("set_daily_water_goal")]
    [Authorize(Roles = "Athlete")]
    public async Task<IActionResult> AddWaterIntake([FromBody] WaterInTakeDto waterInTakeDto)
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");

        var result = await athleteRepository.AddWaterIntake(phoneNumber, waterInTakeDto);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }


    [HttpGet("get_daily_water_goal")]
    [Authorize(Roles = "Athlete")]
    public async Task<IActionResult> GetWaterIntake()
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var athlete = await context.Athletes.Include(a => a.WaterInTake)
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (athlete?.WaterInTake == null)
        {
            return Ok(new ApiResponse()
            {
                Action = true,
                Message = "Water intake found",
                Result = new WaterInTakeDto()
                {
                    DailyCupOfWater = 0,
                    Reminder = 0
                }
            });
        }

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
            [HttpPost("add_water_drinking")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> AddWaterInDay()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.UpdateWaterInDay(phoneNumber,+1);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }
        [HttpPost("remove_water_drinking")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> RemoveWaterInDay()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.UpdateWaterInDay(phoneNumber,-1);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }
        [HttpGet("GetThisDayWaterDrunk")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetThisDayWaterDrunk()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");

            var athlete = await context.Users
                .Include(u => u.Athlete)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            if (athlete == null || athlete.Athlete == null)
                return NotFound("Athlete not found");

            var today = DateTime.Now.Date;

            var listOfWaterInDay = await context.WaterInDays
                .Where(w => w.AthleteId == athlete.Athlete.Id && w.Date.Date == today)
                .ToListAsync();

            return Ok(new ApiResponse()
            {
                Action = true,
                Message = "Water in day found",
                Result = listOfWaterInDay.Select(w => new WaterInDayDto()
                {
                    NumberOfCupsDrinked = w.NumberOfCupsDrinked,
                    Date = w.Date.ToString("yyyy-MM-dd")
                })
            });
        }
        
        [HttpPut("update_goal_weight")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> UpdateGoalWeight(double  goalWeight)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.UpdateGoalWeight(phoneNumber, goalWeight);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("update_weight")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> UpdateWeight(double weight)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.UpdateWeight(phoneNumber, weight);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("update_height_weight")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> UpdateHeightWeight(double weight, int hight)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.UpdateHeightWeight(phoneNumber, weight, hight);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("GetThisMonthWeightReport")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetLastMonthWeightReport()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.GetLastMonthWeightReport(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }
             
        [HttpGet("GetLast4WeekWaterDrunk")]
        [Authorize(Roles = "Athlete")]

        public async Task<IActionResult> GetLast4WeekWaterDrunk()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");

            var athlete = await context.Users
                .Include(u => u.Athlete)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            if (athlete?.Athlete == null)
                return NotFound("Athlete not found");

            var today = DateTime.Now.Date;
            var diffToSaturday = ((int)today.DayOfWeek + 1) % 7;
            var thisSaturday = today.AddDays(-diffToSaturday);
            var last4WeeksSaturday = thisSaturday.AddDays(-21);

            var listOfWaterInDay = await context.WaterInDays
                .Where(w => w.AthleteId == athlete.Athlete.Id &&
                            w.Date >= last4WeeksSaturday)
                .ToListAsync();

            return Ok(new ApiResponse()
            {
                Action = true,
                Message = "Water in day found",
                Result = listOfWaterInDay.Select(w => new WaterInDayDto()
                {
                    NumberOfCupsDrinked = w.NumberOfCupsDrinked,
                    Date = w.Date.ToString("yyyy-MM-dd")
                })
            });
        }

    
}