using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ZarinPal.Verify;
using sport_app_backend.Interface;
using sport_app_backend.Models;


namespace sport_app_backend.Controller;
[Route("api/[controller]")]
[ApiController]
public class BuyFromSiteController(IBuyFromSiteRepository buyFromSiteRepository):ControllerBase
{
    [HttpPost("CheckCode")]
    public async Task<IActionResult> CheckCode([FromBody] CheckCodeRequestFromBuyFromSiteDto checkCodeRequestDto)
    {
        var result =  await buyFromSiteRepository.CheckCode(checkCodeRequestDto);

        if (!result.Action)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
    

    [HttpPost("SendCode")]
    public async Task<IActionResult>SendCode([FromBody]string userPhoneNumber)
    {
        try
        {
            var result = await buyFromSiteRepository.Login(userPhoneNumber);

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
    [HttpPost("buy_Service/{serviceId:int}")]
    [Authorize(Roles = "Athlete")]
    public async Task<IActionResult> BuyCoachingService([FromRoute] int serviceId)
    {
        var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        var result = await buyFromSiteRepository.BuyCoachingService(phoneNumber, serviceId);
        if (!result.Action) return BadRequest(result);
        return Ok(result);
    }
    [HttpGet("VerifyPayment")]
        
    public async Task<IActionResult> VerifyPayment([FromQuery] string authority, [FromQuery] string status)
    {

        var verifyRequest = new ZarinPalVerifyRequestDto
        {
            Authority = authority,
        };

        var result = await buyFromSiteRepository.VerifyPaymentAsync(verifyRequest,status);

        if (result.Action)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpGet("get_Exercise/{exerciseId}")]
    public async Task<IActionResult> GetExercise([FromRoute] int exerciseId)
    {
        var result = await buyFromSiteRepository.GetExercise(exerciseId);
        if (result.Action != true) return BadRequest(result);
        return Ok(result);
            
    }
    [HttpGet("GetWorkoutProgram")]
        
    public async Task<IActionResult> GetWorkoutProgram([FromQuery] string wPkey)
    {
        var result = await buyFromSiteRepository.GetWorkoutProgram(wPkey);

        if (result.Action)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }
    [HttpGet("workout-plan/{wPkey}")]
    public async Task<IActionResult> GetWorkoutPlanPdf(string wPkey)
    {
      
        var result = await buyFromSiteRepository.CreateWorkoutPdfAsync(wPkey);
        if (result.Action != true) return BadRequest(result);
        return Ok(result.Result);
    }
    [HttpPost("AthleteQuestion")]
    public async Task<IActionResult> AthleteQuestion([FromBody] AthleteQuestionBuyFromSiteDto athleteQuestionBuyFromSiteDto)
    {
        var result =  await buyFromSiteRepository.AthleteQuestion(athleteQuestionBuyFromSiteDto);
    
        if (!result.Action)
        {
            return BadRequest(result);
        }
    
        return Ok(result);
    }
    [HttpPost("uploadImageForAthleteQuestion")]
    public async Task<IActionResult> UploadImageForAthleteQuestion([FromQuery] string wPkey,[FromQuery] int id,[FromQuery]string sideName,IFormFile file)
    {
     
        var result = await buyFromSiteRepository.UploadImageForAthleteQuestion(wPkey,id,sideName,file);
    
        if (!result.Action) return NotFound(result);
        return Ok(result);
    }
    [HttpDelete("RemoveImageForAthleteQuestion")]
    public async Task<IActionResult> RemoveImageForAthleteQuestion([FromQuery] string wPkey,[FromQuery] int id,[FromQuery]string sideName)
    {
   
        
        var result = await buyFromSiteRepository.RemoveImageForAthleteQuestion(wPkey,id,sideName);

        if (!result.Action) return NotFound(result);
        return Ok(result);
    }
    [HttpGet("GetImageForAthleteQuestion")]
    public async Task<IActionResult> GetImageForAthleteQuestion([FromQuery] string wPkey,[FromQuery] int id)
    {
        
        var result = await buyFromSiteRepository.GetImageForAthleteQuestion(wPkey,id);

        if (!result.Action) return NotFound(result);
        return Ok(result);
    }
    [HttpPost("AccessToken")]
    public async Task<IActionResult> AccessToken([FromHeader(Name = "Refresh-Token")] string refreshToken) 
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest("Refresh token is missing from the header.");
        }
        var result = await buyFromSiteRepository.GenerateAccessToken(refreshToken);
        if (!result.Action)
        {
            return Unauthorized(result);
        }
    
        return Ok(result);
    }
    
}