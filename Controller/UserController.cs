using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models;

namespace sport_app_backend.Controller;
[Route("api/[controller]")]
[ApiController]
public class UserController(IUserRepository userRepository) : ControllerBase

{
    [HttpPost("CheckCode")]
    public async Task<ApiResponse> CheckCode([FromBody] CheckCodeRequestDto checkCodeRequestDto)
    {
        return await userRepository.CheckCode(checkCodeRequestDto);
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
    [HttpPost("report_app")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> ReportApp([FromBody] ReportAppDto reportAppDto)
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var result = await userRepository.ReportApp(phoneNumber, reportAppDto);
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



}
