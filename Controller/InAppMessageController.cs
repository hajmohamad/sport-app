using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models;

namespace sport_app_backend.Controller;

[ApiController]
[Route("api/messages")]
public class InAppMessageController(IInAppMessageRepository messageRepository) : ControllerBase
{

    [HttpPost("create")]
    public async Task<IActionResult> CreateMessage([FromBody] CreateInAppMessageDto dto)
    {
        var result = await messageRepository.CreateMessage(dto);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    
    [HttpGet("list")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> GetMessages()
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");

        var result = await messageRepository.GetMessages(phoneNumber);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

   
    [HttpGet("{messageId:int}")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> GetMessageDetail([FromRoute] int messageId)
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");

        var result = await messageRepository.GetMessageDetail(phoneNumber, messageId);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{messageId:int}/read")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> MarkAsRead([FromRoute] int messageId)
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");

        var result = await messageRepository.MarkAsRead(phoneNumber, messageId);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

  
    [HttpPost("read-all")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");

        var result = await messageRepository.MarkAllAsRead(phoneNumber);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

   
    [HttpGet("unread-count")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");

        var count = await messageRepository.GetUnreadCount(phoneNumber);
        return Ok(new ApiResponse { Action = true, Message = "تعداد پیام‌های خوانده‌نشده", Result = count });
    }
}