using Microsoft.AspNetCore.Mvc;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models.Payments;
using sport_app_backend.Services;

namespace sport_app_backend.Controller
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController(
        IAdminRepository adminRepository,
        IWebHostEnvironment webHostEnvironment, IConfiguration config) : ControllerBase
    {
        [HttpPut("Verified_coach/{coachPhoneNumber}")]
        public async Task<IActionResult> Verified_coach([FromRoute] string coachPhoneNumber)
        {
            var result = await adminRepository.VerifiedCoach(coachPhoneNumber);
            if (result.Action == false) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("GetAllCoachPayouts")]
        public async Task<IActionResult> GetAllCoachPayouts()
        {
            var result = await adminRepository.GetAllCoachPayouts();
            return Ok(result);
        }

        [HttpPut("UpdateCoachPayoutStatus/{payoutId}")]
        public async Task<IActionResult> UpdateCoachPayoutStatus(int payoutId, [FromQuery] string newStatus,
            [FromQuery] string? transactionReference)
        {
            if (!Enum.TryParse<PayoutStatus>(newStatus, true, out var statusEnum))
            {
                return BadRequest(new { Message = "مقدار وضعیت ارسال شده نامعتبر است." });
            }

            var result = await adminRepository.UpdateCoachPayoutStatus(payoutId, statusEnum, transactionReference);
            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("GetCoachService/{phoneNumber}")]
        public async Task<IActionResult> GetCoachService(string phoneNumber)
        {
            var result = await adminRepository.GetCoachService(phoneNumber);
            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        
        [HttpGet("supportApp")]
        public async Task<IActionResult> GetSupportApp( )
        {
            var result = await adminRepository.GetSupportApp();
            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        [HttpPost("AnswerSupportApp/{id:int}")]
        public async Task<IActionResult> AnswerSupportApp([FromRoute] int id )
        {
            var result = await adminRepository.AnswerSupportApp(id);
            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
 
        [HttpPost("sendMassageToCoach")]
        [TypeFilter(typeof(IpAddressFilter))]

        public async Task<IActionResult> SendMassageToCoach([FromQuery] string phoneNumber,
            [FromQuery] string message)
        {
            var result = await adminRepository.SendMassageToCoach(phoneNumber, message);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        
        

       
    }
}