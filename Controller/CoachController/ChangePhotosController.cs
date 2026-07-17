using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface.Coach;
using sport_app_backend.Models;

namespace sport_app_backend.Controller.CoachController;

[Authorize(Roles = "Coach")]
[Route("api/Coach/ChangePhotos")]
[ApiController]
public class ChangePhotosController(IChangePhotosRepository repository, ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllChangePhotos()
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0)
            return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

        var result = await repository.GetAllChangePhotos(coachId);
        if (!result.Action) return NotFound(result);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetChangePhotoById([FromRoute] int id)
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0)
            return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

        var result = await repository.GetChangePhotoById(coachId, id);
        if (!result.Action) return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddChangePhoto([FromForm] AddAthleteChangePhotoDto dto)
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0)
            return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

        var result = await repository.AddChangePhoto(coachId, dto.PhotoUrl, dto);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> EditChangePhoto([FromRoute] int id, [FromForm] EditAthleteChangePhotoDto dto)
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0)
            return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

        dto.Id = id;

        var result = await repository.EditChangePhoto(coachId, dto.PhotoUrl, dto);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteChangePhoto([FromRoute] int id)
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0)
            return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

        var result = await repository.DeleteChangePhoto(coachId, id);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    private async Task<int> GetCoachIdAsync()
    {
        var coachIdClaim = User.FindFirst("coach_id")?.Value;
        if (int.TryParse(coachIdClaim, out var coachId))
            return coachId;

        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(phoneNumber))
            return 0;

        return await dbContext.Coaches
            .AsNoTracking()
            .Where(c => c.PhoneNumber == phoneNumber)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
    }
}
