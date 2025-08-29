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

    ///Send code
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

}