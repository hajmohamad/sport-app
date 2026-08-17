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
        if (int.TryParse(coachIdClaim, out var coachId))
        {
            return coachId;
        }

        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(phoneNumber))
        {
            return 0;
        }

        var id = await dbContext.Coaches
            .AsNoTracking()
            .Where(c => c.PhoneNumber == phoneNumber)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
        return id;


    }
    private async Task<int> GetAthleteIdAsync()
    {
        var athleteIdClaim = User.FindFirst("athlete_id")?.Value 
                             ?? User.FindFirst("Athlete_id")?.Value;

        if (int.TryParse(athleteIdClaim, out var athleteId))
        {
            return athleteId;
        }

        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(phoneNumber))
        {
            return 0;
        }

        var id = await dbContext.Athletes
            .AsNoTracking()
            .Where(c => c.PhoneNumber == phoneNumber)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();

        return id;
    }


    #endregion

    
    [HttpGet("coach/list")]
    [Authorize(Roles = "Coach")]
    public async Task<IActionResult> GetCoachChats([FromQuery] string? status = null) // اضافه کردن پارامتر status
    {
        var coachId = await GetCoachIdAsync();
        if (coachId == 0) return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
    
        var userId = GetUserId();
        if (userId == 0) return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

        var result = await chatRepository.GetCoachChatList(coachId, userId, status); 
    
        return result.Action ? Ok(result) : BadRequest(result);
    }

    [HttpGet("athlete/list")]
    [Authorize(Roles = "Athlete")]
    public async Task<IActionResult> GetAthleteChats()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });


        var result = await chatRepository.GetAthleteChatList(userId);
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
        if (userId == 0)
        {
            return Unauthorized(new ApiResponse
            {
                Action = false,
                Message = "خطای احراز هویت."
            });
        }

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
    [Authorize(Roles = "Coach,Athlete")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAttachment(
        [FromRoute] long conversationId,
        IFormFile? file)
    {
        var userId = GetUserId();

        if (userId == 0)
        {
            return Unauthorized(new ApiResponse
            {
                Action = false,
                Message = "خطای احراز هویت."
            });
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new ApiResponse
            {
                Action = false,
                Message = "فایل ارسال نشده است."
            });
        }

        var result = await chatRepository.UploadAttachment(
            userId,
            conversationId,
            file);

        return result.Action ? Ok(result) : BadRequest(result);
    }

    [HttpPost("BackFile")]
    public async Task<IActionResult> BackFile()
    {
        var result = await chatRepository.BackfillCoachAthleteConversationsFromSuccessfulPayments();
    
        return result.Action ? Ok(new
        {
            result
        }) : BadRequest(new
        {
            result
        });
    }
}
