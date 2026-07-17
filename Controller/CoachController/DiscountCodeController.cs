using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.Coach;
using sport_app_backend.Interface.Coach;
using sport_app_backend.Models;

namespace sport_app_backend.Controller.CoachController;

[Authorize(Roles = "Coach")]
[Route("api/Coach/discount-codes")] 
[ApiController]
public class DiscountCodeController(IDiscountCodeRepository discountCodeRepository, ApplicationDbContext dbContext) : ControllerBase
{
    // POST api/Coach/discount-codes/add-discount
    [HttpPost("add-discount")]
    public async Task<IActionResult> CreateDiscountCode([FromBody] DiscountCodeCreateDto discountCodeCreateDto)
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0) return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

        var result = await discountCodeRepository.CreateDiscountCode(coachId, discountCodeCreateDto);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    // PUT api/Coach/discount-codes/{discountCodeId}
    [HttpPut("{discountCodeId:int}")]
    public async Task<IActionResult> UpdateDiscountCode([FromRoute] int discountCodeId, [FromBody] DiscountCodeUpdateDto discountCodeUpdateDto)
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0) return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

        var result = await discountCodeRepository.UpdateDiscountCode(coachId, discountCodeId, discountCodeUpdateDto);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    // GET api/Coach/discount-codes
    [HttpGet] // اصلاح شد: وقتی رشته خالی باشد، از Route اصلی کنترلر استفاده می‌کند
    public async Task<IActionResult> GetDiscountCodes()
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0) return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

        var result = await discountCodeRepository.GetDiscountCodes(coachId);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    // GET api/Coach/discount-codes/{discountCodeId}
    [HttpGet("{discountCodeId:int}")]
    public async Task<IActionResult> GetDiscountCode([FromRoute] int discountCodeId)
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0) return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

        var result = await discountCodeRepository.GetDiscountCodeById(coachId, discountCodeId);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    // PUT api/Coach/discount-codes/{discountCodeId}/changeStatus
    [HttpPut("{discountCodeId:int}/changeStatus")]
    public async Task<IActionResult> ChangeStatus([FromRoute] int discountCodeId, [FromBody] string status)
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0) return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

        var result = await discountCodeRepository.ChangeStatusForDiscountCode(coachId, discountCodeId, status);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    private async Task<int> GetCoachIdAsync()
    {
        var coachIdClaim = User.FindFirst("coach_id")?.Value;
        if (int.TryParse(coachIdClaim, out var coachId)) return coachId;

        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(phoneNumber)) return 0;

        return await dbContext.Coaches
            .AsNoTracking()
            .Where(c => c.PhoneNumber == phoneNumber)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
    }
}
