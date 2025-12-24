
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Dtos.ZarinPal.Verify;
using sport_app_backend.Interface;
using sport_app_backend.Interface.Athlete;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Program;


namespace sport_app_backend.Controller
{

    [Route("api/[controller]")]
    [ApiController]
    public class AthleteController(IAthleteRepository athleteRepository, ApplicationDbContext context)
        : ControllerBase
    {
       

        [HttpPost("Add_FirstQuestions")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> AddFirstQuestions([FromBody] AthleteFirstQuestionsDto athleteFirstQuestions)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.AthleteFirstQuestions(phoneNumber, athleteFirstQuestions);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }
   

        [HttpGet("get_Athlete_profile")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetAthleteProfile()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null)
                return BadRequest(new ApiResponse() { Action = false, Message = "PhoneNumber is null" });
            var user = await context.Users
                .Include(u => u.Athlete).ThenInclude(w=>w.WaterInTake).FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (user is null) return BadRequest(new ApiResponse() { Action = false, Message = "User not found" });
      
            return Ok(new ApiResponse()
                { Action = true, Message = "User found", Result = user.ToAthleteProfileResponseDto() });

        }
        




        [HttpPut("Update_TimeBeforeWorkout/{timeBeforeWorkoutDto:int}")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> UpdateTimeBeforeWorkout([FromRoute] int timeBeforeWorkoutDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var athlete = await context.Athletes.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (athlete is null) return BadRequest("User not found");
            athlete.TimeBeforeWorkout = timeBeforeWorkoutDto;
            await context.SaveChangesAsync();
            return Ok(new ApiResponse()
            {

                Action = true,
                Message = "TimeBeforeWorkout updated"
            });

        }

        [HttpPut("Update_RestTime/{restTimeDto:int}")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> UpdateRestTime([FromRoute] int restTimeDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var athlete = await context.Athletes.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (athlete is null) return BadRequest("User not found");
            athlete.RestTime = restTimeDto;
            await context.SaveChangesAsync();
            return Ok(new ApiResponse()
            {

                Action = true,
                Message = "RestTime updated"
            });

        }

   
        [HttpGet("get_AllPayments")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetAllPayments()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.GetAllPayments(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("getPayment/{paymentId}")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetPayment([FromRoute] int paymentId)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.GetPayment(phoneNumber, paymentId);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }
        [HttpPut("ActiveProgram/{paymentId}")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> ActiveProgram([FromRoute] int paymentId)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.ActiveProgram(phoneNumber, paymentId);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("feedback_exercise")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> FeedbackExercise([FromBody] ExerciseFeedbackDto feedbackExerciseDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.ExerciseFeedBack(phoneNumber, feedbackExerciseDto);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }


        [HttpPost("change_exercise_request")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> ChangeExerciseRequest([FromBody] ExerciseChangeDto exerciseChangeDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.ChangeExercise(phoneNumber, exerciseChangeDto);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("Get_AllTrainingSession")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetAllTrainingSession()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.GetAllTrainingSession(phoneNumber);
            if (!result.Action) return BadRequest(result);
            
            return Ok(result);
        }

        [HttpGet("TrainingSession/{trainingSessionId}")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetTrainingSession([FromRoute] int trainingSessionId)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.GetTrainingSession(phoneNumber, trainingSessionId);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("DoTrainingSession/{trainingSessionId}/{exerciseNumber}")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> DoTrainingSession([FromRoute] int trainingSessionId,
            [FromRoute] int exerciseNumber)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.DoTrainingSession(phoneNumber, trainingSessionId, exerciseNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("FinishTrainingSession")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> FinishTrainingSession(
            [FromBody] FinishTrainingSessionDto finishTrainingSessionDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.FinishTrainingSession(phoneNumber, finishTrainingSessionDto);
            if (!result.Action) return BadRequest(result);
            return Ok(result);

        }


        [HttpPost("FeedbackTrainingSession")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> FeedbackTrainingSession(
            [FromBody] FeedbackTrainingSessionDto feedbackTrainingSessionDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.FeedbackTrainingSession(phoneNumber, feedbackTrainingSessionDto);
            if (!result.Action) return BadRequest(result);
            return Ok(result);

        }
        [HttpGet("CalculateCalories/{trainingSessionId}")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> CalculateCalories([FromRoute] int trainingSessionId)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.CalculateCalories(phoneNumber, trainingSessionId);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }


        [HttpPut("ResetTrainingSession/{trainingSessionId}")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> ResetTrainingSession([FromRoute] int trainingSessionId)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.ResetTrainingSession(phoneNumber, trainingSessionId);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }
         [HttpGet("GetFaq")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetFaq()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await athleteRepository.GetFaq();

            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        
        
    }
}