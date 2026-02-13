
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
public class AchievementController
    (IAchievements achievementsRepository, ApplicationDbContext context)
    : ControllerBase
{
       
    [HttpPost("complete new challenge")]
    [Authorize(Roles = "Athlete")]
    public async Task<IActionResult> CompleteNewChallenge([FromBody] string challenge)
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var result = await achievementsRepository.CompleteNewChallenge(phoneNumber, challenge);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("completed_challenge")]
    [Authorize(Roles = "Athlete")]
    public async Task<IActionResult> CompletedChallenge()
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var result = await achievementsRepository.CompletedChallenge(phoneNumber);
        if (!result.Action) return BadRequest(result);
        return Ok(result);

    }

    [HttpGet("get_achievements")]
    [Authorize(Roles = "Athlete")]
    public async Task<IActionResult> GetAchievements()
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var result = await achievementsRepository.GetAchievements(phoneNumber);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

}