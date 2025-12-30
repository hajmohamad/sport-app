using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using WebPush;

namespace sport_app_backend.Controller;

[ApiController]
[Route("api/notifications")]
public class NotificationController(INotification notification) : ControllerBase
{
    [HttpPost("subscribe")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> Subscribe([FromBody] NotificationSubscriptionDto dto)
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");

        var result = await notification.AddNewSubscribe(phoneNumber, dto);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    // [HttpPost("send")]
    // public async Task<IActionResult> SendNotification([FromBody] string messageText)
    // {
    //    
    //     var subscription = new PushSubscription(
    //         "https://fcm.googleapis.com/fcm/send/dOtHPoF9ejY:APA91bG0C01X0ffpydw5sdgHLk9rVBsx5orcuUgGs0GeqfhMP1Xu572kuExofvqJxANrZ4pkPuaniylzTx72ujJ7y8V8oYSRDSoMUTLFGXeebSkbNQuk6OQ0vmeDKyT-D6l3V4gUXEil",
    //         "BNuAyIwS2NcMZtdBNhHsEpg6UsUuXh3geme32lrXl7sPysfCGznqfy33arQip0EtX2T3JY_OQ6oLj91-fJ6Vsco",
    //         "ob0pcaPXO4TMdgUgvydwmQ"
    //     );
    //
    //     var vapidDetails = new VapidDetails("mailto:example@yourdomain.com", VapidPublicKey, VapidPrivateKey);
    //     var webPushClient = new WebPushClient();
    //
    //     try
    //     {
    //         var payload = System.Text.Json.JsonSerializer.Serialize(new {
    //             title = "پیام جدید از سرور",
    //             body = messageText
    //         });
    //
    //         await webPushClient.SendNotificationAsync(subscription, payload, vapidDetails);
    //         return Ok("Notification sent successfully!");
    //     }
    //     catch (WebPushException ex)
    //     {
    //         return BadRequest("Error sending notification: " + ex.Message);
    //     }
    // }

}