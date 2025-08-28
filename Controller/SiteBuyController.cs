using Microsoft.AspNetCore.Mvc;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models;


namespace sport_app_backend.Controller;
[Route("api/[controller]")]
[ApiController]
public class SiteBuyController(ISiteBuyRepository siteBuyRepository):ControllerBase
{
    [HttpPost("CheckCode")]
    public async Task<IActionResult> CheckCode([FromBody] CheckCodeRequestDto checkCodeRequestDto)
    {
        var result =  await siteBuyRepository.CheckCode(checkCodeRequestDto);

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
            var result = await siteBuyRepository.Login(userPhoneNumber);

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

}