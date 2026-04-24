using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using System.Security.Claims;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Interface.Coach;


namespace sport_app_backend.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoachController(ICoachRepository coachRepository, ApplicationDbContext dbContext) : ControllerBase
    {
        [HttpPost("CoachQuestions")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> SubmitCoachQuestions([FromBody] CoachQuestionDto coachQuestionDto)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) return BadRequest("CoachId is null");

            var result = await coachRepository.SubmitCoachQuestions(coachId.Value, coachQuestionDto);
            if (!result.Action) return BadRequest(result);

            return Ok(result);
        }
        [HttpPost("CreatePayoutRequest")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> CreatePayoutRequest()
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) return BadRequest(new ApiResponse { Action = false, Message = "CoachId is null" });
            var result = await coachRepository.CreatePayoutRequest(coachId.Value);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }


        [HttpGet("get_coach_profile")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetCoachProfile()
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) return BadRequest(new ApiResponse { Action = false, Message = "CoachId is null" });
            var result = await coachRepository.GetProfile(coachId.Value);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("add_coaching_Service")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> AddCoachingService(AddCoachServiceDto coachingServiceDto)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) return BadRequest(new ApiResponse { Action = false, Message = "CoachId is null" });
            var result = await coachRepository.AddCoachingServices(coachId.Value, coachingServiceDto);

            if (result.Action) return Ok(result);
            else return BadRequest(result);
        }

        ///edit coaching Service
        [HttpPut("edit_coaching_Service/{id}")]
        [Authorize(Roles = "Coach")]

        public async Task<IActionResult> EditCoachingService([FromRoute] int id, AddCoachServiceDto coachingServiceDto)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) return BadRequest(new ApiResponse { Action = false, Message = "CoachId is null" });
            var result = await coachRepository.UpdateCoachingService(coachId.Value, id, coachingServiceDto);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);
        }
        
        //delete coaching Service
        [HttpDelete("delete_coaching_Service/{id}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> DeleteCoachingService([FromRoute] int id)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) return BadRequest(new ApiResponse { Action = false, Message = "CoachId is null" });
            var result = await coachRepository.DeleteCoachingService(coachId.Value, id);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);
        }
        [HttpGet("AthleteMonthlyActivityForCoach/{athleteId}/{year}/{month}")]
        [Authorize(Roles = "Coach")]

        public async Task<IActionResult> AthleteMonthlyActivityForCoach([FromRoute] int athleteId,[FromRoute] int year,[FromRoute] int month)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) return BadRequest("CoachId is null");
            var result = await coachRepository.AthleteMonthlyActivityForCoach(athleteId,year,month);
            if (!result.Action) return BadRequest(result);
            return Ok(result);

        }
        [HttpGet("AthleteReportForCoach/{athleteId}")]
        [Authorize(Roles = "Coach")]

        public async Task<IActionResult> AthleteReportForCoach([FromRoute] int athleteId)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) return BadRequest("CoachId is null");
            var result = await coachRepository.AthleteReportForCoach(athleteId);
            if (!result.Action) return BadRequest(result);
            return Ok(result);

        }

        /// get coaching Service
        [HttpGet("get_coaching_Service")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetCoachingService()
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) return BadRequest(new ApiResponse { Action = false, Message = "CoachId is null" });
            var coach = await dbContext.Coaches.Include(c => c.CoachingServices).FirstOrDefaultAsync(c => c.Id == coachId.Value);
            if (coach == null) return NotFound(new ApiResponse { Action = false, Message = "Coach not found" });
            var coachingService = coach.CoachingServices.Where(x=>x.IsDeleted==false).ToList();
            var coachingServiceDto = coachingService.Select(x => x.ToCoachingServiceResponse()).ToList();
            return Ok(new ApiResponse { Action = true, Message = "Coaching Service found", Result = coachingServiceDto });

        }
       

        
        [HttpGet("get_all_payment")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetAllPayment()
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) return BadRequest(new ApiResponse { Action = false, Message = "CoachId is null" });
            var result = await coachRepository.GetAllPayment(coachId.Value);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("get_payment/{paymentId}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetPayment([FromRoute] int paymentId){
            var coachId = await GetCoachIdAsync();
            if (coachId is null) return BadRequest(new ApiResponse { Action = false, Message = "CoachId is null" });
            var result = await coachRepository.GetPayment(coachId.Value, paymentId);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);
            
        }

      

        [HttpPost("Save_workoutProgram/{paymentId}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> SaveWorkoutProgram([FromRoute] int paymentId,[FromBody]WorkoutProgramDto  saveWorkoutProgramDto)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) return BadRequest(new ApiResponse { Action = false, Message = "CoachId is null" });
            var result = await coachRepository.SaveWorkoutProgram(coachId.Value,paymentId, saveWorkoutProgramDto);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);
            
        }
        [HttpGet("dashboard")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetDashboard()
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) 
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

            var result = await coachRepository.GetCoachDashboard(coachId.Value);

            if (!result.Action) 
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("dashboard/monthly-chart/{year}/{month}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetMonthlyChart([FromRoute] int year, [FromRoute] int month)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) 
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

            var result = await coachRepository.GetMonthlyIncomeChart(coachId.Value, year, month);

            if (!result.Action) 
                return BadRequest(result);

            return Ok(result);
        }
        [HttpPut("UpdateSocialMediaLink")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> UpdateSocialMediaLink([FromBody] SocialMediaLinkDto socialMediaLinkDto)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) 
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

            var result = await coachRepository.UpdateSocialMediaLink(coachId.Value,socialMediaLinkDto);

            if (!result.Action) 
                return BadRequest(result);

            return Ok(result);
        }
        [HttpGet("GetSocialMediaLink")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetSocialMediaLink()
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) 
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

            var result = await coachRepository.GetSocialMediaLink(coachId.Value);

            if (!result.Action) 
                return BadRequest(result);

            return Ok(result);
        }
        [HttpGet("GetStudentList")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetStudentList()
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null)
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await coachRepository.GetAthletesWithStatus(coachId.Value);

            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        [HttpGet("GetTransactionList")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetTransactionList()
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null)
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await coachRepository.GetTransactions(coachId.Value);

            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        [HttpGet("GetFaq")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetFaq()
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null) return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

            var result = await coachRepository.GetFaq();

            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        [HttpGet("test")]
        public async Task<IActionResult> test()
        {
         

            var result = await coachRepository.Test();

            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        [HttpGet("getwpkey")]
        public async Task<IActionResult> getwpkey([FromQuery]int workoutProgramId)
        {
         

            var result = await coachRepository.GetWPkey(workoutProgramId);

            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        [HttpGet("getWorkoutProgramFeedBack")]
        public async Task<IActionResult> GetWorkoutProgramFeedBack()
        {
            var coachId = await GetCoachIdAsync();
            if (coachId is null)
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await coachRepository.GetWorkoutProgramFeedBack(coachId.Value);

            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }        
      
        [HttpPut("ChooseWorkoutProgramFeedBack")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> ChoseWorkoutProgramFeedBack([FromBody] List<ChoseWorkoutProgramFeedBackDto> choseWorkoutProgramFeedBackDtOs){
            var coachId = await GetCoachIdAsync();
            if (coachId is null) return BadRequest(new ApiResponse { Action = false, Message = "CoachId is null" });
            var result = await coachRepository.ChoseWorkoutProgramFeedBack(coachId.Value,choseWorkoutProgramFeedBackDtOs);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
            
            
        }
        
        
        
        private async Task<int?> GetCoachIdAsync()
        {
            var coachIdClaim = User.FindFirst("coach_id")?.Value;
            if (int.TryParse(coachIdClaim, out var coachId))
            {
                return coachId;
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            return await dbContext.Coaches
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();
        }
        
    }

  

   
}
