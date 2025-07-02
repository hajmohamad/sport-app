using Microsoft.AspNetCore.Mvc;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;

namespace sport_app_backend.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController(IAdminRepository adminRepository) : ControllerBase
    {
        [HttpPost("add_Exercises")]
        public async Task<IActionResult> AddExercises([FromBody] List<AddExercisesRequestDto> exercises)
        {
            return Ok(await adminRepository.AddExercises(exercises));
        }
        [HttpPut("confirm_Transaction_id")]
        public async Task<IActionResult> ConfirmTransactionId([FromBody] string confirmTransactionIdDto)
        {
            var result = await adminRepository.ConfirmTransactionId(confirmTransactionIdDto);
            if(result.Action == false) return BadRequest(result);
            return Ok(result);
        
        }
        [HttpPut("Verified_coach/{coachPhoneNumber}")]
        public async Task<IActionResult> Verified_coach([FromRoute] string coachPhoneNumber)
        {
            var result = await adminRepository.VerifiedCoach(coachPhoneNumber);
            if(result.Action == false) return BadRequest(result);
            return Ok(result);
        
        }
       
    }

}