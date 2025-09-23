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
    // [HttpGet("get_AllExercise")]
    // public async Task<IActionResult> GetAllExercise()
    // {
    //     var result = await buyFromSiteRepository.GetAllExercise();
    //     if (result.Action != true) return BadRequest(result);
    //     return Ok(result);
    //         
    // }
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