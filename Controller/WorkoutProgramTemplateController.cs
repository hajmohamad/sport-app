using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sport_app_backend.Dtos.WorkoutProgramTemplateDto;
using sport_app_backend.Interface.Coach;

namespace sport_app_backend.Controller;

[Authorize(Roles = "Coach")]
[Route("api/coach/program-templates")]
[ApiController]
public class WorkoutProgramTemplateController(IWorkoutProgramTemplateRepository repository) : ControllerBase
{
    [HttpPost("CreateTemplate")]
    public async Task<IActionResult> Create([FromBody] WorkoutProgramTemplateCreateDto dto)
    {
        var coachIdClaim = User.FindFirst("id")?.Value;
        if (!int.TryParse(coachIdClaim, out var coachId))
            return Unauthorized();

        var result = await repository.CreateTemplate(coachId, dto);
        return Ok(result);
    }

    [HttpPut("UpdateTemplate/{templateId}")]
    public async Task<IActionResult> Update(int templateId, [FromBody] WorkoutProgramTemplateUpdateDto dto)
    {
        var coachIdClaim = User.FindFirst("id")?.Value;
        if (!int.TryParse(coachIdClaim, out var coachId))
            return Unauthorized();

        var result = await repository.UpdateTemplate(coachId, templateId, dto);
        return Ok(result);
    }

    [HttpDelete("DeleteTemplate/{templateId}")]
    public async Task<IActionResult> Delete(int templateId)
    {
        var coachIdClaim = User.FindFirst("id")?.Value;
        if (!int.TryParse(coachIdClaim, out var coachId))
            return Unauthorized();

        var result = await repository.DeleteTemplate(coachId, templateId);
        return Ok(result);
    }

    [HttpGet("GetTemplateById/{templateId}")]
    public async Task<IActionResult> GetById(int templateId)
    {
        var coachIdClaim = User.FindFirst("id")?.Value;
        if (!int.TryParse(coachIdClaim, out var coachId))
            return Unauthorized();

        var result = await repository.GetTemplateById(coachId, templateId);
        return Ok(result);
    }

    [HttpGet("GetAllTemplates")]
    public async Task<IActionResult> GetAll()
    {
        var coachIdClaim = User.FindFirst("id")?.Value;
        if (!int.TryParse(coachIdClaim, out var coachId))
            return Unauthorized();

        var result = await repository.GetAllTemplates(coachId);
        return Ok(result);
    }

    [HttpPost("ApplyTemplateToProgram")]
    public async Task<IActionResult> Apply([FromBody] CreateWorkoutProgramFromTemplateDto dto)
    {
        var coachIdClaim = User.FindFirst("id")?.Value;
        if (!int.TryParse(coachIdClaim, out var coachId))
            return Unauthorized();

        var result = await repository.ApplyTemplateToProgram(coachId, dto.TemplateId, dto.PaymentId);
        return Ok(result);
    }
}
