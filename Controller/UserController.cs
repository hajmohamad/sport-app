using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using sport_app_backend.Models.Actions;
using WebPush;

namespace sport_app_backend.Controller;
[Route("api/[controller]")]
[ApiController]
public class UserController(IUserRepository userRepository) : ControllerBase

{
    [HttpPost("CheckCode")]
    public async Task<IActionResult> CheckCode([FromBody] CheckCodeRequestDto checkCodeRequestDto)
    {
        var result =  await userRepository.CheckCode(checkCodeRequestDto);

        if (!result.Action)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    ///Send code
    [HttpPost("SendCode")]
    public async Task<IActionResult>SendCode([FromBody]string userPhoneNumber)
    {
        try
        {
            var result = await userRepository.Login(userPhoneNumber);

            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception e)
        {

            return BadRequest(new ApiResponse()
            {
                Action = false,
                Message = e.Message
            });
        }
    }

    [HttpPut("addRoleGender")]
    [Authorize(Roles = "None")]
    public async Task<IActionResult> AddRoleGender([FromBody] RoleGenderDto roleGenderDto)
    {
        var phoneNumber =  User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        return Ok(await userRepository.AddRoleGender(phoneNumber, roleGenderDto));
    }

    [HttpPost("AccessToken")]
    public async Task<IActionResult> AccessToken([FromHeader(Name = "Refresh-Token")] string refreshToken) 
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest("Refresh token is missing from the header.");
        }
        var result = await userRepository.GenerateAccessToken(refreshToken);
        if (!result.Action)
        {
            return Unauthorized(result);
        }
    
        return Ok(result);
    }
    
    //add username
    // [HttpPut("add_username")]
    // [Authorize(Roles = "Athlete,Coach")]
    // public async Task<IActionResult> AddUsername([FromBody] string username)
    // {
    //     var phoneNumber =  User.FindFirst(ClaimTypes.Name)?.Value;
    //     if (phoneNumber is null) return BadRequest("PhoneNumber is null");
    //     var result = await userRepository.AddUsername(phoneNumber, username);
    //     if (!result.Action) return BadRequest(result);
    //     return Ok(result);
    // }

    [HttpPut("edit_user_profile")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> EditUserProfile([FromBody] EditUserProfileDto userProfileDto)
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var result = await userRepository.EditUserProfile(phoneNumber, userProfileDto);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("get_user_profile_for_edit")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> GetUserProfileForEdit()
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var result = await userRepository.GetUserProfileForEdit(phoneNumber);
        if (!result.Action) return NotFound(result);
        return Ok(result);
    }
    [HttpPost("AppSupport")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> AppSupport([FromBody] ReportAppDto reportAppDto)
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var result = await userRepository.AppSupport(phoneNumber, reportAppDto);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }
    
    //logout
    [HttpDelete("logout")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> Logout()
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var result = await userRepository.Logout(phoneNumber);
        if (!result.Action) return NotFound(result);
        return Ok(result);
    }
    
    [HttpPost("uploadProfilePhoto")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> uploadProfilePhoto(IFormFile file)
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        
        var result = await userRepository.SaveImageAsync(phoneNumber,file);

        if (!result.Action) return NotFound(result);
        return Ok(result);
    }
    
    [HttpGet("get_AllExercise")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> GetAllExercise()
    {
        var result = await userRepository.GetAllExercise();
        if (result.Action != true) return BadRequest(result);
        return Ok(result);
            
    }
    [HttpGet("get_Exercise/{exerciseId}")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> GetExercise([FromRoute] int exerciseId)
    {
        var result = await userRepository.GetExercise(exerciseId);
        if (result.Action != true) return BadRequest(result);
        return Ok(result);
            
    }
    
    [HttpDelete("removeProfilePhoto")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> RemoveProfilePhoto()
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var result = await userRepository.RemoveProfilePhoto(phoneNumber);
        if (result.Action != true) return BadRequest(result);
        return Ok(result);
            
    }
    private const string VapidPublicKey = "BFtaOg7TbbrtSgj87M8UIRyYoeZQP3JFoTuM84lR3VjAi3P4PsR5cuvQw8zgTPww5K71eklziLb0mjH-9gL_1R8";
    private const string VapidPrivateKey = "BpndCrC7Y-Dn-Knh0by2FZ029uplKpco4RS4_tVhRVM";

    [HttpPost("subscribe")]
    public IActionResult Subscribe([FromBody] NotificationSubscription sub)
    {
      Console.WriteLine(sub.Endpoint);
      Console.WriteLine(sub.Keys.Auth);
      Console.WriteLine(sub.Keys.P256dh);


        return Ok(new { message = "Subscription saved successfully." });
    }

    // ۲. متد ارسال نوتیفیکیشن (از سمت ادمین یا رویدادهای سیستم)
    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] string messageText)
    {
       
        var subscription = new PushSubscription(
           "https://fcm.googleapis.com/fcm/send/dOtHPoF9ejY:APA91bG0C01X0ffpydw5sdgHLk9rVBsx5orcuUgGs0GeqfhMP1Xu572kuExofvqJxANrZ4pkPuaniylzTx72ujJ7y8V8oYSRDSoMUTLFGXeebSkbNQuk6OQ0vmeDKyT-D6l3V4gUXEil",
           "BNuAyIwS2NcMZtdBNhHsEpg6UsUuXh3geme32lrXl7sPysfCGznqfy33arQip0EtX2T3JY_OQ6oLj91-fJ6Vsco",
           "ob0pcaPXO4TMdgUgvydwmQ"
        );

        var vapidDetails = new VapidDetails("mailto:example@yourdomain.com", VapidPublicKey, VapidPrivateKey);
        var webPushClient = new WebPushClient();

        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new {
                title = "پیام جدید از سرور",
                body = messageText
            });

            await webPushClient.SendNotificationAsync(subscription, payload, vapidDetails);
            return Ok("Notification sent successfully!");
        }
        catch (WebPushException ex)
        {
            return BadRequest("Error sending notification: " + ex.Message);
        }
    }

    [HttpGet("AppUpdate")]
    public async Task<IActionResult> AppUpdate()
    {
        var result =  userRepository.UpdateApp();
        return Ok(result);

    }
    [HttpGet("CheckQuestionSubmitted")]
    [Authorize(Roles = "Athlete,Coach")]

    public async Task<IActionResult> CheckQuestionSubmitted()
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var result =  await userRepository.CheckQuestionSubmitted(phoneNumber);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }
    [HttpGet("GetExercisesWithFilter")]
    public async Task<IActionResult> GetExercises(
        [FromQuery] string? level,
        [FromQuery] string? type,
        [FromQuery] string? mechanic,
        [FromQuery] string?[] equipment,
        [FromQuery] string? muscle,
        [FromQuery] string? place,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (exercises, totalCount) = await userRepository.GetExercisesAsync(
            level, type,mechanic, equipment, muscle, place, page, pageSize);

        return Ok(new
        {
            totalCount,
            page,
            pageSize,
            exercises
        });
    }
    public class NotificationSubscription
    {
        public string Endpoint { get; set; }
        public SubscriptionKeys Keys { get; set; } // فیلد کلیدها به عنوان یک شیء مجزا
    }

    public class SubscriptionKeys
    {
        public string P256dh { get; set; }
        public string Auth { get; set; }
    }


}
