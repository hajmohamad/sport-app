using Microsoft.AspNetCore.Mvc;
using sport_app_backend.Dtos.Eitaa;
using sport_app_backend.Interface;

namespace sport_app_backend.Controller;

[ApiController]
[Route("api/auth/eitaa")]
public class EitaaAuthController(IEitaaAuthService eitaaAuthService)
    : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] EitaaLoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await eitaaAuthService.LoginAsync(
            request,
            cancellationToken);

        if (!result.Action)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete(
        [FromBody] EitaaCompleteLoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await eitaaAuthService.CompleteLoginAsync(
            request,
            cancellationToken);

        if (!result.Action)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}