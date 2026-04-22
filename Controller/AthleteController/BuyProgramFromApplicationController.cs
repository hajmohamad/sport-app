// using System.Security.Claims;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using sport_app_backend.Data;
// using sport_app_backend.Dtos;
// using sport_app_backend.Dtos.ProgramDto;
// using sport_app_backend.Dtos.ZarinPal.Verify;
// using sport_app_backend.Interface;
// using sport_app_backend.Interface.Athlete;
// using sport_app_backend.Mappers;
// using sport_app_backend.Models;
// using sport_app_backend.Models.Account;
// using sport_app_backend.Models.Program;
//
// namespace sport_app_backend.Controller;
// [Route("api/[controller]")]
// [ApiController]
// public class BuyProgramFromApplicationController(IBuyProgramFromApplications athleteRepository, ApplicationDbContext context)
//     : ControllerBase
// {
//     [HttpPost("add_athlete_question")]
//     [Authorize(Roles = "Athlete")]
//
//     public async Task<IActionResult> AddAthleteQuestion([FromBody] AthleteQuestionDto athleteQuestionDto)
//     {
//         var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
//         if (phoneNumber is null) return BadRequest("PhoneNumber is null");
//
//         var result = await athleteRepository.SubmitAthleteQuestions(phoneNumber, athleteQuestionDto);
//         if (!result.Action) return BadRequest(result.Message);
//         return Ok(result);
//     }
//     [HttpPut("search_Coaches")]
//     [Authorize(Roles = "Athlete")]
//     public async Task<IActionResult> SearchCoaches([FromBody] CoachNameSearchDto coachNameSearchDto)
//     {
//         var resualt = await athleteRepository.SearchCoaches(coachNameSearchDto);
//         if (!resualt.Action) return BadRequest(resualt);
//         return Ok(resualt);
//
//
//     }
//     [HttpPost("buy_Service/{serviceId:int}")]
//     [Authorize(Roles = "Athlete")]
//     public async Task<IActionResult> BuyCoachingService([FromRoute] int serviceId,
//         [FromBody] CheckoutDiscountRequestDto? checkoutDiscountRequestDto)
//     {
//         var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
//         if (phoneNumber is null) return BadRequest("PhoneNumber is null");
//         var result = await athleteRepository.BuyCoachingService(phoneNumber, serviceId, checkoutDiscountRequestDto);
//         if (!result.Action) return BadRequest(result);
//         return Ok(result);
//     }
//
//     [HttpPost("preview-checkout/{serviceId:int}")]
//     [Authorize(Roles = "Athlete")]
//     public async Task<IActionResult> PreviewCheckout([FromRoute] int serviceId,
//         [FromBody] CheckoutDiscountRequestDto? checkoutDiscountRequestDto)
//     {
//         var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
//         if (phoneNumber is null) return BadRequest("PhoneNumber is null");
//         var result = await athleteRepository.PreviewCheckout(phoneNumber, serviceId, checkoutDiscountRequestDto);
//         if (!result.Action) return BadRequest(result);
//         return Ok(result);
//     }
//     [HttpGet("get_lastQuestion")]
//     [Authorize(Roles = "Athlete")]
//     public async Task<IActionResult> GetLastQuestion()
//     {
//         var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
//         if (phoneNumber is null) return BadRequest("PhoneNumber is null");
//         var result = await athleteRepository.GetLastQuestion(phoneNumber);
//         if (!result.Action) return BadRequest(result);
//         return Ok(result);
//     }
//
//     [HttpGet("VerifyPayment")]
//         
//     public async Task<IActionResult> VerifyPayment([FromQuery] string authority, [FromQuery] string status)
//     {
//
//         var verifyRequest = new ZarinPalVerifyRequestDto
//         {
//             Authority = authority,
//         };
//
//         var result = await athleteRepository.VerifyPaymentAsync(verifyRequest,status);
//
//         if (result.Action)
//         {
//             return Ok(result);
//         }
//
//         return BadRequest(result);
//     }
//     
//     [HttpPost("uploadImageForAthleteQuestion")]
//     [Authorize(Roles = "Athlete")]
//     public async Task<IActionResult> UploadImageForAthleteQuestion([FromQuery] int id,[FromQuery]string sideName,IFormFile file)
//     {
//         var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
//         if (phoneNumber is null) return BadRequest("PhoneNumber is null");
//         
//         var result = await athleteRepository.UploadImageForAthleteQuestion(phoneNumber,id,sideName,file);
//
//         if (!result.Action) return NotFound(result);
//         return Ok(result);
//     }
//     [HttpDelete("RemoveImageForAthleteQuestion")]
//     [Authorize(Roles = "Athlete")]
//     public async Task<IActionResult> RemoveImageForAthleteQuestion([FromQuery] int id,[FromQuery]string sideName)
//     {
//         var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
//         if (phoneNumber is null) return BadRequest("PhoneNumber is null");
//         
//         var result = await athleteRepository.RemoveImageForAthleteQuestion(phoneNumber,id,sideName);
//
//         if (!result.Action) return NotFound(result);
//         return Ok(result);
//     }
//     [HttpGet("GetImageForAthleteQuestion")]
//     [Authorize(Roles = "Athlete")]
//     public async Task<IActionResult> GetImageForAthleteQuestion([FromQuery] int id)
//     {
//         var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
//         if (phoneNumber is null) return BadRequest("PhoneNumber is null");
//         
//         var result = await athleteRepository.GetImageForAthleteQuestion(phoneNumber,id);
//
//         if (!result.Action) return NotFound(result);
//         return Ok(result);
//     }
//     [HttpGet("Get-Coaches")]
//         [Authorize(Roles = "Athlete")]
//         public async Task<IActionResult> GetCoaches()
//         { var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
//             switch (phoneNumber)
//             {
//                 case null:
//                     return BadRequest(new ApiResponse() { Action = false, Message = "PhoneNumber is null" });
//                 case "09373531171":
//                     var amirResult  = await context.Users.
//                         Where(c => c.TypeOfUser == TypeOfUser.COACH&&
//                                    !string.IsNullOrEmpty(c.FirstName)).Select(user=> new CoachForSearch
//                         {
//                             Id = user.Coach.Id,
//                             UserName = user.UserName ?? string.Empty,
//                             FirstName = user.FirstName ?? string.Empty,
//                             LastName = user.LastName ?? string.Empty,
//                             ImageProfile = user.ImageProfile,
//                         } ).ToListAsync();
//                     return  Ok(new ApiResponse()
//                     {
//                         Action = true, Message = "Coaches found", Result = amirResult 
//                     });
//                     break;
//             }
//             var result  = await context.Users.
//                 Where(c => c.TypeOfUser == TypeOfUser.COACH&& c.Coach.Verified&&
//                                                          !string.IsNullOrEmpty(c.FirstName)).ToListAsync();
//             return Ok(new ApiResponse()
//             {
//                 Action = true, Message = "Coaches found", Result = result .Select(c => c.ToCoachForSearch()).ToList()
//             });
//         }
//
//         [HttpGet("get_coach_profile/{coachId}")] //need to make it with id
//         [Authorize(Roles = "Athlete")]
//         public async Task<IActionResult> GetCoachProfile([FromRoute] int coachId)
//         {
//             var coach = await context.Coaches.Include(c => c.User).
//                 Include(c => c.CoachingServices)
//                 .FirstOrDefaultAsync(c => c.Id == coachId);
//             if (coach == null) return BadRequest(new ApiResponse() { Action = false, Message = "Coach not found" });
//             var payments = await context.Payments.Include(p => p.Athlete).Include(p => p.WorkoutProgram).Where(p =>
//                 p.CoachId == coach.Id && p.WorkoutProgram != null &&
//                 p.WorkoutProgram.Status != WorkoutProgramStatus.WRITING &&
//                 p.WorkoutProgram.Status != WorkoutProgramStatus.NOTSTARTED).ToListAsync();
//             var numberOfProgram = payments.Count(p => p.WorkoutProgram != null);
//             var numberOfAthlete = payments.Select(x => x.AthleteId).Distinct().Count();
//
//             return Ok(new ApiResponse()
//             {
//                 Action = true, Message = "Coach found",
//                 Result = coach.ToCoachProfileForAthleteDto(numberOfProgram, numberOfAthlete)
//             });
//         }
//
// }
