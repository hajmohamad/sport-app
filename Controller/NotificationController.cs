using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using WebPush;

namespace sport_app_backend.Controller;

[ApiController]
[Route("api/notifications")]
public class NotificationController(INotificationService notification,ApplicationDbContext db,IWebPushNotificationService webPushNotificationService) : ControllerBase
{
    [HttpPost("subscribe")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> Subscribe([FromBody] NotificationSubscriptionDto dto)
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");

        var result = await notification.AddNewSubscribeAsync(phoneNumber, dto);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] string messageText)
    {
        var users =await db.NotificationSubscriptions.ToListAsync();
        foreach (var user in users)
        {

         
            webPushNotificationService.SendAsync(user,"notification",messageText);
            
            // var vapidDetails = new VapidDetails("mailto:example@yourdomain.com",
            //     "BOEaNF0WMvjqB3QrKDYvHg28v6brpn1PpJuJZZBsYT0OwEy1E2skB4KSl3jFbPdqty0xRWSd1ncqfYbGNX6MEbc",
            //     "YLKGteozQLjg2MeYDitWEq2UAiGhI52_FPmldC9-Sog");
            // var webPushClient = new WebPushClient();
            //
            // try
            // {
            //     var payload = System.Text.Json.JsonSerializer.Serialize(new
            //     {
            //         title = "پیام جدید از سرور",
            //         body = messageText
            //     });
            //
            //     await webPushClient.SendNotificationAsync(subscription, payload, vapidDetails);
            // }
            // catch (WebPushException ex)
            // {
            //     return BadRequest("Error sending notification: " + ex.Message);
            // }
        }
        return Ok("Success");
        
    }

}