using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using System.Security.Claims;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Interface.Coach;
using sport_app_backend.Models.Actions.CouchExercise;


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
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");

            var result = await coachRepository.SubmitCoachQuestions(phoneNumber, coachQuestionDto);
            if (!result.Action) return BadRequest(result);

            return Ok(result);
        }
        [HttpPost("CreatePayoutRequest")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> CreatePayoutRequest()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.CreatePayoutRequest(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }


        [HttpGet("get_coach_profile")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetCoachProfile()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.GetProfile(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("add_coaching_Service")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> AddCoachingService(AddCoachServiceDto coachingServiceDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.AddCoachingServices(phoneNumber, coachingServiceDto);

            if (result.Action) return Ok(result);
            else return BadRequest(result);
        }

        ///edit coaching Service
        [HttpPut("edit_coaching_Service/{id}")]
        [Authorize(Roles = "Coach")]

        public async Task<IActionResult> EditCoachingService([FromRoute] int id, AddCoachServiceDto coachingServiceDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.UpdateCoachingService(phoneNumber, id, coachingServiceDto);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);
        }
        
        //delete coaching Service
        [HttpDelete("delete_coaching_Service/{id}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> DeleteCoachingService([FromRoute] int id)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.DeleteCoachingService(phoneNumber, id);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);
        }
        [HttpGet("AthleteMonthlyActivityForCoach/{athleteId}/{year}/{month}")]
        [Authorize(Roles = "Coach")]

        public async Task<IActionResult> AthleteMonthlyActivityForCoach([FromRoute] int athleteId,[FromRoute] int year,[FromRoute] int month)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await coachRepository.AthleteMonthlyActivityForCoach(athleteId,year,month);
            if (!result.Action) return BadRequest(result);
            return Ok(result);

        }
        [HttpGet("AthleteReportForCoach/{athleteId}")]
        [Authorize(Roles = "Coach")]

        public async Task<IActionResult> AthleteReportForCoach([FromRoute] int athleteId)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await coachRepository.AthleteReportForCoach(athleteId);
            if (!result.Action) return BadRequest(result);
            return Ok(result);

        }

        /// get coaching Service
        [HttpGet("get_coaching_Service")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetCoachingService()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var coach = await dbContext.Coaches.Include(c => c.CoachingServices).FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach == null) return NotFound(new ApiResponse { Action = false, Message = "Coach not found" });
            var coachingService = coach.CoachingServices.Where(x=>x.IsDeleted==false).ToList();
            var coachingServiceDto = coachingService.Select(x => x.ToCoachingServiceResponse()).ToList();
            return Ok(new ApiResponse { Action = true, Message = "Coaching Service found", Result = coachingServiceDto });

        }

        [HttpPost("discount-codes/add-discount")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> CreateDiscountCode(
            [FromBody] DiscountCodeCreateDto discountCodeCreateDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.CreateDiscountCode(phoneNumber, discountCodeCreateDto);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("discount-codes/{discountCodeId:int}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> UpdateDiscountCode([FromRoute] int discountCodeId,
            [FromBody] DiscountCodeUpdateDto discountCodeUpdateDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.UpdateDiscountCode(phoneNumber, discountCodeId, discountCodeUpdateDto);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("discount-codes")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetDiscountCodes()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.GetDiscountCodes(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("discount-codes/{discountCodeId:int}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetDiscountCode([FromRoute] int discountCodeId)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.GetDiscountCodeById(phoneNumber, discountCodeId);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("discount-codes/{discountCodeId:int}/disable")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> DisableDiscountCode([FromRoute] int discountCodeId)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.DisableDiscountCode(phoneNumber, discountCodeId);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        

        
        [HttpGet("get_all_payment")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetAllPayment()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.GetAllPayment(phoneNumber);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("get_payment/{paymentId}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetPayment([FromRoute] int paymentId){
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.GetPayment(phoneNumber, paymentId);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);
            
        }

      

        [HttpPost("Save_workoutProgram/{paymentId}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> SaveWorkoutProgram([FromRoute] int paymentId,[FromBody]WorkoutProgramDto  saveWorkoutProgramDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.SaveWorkoutProgram(phoneNumber,paymentId, saveWorkoutProgramDto);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);
            
        }
        [HttpGet("dashboard")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetDashboard()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) 
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

            var result = await coachRepository.GetCoachDashboard(phoneNumber);

            if (!result.Action) 
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("dashboard/monthly-chart/{year}/{month}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetMonthlyChart([FromRoute] int year, [FromRoute] int month)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) 
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

            var result = await coachRepository.GetMonthlyIncomeChart(phoneNumber, year, month);

            if (!result.Action) 
                return BadRequest(result);

            return Ok(result);
        }
        [HttpPut("UpdateSocialMediaLink")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> UpdateSocialMediaLink([FromBody] SocialMediaLinkDto socialMediaLinkDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) 
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

            var result = await coachRepository.UpdateSocialMediaLink(phoneNumber,socialMediaLinkDto);

            if (!result.Action) 
                return BadRequest(result);

            return Ok(result);
        }
        [HttpGet("GetSocialMediaLink")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetSocialMediaLink()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) 
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

            var result = await coachRepository.GetSocialMediaLink(phoneNumber);

            if (!result.Action) 
                return BadRequest(result);

            return Ok(result);
        }
        [HttpGet("GetStudentList")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetStudentList()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await coachRepository.GetAthletesWithStatus(phoneNumber);

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
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await coachRepository.GetTransactions(phoneNumber);

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
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await coachRepository.GetFaq();

            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        
        [HttpGet("getwpkey")]
        public async Task<IActionResult> Getwpkey([FromQuery]int workoutProgramId)
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
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await coachRepository.GetWorkoutProgramFeedBack(phoneNumber);

            if (!result.Action)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }        
      
        [HttpPut("ChooseWorkoutProgramFeedBack")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> ChoseWorkoutProgramFeedBack([FromBody] List<ChoseWorkoutProgramFeedBackDto> choseWorkoutProgramFeedBackDtOs){
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.ChoseWorkoutProgramFeedBack(phoneNumber,choseWorkoutProgramFeedBackDtOs);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
            
            
        }
        [HttpGet("GetExercisesWithFilterForCoach")]
        public async Task<IActionResult> GetExercisesWithFilterForCoach(
            [FromQuery] string? name,
            [FromQuery] string? level,
            [FromQuery] string? type,
            [FromQuery] string? mechanic,
            [FromQuery] string?[] equipment,
            [FromQuery] string? muscle,
            [FromQuery] string? place,
            [FromQuery] int? athleteId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var coachId = await GetCoachIdAsync();
            var (exercises, totalCount) = await coachRepository.GetExercisesWithFilterForCoach(
                level, type,mechanic, equipment, muscle, place, page, pageSize, name,athleteId, coachId);

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                exercises
            });
        }
        [HttpPost("addPineExercise")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> AddPineExercise([FromBody]int exerciseId)
        {
            var coachId = await GetCoachIdAsync();
            var result = await coachRepository.AddPineExercise(exerciseId, coachId);

            if (result.Action) return Ok(result);
            return BadRequest(result);
        }
        [HttpDelete("RemovePineExercise")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> RemovePineExercise([FromBody]int exerciseId)
        {
            var coachId = await GetCoachIdAsync();
            var result = await coachRepository.RemovePineExercise(exerciseId, coachId);

            if (result.Action) return Ok(result);
            return BadRequest(result);
        }
        [HttpPost("test")]
        public async Task<IActionResult> Test()
        {
            
            var oldPrograms = await dbContext.WorkoutPrograms
                .Include(w => w.ProgramInDays)
                .ThenInclude(d => d.AllExerciseInDays)
                .Where(w => !dbContext.LastWorkoutExercises.Any(l => l.WorkoutProgramId == w.Id))
                .ToListAsync();

            var lastWorkouts = (from program in oldPrograms
                let exerciseIds = program.ProgramInDays.SelectMany(d => d.AllExerciseInDays)
                    .Select(ex => ex.Id).ToList()
                select new LastWorkoutExercise
                {
                    CoachId = program.CoachId, // فرض بر اینکه این فیلد در برنامه وجود دارد
                    AthleteId = program.AthleteId, // فرض بر اینکه این فیلد در برنامه وجود دارد
                    WorkoutProgramId = program.Id,
                    ExerciseIds = exerciseIds
                }).ToList();

            if (lastWorkouts.Count != 0)
            {
                await dbContext.LastWorkoutExercises.AddRangeAsync(lastWorkouts);
                await dbContext.SaveChangesAsync();
            }
            return Ok();
        }
       
        

        
        
        private async Task<int> GetCoachIdAsync()
        {
            var coachIdClaim = User.FindFirst("coach_id")?.Value;
            if (int.TryParse(coachIdClaim, out var coachId))
            {
                return coachId;
            }
            
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return 0;
            }
            var id =  await dbContext.Coaches
                .AsNoTracking()
                .Where(c => c.PhoneNumber == phoneNumber)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();
            return id;
            

        }
        
    }

  

   
}
