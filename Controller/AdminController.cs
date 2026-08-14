using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.Admin;
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
        IWebHostEnvironment webHostEnvironment,
        IConfiguration config) : ControllerBase
    {



        [HttpGet("GetVerifiedCoaches")]
        public async Task<IActionResult> GetVerifiedCoaches()
        {
            var result = await adminRepository.GetVerifiedCoaches();
            return Ok(result);
        }

        
        [HttpGet("GetAllCoachPayouts")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllCoachPayouts()
        {
            var result = await adminRepository.GetAllCoachPayouts();
            return Ok(result);
        }

        [HttpPut("UpdateCoachPayoutStatus/{payoutId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCoachPayoutStatus(int payoutId, [FromQuery] string newStatus,
            [FromQuery] string? transactionReference, IFormFile? file)
        {
            if (!Enum.TryParse<PayoutStatus>(newStatus, true, out var statusEnum))
            {
                return BadRequest(new { Message = "مقدار وضعیت ارسال شده نامعتبر است." });
            }

            var result =
                await adminRepository.UpdateCoachPayoutStatus(payoutId, statusEnum, transactionReference, file);
            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }


        [HttpGet("GetCoachService/{coachSlug}")]
        public async Task<IActionResult> GetCoachService([FromRoute] string coachSlug)
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
        [Authorize(Roles = "Admin")]

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
        [Authorize(Roles = "Admin")]

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
        [Authorize(Roles = "Admin")]

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
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> CloseTicket(int ticketId)
        {
            var response = await adminRepository.CloseSupportTicketAsync(ticketId);
            if (!response.Action)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPut("change-coach-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeCoachStatus([FromBody] ChangeCoachStatusDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await adminRepository.ChangeCoachStatusAsync(request);

            if (result.Action)
            {
                return Ok(result);
            }

            return NotFound(result);
        }

        [HttpGet("coaches")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllCoaches()
        {
            try
            {
                var result = await adminRepository.GetAllCoachesAsync();
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { Action = false, Message = ex.Message });
            }
        }
    }




}