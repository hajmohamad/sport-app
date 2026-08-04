using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.Chat;
using sport_app_backend.Interface;
using sport_app_backend.Models;

namespace sport_app_backend.Controller;

[Route("api/[controller]")]
[ApiController]
public class ChatController(
    IChatRepository chatRepository,
    ApplicationDbContext dbContext) : ControllerBase
{
    #region Helpers 
    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private async Task<int> GetCoachIdAsync()
    {
        var coachIdClaim = User.FindFirst("coach_id")?.Value;
        if (int.TryParse(coachIdClaim, out var coachId)) return coachId;

        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        return await dbContext.Coaches
            .AsNoTracking()
            .Where(c => c.PhoneNumber == phoneNumber)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<int> GetAthleteIdAsync()
    {
        var athleteClaim = User.FindFirst("athlete_id")?.Value;
        if (int.TryParse(athleteClaim, out var athleteId)) return athleteId;

        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        return await dbContext.Athletes
            .AsNoTracking()
            .Where(c => c.PhoneNumber == phoneNumber)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
    }

    #endregion

    [HttpGet("coach/list")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> GetCoachChats()
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0) return Unauthorized(new ApiResponse { Action = false, Message = "شناسه مربی یافت نشد." });

        var result = await chatRepository.GetCoachChatList(coachId);
        return result.Action ? Ok(result) : BadRequest(result);
    }

    [HttpGet("athlete/list")]
    [Authorize(Roles = "Athlete")]
    public async Task<IActionResult> GetAthleteChats()
    {
        var athleteId = await GetAthleteIdAsync();
        if (athleteId == 0) return Unauthorized(new ApiResponse { Action = false, Message = "شناسه ورزشکار یافت نشد." });

        var result = await chatRepository.GetAthleteChatList(athleteId);
        return result.Action ? Ok(result) : BadRequest(result);
    }

    [HttpPost("messages/send")]
    [Authorize(Roles = "Coach,Athlete")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

        var result = await chatRepository.SendMessage(userId, dto);
        return result.Action ? Ok(result) : BadRequest(result);
    }

    [HttpGet("conversations/{conversationId:long}/messages")]
    [Authorize(Roles = "Coach,Athlete")]
    public async Task<IActionResult> GetMessages(long conversationId, [FromQuery] long? beforeMessageId)
    {
        var userId = GetUserId();
        var result = await chatRepository.GetConversationMessages(userId, conversationId, beforeMessageId,20);
        return result.Action ? Ok(result) : BadRequest(result);
    }

    [HttpPost("conversations/{conversationId:long}/read")]
    [Authorize(Roles = "Coach,Athlete")]
    public async Task<IActionResult> MarkAsRead(long conversationId, [FromBody] long messageId)
    {
        var userId = GetUserId();
        var result = await chatRepository.MarkAsRead(userId, conversationId, messageId);
        return result.Action ? Ok(result) : BadRequest(result);
    }
    [HttpPost("conversations/{conversationId:long}/attachments")]
    public async Task<IActionResult> UploadAttachment(
        [FromRoute] long conversationId,
        IFormFile? file)
    {
        var userId = GetUserId();
        

        if (file is null || file.Length == 0)
        {
            return BadRequest(new ApiResponse
            {
                Action = false,
                Message = "تصویر ارسال نشده است."
            });
        }

        var result = await chatRepository.UploadAttachment(
            userId,
            conversationId,
            file);

        if (!result.Action)
            return BadRequest(result);

        return Ok(result);
    }
}
