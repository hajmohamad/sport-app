using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Controller;
[Route("api/[controller]")]
[ApiController]
public class UserController(IUserRepository userRepository) : ControllerBase

{   
    private readonly IUserRepository _userRepository = userRepository;
   


    [HttpPost("CheckCode")]
    public async Task<ApiResponse> CheckCode([FromBody] CheckCodeRequestDto checkCodeRequestDto)
    {
        return await _userRepository.CheckCode(checkCodeRequestDto);
    }

    ///Send code
    [HttpPost("SendCode")]
    public async Task<ApiResponse> SendCode([FromBody]string userPhoneNumber)
    {
        return await _userRepository.Login(userPhoneNumber);
    }

    [HttpPut("add role")]
    [Authorize(Roles = "None")]
    public async Task<IActionResult> AddRoleGender([FromBody] RoleGenderDto roleGenderDto)
    {
        var phoneNumber =  User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        return Ok(await _userRepository.AddRoleGender(phoneNumber, roleGenderDto));
    }

    [HttpPost("AccsessToken")]
    public async Task<IActionResult> AccsessTokenGenerator([FromBody] string refreshToken)
    {
        return Ok(await _userRepository.GenerateAccessToken(refreshToken));
    }
    
    //add username
    [HttpPut("add username")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> AddUsername([FromBody] string username)
    {
        var phoneNumber =  User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var result = await _userRepository.AddUsername(phoneNumber, username);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("edit_user_profile")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> EditUserProfile([FromBody] EditUserProfileDto userProfileDto)
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var result = await _userRepository.EditUserProfile(phoneNumber, userProfileDto);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("get_user_profile_for_edit")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> GetUserProfileForEdit()
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var result = await _userRepository.GetUserProfileForEdit(phoneNumber);
        if (!result.Action) return NotFound(result);
        return Ok(result);
    }
    [HttpPost("report_app")]
    [Authorize(Roles = "Athlete,Coach")]
    public async Task<IActionResult> ReportApp([FromBody] ReportAppDto reportAppDto)
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var result = await _userRepository.ReportApp(phoneNumber, reportAppDto);
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
        var result = await _userRepository.Logout(phoneNumber);
        if (!result.Action) return NotFound(result);
        return Ok(result);
    }
    



}
