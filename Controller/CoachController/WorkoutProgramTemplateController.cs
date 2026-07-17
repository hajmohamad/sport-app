using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos.WorkoutProgramTemplateDto;
using sport_app_backend.Interface.Coach;
using sport_app_backend.Models;

namespace sport_app_backend.Controller.CoachController;

[Authorize(Roles = "Coach")]
[Route("api/coach/program-templates")]
[ApiController]
public class WorkoutProgramTemplateController(IWorkoutProgramTemplateRepository repository, ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPost("CreateTemplate")]
    public async Task<IActionResult> Create([FromBody] WorkoutProgramTemplateCreateDto dto)
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0)
        {
              
            return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
                
        }

        var result = await repository.CreateTemplate(coachId, dto);
        return Ok(result);
    }

    [HttpPut("UpdateTemplate/{templateId}")]
    public async Task<IActionResult> Update(int templateId, [FromBody] WorkoutProgramTemplateUpdateDto dto)
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0)
        {
              
            return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
                
        }

        var result = await repository.UpdateTemplate(coachId, templateId, dto);
        return Ok(result);
    }

    [HttpDelete("DeleteTemplate/{templateId}")]
    public async Task<IActionResult> Delete(int templateId)
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0)
        {
              
            return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
                
        }

        var result = await repository.DeleteTemplate(coachId, templateId);
        return Ok(result);
    }

    [HttpGet("GetTemplateById/{templateId}")]
    public async Task<IActionResult> GetById(int templateId)
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0)
        {
              
            return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
                
        }

        var result = await repository.GetTemplateById(coachId, templateId);
        return Ok(result);
    }

    [HttpGet("GetAllTemplates")]
    public async Task<IActionResult> GetAll()
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0)
        {
              
            return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
                
        }

        var result = await repository.GetAllTemplates(coachId);
        return Ok(result);
    }

    [HttpPost("ApplyTemplateToProgram")]
    public async Task<IActionResult> Apply([FromBody] CreateWorkoutProgramFromTemplateDto dto)
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0)
        {
              
            return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
                
        }
        var result = await repository.ApplyTemplateToProgram(coachId, dto.TemplateId, dto.PaymentId);
        return Ok(result);
    }
    private async Task<int> GetCoachIdAsync()
    {
        var coachIdClaim = User.FindFirst("coach_id")?.Value;
        if (int.TryParse(coachIdClaim, out var coachId))
        {
            return coachId;
        }
            
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(phoneNumber))
        {
            return 0;
        }
        var id =  await dbContext.Coaches
            .AsNoTracking()
            .Where(c => c.PhoneNumber == phoneNumber)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
        return id;
      

    }

}
