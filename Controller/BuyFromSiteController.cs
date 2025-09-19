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
    // [HttpPost("AthleteQuestion")]
    // public async Task<IActionResult> AthleteQuestion([FromBody] AthleteQuestionBuyFromSiteDto athleteQuestionBuyFromSiteDto)
    // {
    //     var result =  await buyFromSiteRepository.AthleteQuestion(athleteQuestionBuyFromSiteDto);
    //
    //     if (!result.Action)
    //     {
    //         return BadRequest(result);
    //     }
    //
    //     return Ok(result);
    // }
    // [HttpPost("uploadImageForAthleteQuestion")]
    // public async Task<IActionResult> UploadImageForAthleteQuestion([FromQuery] string wPkey,[FromQuery] int id,[FromQuery]string sideName,IFormFile file)
    // {
    //  
    //     var result = await buyFromSiteRepository.UploadImageForAthleteQuestion(wPkey,id,sideName,file);
    //
    //     if (!result.Action) return NotFound(result);
    //     return Ok(result);
    // }
    [HttpPost("submit-questionnaire")]
    public async Task<IActionResult> SubmitQuestionnaireWithImages(
        [FromForm] AthleteQuestionBuyFromSiteDto dto, 
        IFormFile? frontImage, 
        IFormFile? backImage, 
        IFormFile? sideImage)
    {
        var response = await buyFromSiteRepository.SubmitAthleteQuestionWithImages(dto, frontImage, backImage, sideImage);

        if (response.Action)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
 

    

}