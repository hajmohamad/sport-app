using Microsoft.AspNetCore.Mvc;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Support;
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
        [HttpPut("ActiveShowWebsiteCoach/{coachPhoneNumber}")]
        public async Task<IActionResult> ShowWebsite_coach([FromRoute] string coachPhoneNumber)
        {
            var result = await adminRepository.ActiveShowWebsiteCoach(coachPhoneNumber);
            if (result.Action == false) return BadRequest(result);
            return Ok(result);
        }
      
        [HttpGet("GetVerifiedCoaches")]
        public async Task<IActionResult> GetVerifiedCoaches()
        {
            var result = await adminRepository.GetVerifiedCoaches();
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
            [FromQuery] string? transactionReference,IFormFile? file)
        {
            if (!Enum.TryParse<PayoutStatus>(newStatus, true, out var statusEnum))
            {
                return BadRequest(new { Message = "مقدار وضعیت ارسال شده نامعتبر است." });
            }

            var result = await adminRepository.UpdateCoachPayoutStatus(payoutId, statusEnum, transactionReference,file);
            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        

        [HttpGet("GetCoachService/{coachSlug}")]
        public async Task<IActionResult> GetCoachService([FromRoute]string coachSlug)
        {
            var result = await adminRepository.GetCoachService(coachSlug);
            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        
        //
        // [HttpPost("sendMassageToCoach")]
        // [TypeFilter(typeof(IpAddressFilter))]
        //
        // public async Task<IActionResult> SendMassageToCoach([FromQuery] string phoneNumber,
        //     [FromQuery] string message)
        // {
        //     var result = await adminRepository.SendMassageToCoach(phoneNumber, message);
        //     if (!result.IsSuccess)
        //     {
        //         return BadRequest(result);
        //     }
        //
        //     return Ok(result);
        // } 
          [HttpGet("SupportTickets")]
        public async Task<IActionResult> GetSupportTickets([FromQuery] TicketStatus? status)
        {
            var response = await adminRepository.GetAllSupportTicketsAsync(status);
            if (!response.Action)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }


        [HttpGet("SupportTickets/{ticketId}")]
        public async Task<IActionResult> GetSupportTicketDetails(int ticketId)
        {
            var response = await adminRepository.GetSupportTicketDetailsAsync(ticketId);
            if (!response.Action)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

  
        [HttpPost("SupportTickets/{ticketId}/Reply")]
        public async Task<IActionResult> ReplyToTicket(int ticketId, [FromBody] ReplyTicketDto replyDto)
        {
            if (replyDto == null || string.IsNullOrWhiteSpace(replyDto.MessageText))
            {
                return BadRequest(new ApiResponse { Action = false, Message = "متن پیام نمی‌تواند خالی باشد." });
            }

            var response = await adminRepository.ReplyToSupportTicketAsync(ticketId, replyDto);
            if (!response.Action)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        
        [HttpPost("SupportTickets/{ticketId}/Close")]
        public async Task<IActionResult> CloseTicket(int ticketId)
        {
            var response = await adminRepository.CloseSupportTicketAsync(ticketId);
            if (!response.Action)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
    }
  

       
    
}