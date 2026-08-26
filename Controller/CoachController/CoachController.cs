using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.Coach;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Interface.Coach;
using sport_app_backend.Mappers;
using sport_app_backend.Models;

namespace sport_app_backend.Controller.CoachController
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
            if (phoneNumber is null)
                return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.CreatePayoutRequest(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("ProfileChecklist")]
        public async Task<IActionResult> GetProfileChecklist()
        {
            var coachId = await GetCoachIdAsync();
            if (coachId == 0)
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await coachRepository.GetCoachChecklist(coachId);

            return Ok(new ApiResponse
            {
                Action = true,
                Message = "Checklist status retrieved",
                Result = result
            });
        }


        [HttpGet("get_coach_profile")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetCoachProfile()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null)
                return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.GetProfile(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("payments")]
        [Authorize(Roles = "Coach")]

        public async Task<IActionResult> GetCoachPayments(
            [FromQuery] string? sortBy = "date",
            [FromQuery] bool sortDesc = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var filter = new PaymentFilterDto
            {
                SortBy = sortBy,
                SortDesc = sortDesc,
                Page = page,
                PageSize = pageSize
            };
            var coachId = await GetCoachIdAsync();
            if (coachId == 0)
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await coachRepository.GetCoachPayments(coachId, filter);

            return Ok(result);
        }


        [HttpPost("add_coaching_Service")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> AddCoachingService(AddCoachServiceDto coachingServiceDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null)
                return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
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
            if (phoneNumber is null)
                return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
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
            if (phoneNumber is null)
                return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.DeleteCoachingService(phoneNumber, id);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("AthleteMonthlyActivityForCoach/{athleteId}/{year}/{month}")]
        [Authorize(Roles = "Coach")]

        public async Task<IActionResult> AthleteMonthlyActivityForCoach([FromRoute] int athleteId, [FromRoute] int year,
            [FromRoute] int month)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await coachRepository.AthleteMonthlyActivityForCoach(athleteId, year, month);
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
            if (phoneNumber is null)
                return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var coach = await dbContext.Coaches.Include(c => c.CoachingServices)
                .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach == null) return NotFound(new ApiResponse { Action = false, Message = "Coach not found" });
            var coachingService = coach.CoachingServices.Where(x => x.IsDeleted == false).ToList();
            var coachingServiceDto = coachingService.Select(x => x.ToCoachingServiceResponse()).ToList();
            return Ok(new ApiResponse
                { Action = true, Message = "Coaching Service found", Result = coachingServiceDto });

        }

        #region discountCode

        [HttpGet("GetAllServicesWithCalculatedDiscount/{percent:int}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetAllServicesWithCalculatedDiscount([FromRoute] int percent)
        {

            var coachId = await GetCoachIdAsync();
            if (coachId == 0)
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await coachRepository.GetAllServicesWithCalculatedDiscount(coachId, percent);
            if (!result.Action) return BadRequest(result);
            return Ok(result);

        }


        #endregion


        [HttpGet("get_all_payment")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetAllPayment()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null)
                return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.GetAllPayment(phoneNumber);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("get_payment/{paymentId}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetPayment([FromRoute] int paymentId)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null)
                return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.GetPayment(phoneNumber, paymentId);
            if (result.Action != true) return BadRequest(result);
            return Ok(result);

        }



        [HttpPost("Save_workoutProgram/{paymentId}")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> SaveWorkoutProgram([FromRoute] int paymentId,
            [FromBody] WorkoutProgramDto saveWorkoutProgramDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null)
                return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result = await coachRepository.SaveWorkoutProgram(phoneNumber, paymentId, saveWorkoutProgramDto);
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

            var result = await coachRepository.UpdateSocialMediaLink(phoneNumber, socialMediaLinkDto);

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

        #region CardNumber

        [HttpPost("AddCardNumber")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> AddCardNumber(
            [FromBody] AddCardNumberDto addCardNumberDto)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId == 0)
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await coachRepository.AddCardNumber(coachId, addCardNumberDto);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("GetCardNumber")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetCardNumber()
        {
            var coachId = await GetCoachIdAsync();
            if (coachId == 0)
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await coachRepository.GetCardNumber(coachId);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }


        #endregion

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
        public async Task<IActionResult> Getwpkey([FromQuery] int workoutProgramId)
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
        public async Task<IActionResult> ChoseWorkoutProgramFeedBack(
            [FromBody] List<ChoseWorkoutProgramFeedBackDto> choseWorkoutProgramFeedBackDtOs)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null)
                return BadRequest(new ApiResponse { Action = false, Message = "PhoneNumber is null" });
            var result =
                await coachRepository.ChoseWorkoutProgramFeedBack(phoneNumber, choseWorkoutProgramFeedBackDtOs);
            if (!result.Action) return BadRequest(result);
            return Ok(result);


        }

        [HttpGet("GetExercisesWithFilterForCoach")]
        [Authorize(Roles = "Coach")]
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
            if (coachId == 0)
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var (exercises, totalCount) = await coachRepository.GetExercisesWithFilterForCoach(
                level, type, mechanic, equipment, muscle, place, page, pageSize, name, athleteId, coachId);

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
        public async Task<IActionResult> AddPineExercise([FromBody] int exerciseId)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId == 0)
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await coachRepository.AddPineExercise(exerciseId, coachId);

            if (result.Action) return Ok(result);
            return BadRequest(result);
        }

        [HttpDelete("RemovePineExercise")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> RemovePineExercise([FromBody] int exerciseId)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId == 0)
            {

                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

            }

            var result = await coachRepository.RemovePineExercise(exerciseId, coachId);

            if (result.Action) return Ok(result);
            return BadRequest(result);
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

            var id = await dbContext.Coaches
                .AsNoTracking()
                .Where(c => c.PhoneNumber == phoneNumber)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();
            return id;


        }

        [Authorize(Roles = "Coach")] // فقط مربی دسترسی داشته باشد
        [HttpPost("buyForAthlete")]
        public async Task<IActionResult> BuyForAthlete([FromBody] CoachBuyRequestDto dto)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId == 0)
            {

                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });

            }

            var result = await coachRepository.BuyServiceByCoachFromWallet(coachId, dto);

            if (result.Action) return Ok(result);
            return BadRequest(result);
        }

        #region Wallet

        [HttpPost("charge")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> RequestCharge([FromBody] double price)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId == 0)
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var res = await coachRepository.RequestWalletChargeAsync(coachId, price);
            return Ok(res);
        }

        [HttpGet("verify")]

        public async Task<IActionResult> Verify([FromQuery] string authority, [FromQuery] string status)
        {
            var res = await coachRepository.VerifyWalletChargeAsync(
                authority,
                status
            );
            return Ok(res);
        }

        [HttpGet("amount")]

        public async Task<IActionResult> GetCoachAmount()
        {
            var coachId = await GetCoachIdAsync();
            if (coachId == 0)
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var res = await coachRepository.GetCoachAmount(
                coachId
            );
            return Ok(res);
        }


        #endregion

        #region WebSiteUrl Management

        [HttpGet("WebSiteUrl")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> GetWebSiteUrlStatus()
        {
            var coachId = await GetCoachIdAsync();
            if (coachId == 0)
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await coachRepository.GetWebSiteUrlStatusAsync(coachId);
            if (!result.Action) return NotFound(result);
            return Ok(result);
        }

        [HttpPost("WebSiteUrl")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> UpdateWebSiteUrl([FromBody] UpdateWebSiteUrlDto model)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId == 0)
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            if (model == null || string.IsNullOrWhiteSpace(model.WebSiteUrl))
            {
                return BadRequest(new ApiResponse { Action = false, Message = "لطفاً آدرس معتبری وارد کنید." });
            }

            var result = await coachRepository.UpdateWebSiteUrlAsync(coachId, model.WebSiteUrl);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("CheckWebSiteUrl")]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> CheckWebSiteUrl([FromQuery] string url)
        {
            var coachId = await GetCoachIdAsync();
            if (coachId == 0)
            {
                return Unauthorized(new ApiResponse { Action = false, Message = "خطای احراز هویت." });
            }

            var result = await coachRepository.CheckWebSiteUrlAvailabilityAsync(coachId, url);
            if (!result.Action) return BadRequest(result);

            return Ok(result);
        }


        #endregion


    }
}

  

   

