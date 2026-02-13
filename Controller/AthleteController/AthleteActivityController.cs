
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
public class AthleteActivityController(IActivity activityRepository, ApplicationDbContext context)
        : ControllerBase
{
         [HttpGet("GetActivityPage")]
        [Authorize(Roles = "Athlete")]
         public async Task<IActionResult> GetActivityPage()
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");

        var result = await activityRepository.GetActivityPage(phoneNumber);
        if (!result.Action) return BadRequest(result.Message);
        return Ok(result);
    }
    

        [HttpPost("add_Activity")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> AddActivity([FromBody] AddActivityDto addActivityDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await activityRepository.AddActivity(phoneNumber, addActivityDto);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("getLastWeekActivity")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetLastWeekActivity()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await activityRepository.GetLastWeekActivity(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }


        [HttpGet("get_today_activity_report")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetTodayActivityReport()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await activityRepository.TodayActivityReport(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("delete_activity")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> DeleteActivity(int activityId)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await activityRepository.DeleteActivity(phoneNumber, activityId);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }
        [HttpGet("GetMonthlyActivity/{year}/{month}")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetMonthlyActivity([FromRoute] int year,[FromRoute] int month)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await activityRepository.GetMonthlyActivity(phoneNumber,year,month);
            if (!result.Action) return BadRequest(result);
            return Ok(result);

        }


}