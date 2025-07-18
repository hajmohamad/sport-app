
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Dtos.ZarinPal.Verify;
using sport_app_backend.Interface;
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
        [HttpPost("add_athlete_question")]
        [Authorize(Roles = "Athlete")]

        public async Task<IActionResult> AddAthleteQuestion([FromBody] AthleteQuestionDto athleteQuestionDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");

            var result = await athleteRepository.SubmitAthleteQuestions(phoneNumber, athleteQuestionDto);
            if (!result.Action) return BadRequest(result.Message);
            return Ok(result);
        }
        [HttpGet("GetActivityPage")]
        [Authorize(Roles = "Athlete")]

        public async Task<IActionResult> GetActivityPage()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");

            var result = await athleteRepository.GetActivityPage(phoneNumber);
            if (!result.Action) return BadRequest(result.Message);
            return Ok(result);
        }

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

        [HttpPut("set_daily_water_goal")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> AddWaterIntake([FromBody] WaterInTakeDto waterInTakeDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");

            var result = await athleteRepository.AddWaterIntake(phoneNumber, waterInTakeDto);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }


        [HttpGet("get_daily_water_goal")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetWaterIntake()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var athlete = await context.Athletes.Include(a => a.WaterInTake)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (athlete?.WaterInTake == null)
            {
                return Ok(new ApiResponse()
                {
                    Action = true,
                    Message = "Water intake found",
                    Result = new WaterInTakeDto()
                    {
                        DailyCupOfWater = 0,
                        Reminder = 0
                    }
                });
            }

            return Ok(new ApiResponse()
            {
                Action = true,
                Message = "Water intake found",
                Result = new WaterInTakeDto()
                {
                    DailyCupOfWater = athlete.WaterInTake.DailyCupOfWater,
                    Reminder = athlete.WaterInTake.Reminder
                }
            });
        }



        [HttpPost("add_water_drinking")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> AddWaterInDay()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.UpdateWaterInDay(phoneNumber,+1);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }
        [HttpPost("remove_water_drinking")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> RemoveWaterInDay()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.UpdateWaterInDay(phoneNumber,-1);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }
        [HttpGet("GetThisDayWaterDrunk")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetThisDayWaterDrunk()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");

            var athlete = await context.Users
                .Include(u => u.Athlete)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            if (athlete == null || athlete.Athlete == null)
                return NotFound("Athlete not found");

            var today = DateTime.Now.Date;

            var listOfWaterInDay = await context.WaterInDays
                .Where(w => w.AthleteId == athlete.Athlete.Id && w.Date.Date == today)
                .ToListAsync();

            return Ok(new ApiResponse()
            {
                Action = true,
                Message = "Water in day found",
                Result = listOfWaterInDay.Select(w => new WaterInDayDto()
                {
                    NumberOfCupsDrinked = w.NumberOfCupsDrinked,
                    Date = w.Date.ToString("yyyy-MM-dd")
                })
            });
        }
        
        [HttpGet("GetMonthlyActivity/{year}/{month}")]
        [Authorize(Roles = "Athlete")]

        public async Task<IActionResult> GetMonthlyActivity([FromRoute] int year,[FromRoute] int month)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.GetMonthlyActivity(phoneNumber,year,month);
            if (!result.Action) return BadRequest(result);
            return Ok(result);

        }
        [HttpGet("GetLast4WeekWaterDrunk")]
        [Authorize(Roles = "Athlete")]

        public async Task<IActionResult> GetLast4WeekWaterDrunk()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");

            var athlete = await context.Users
                .Include(u => u.Athlete)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            if (athlete?.Athlete == null)
                return NotFound("Athlete not found");

            var today = DateTime.Now.Date;
            var diffToSaturday = ((int)today.DayOfWeek + 1) % 7;
            var thisSaturday = today.AddDays(-diffToSaturday);
            var last4WeeksSaturday = thisSaturday.AddDays(-21);

            var listOfWaterInDay = await context.WaterInDays
                .Where(w => w.AthleteId == athlete.Athlete.Id &&
                            w.Date >= last4WeeksSaturday)
                .ToListAsync();

            return Ok(new ApiResponse()
            {
                Action = true,
                Message = "Water in day found",
                Result = listOfWaterInDay.Select(w => new WaterInDayDto()
                {
                    NumberOfCupsDrinked = w.NumberOfCupsDrinked,
                    Date = w.Date.ToString("yyyy-MM-dd")
                })
            });
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

        [HttpPut("search_Coaches")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> SearchCoaches([FromBody] CoachNameSearchDto coachNameSearchDto)
        {
            var resualt = await athleteRepository.SearchCoaches(coachNameSearchDto);
            if (!resualt.Action) return BadRequest(resualt);
            return Ok(resualt);


        }

        [HttpGet("Get-Coaches")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetCoaches()
        {
            var result  = await context.Users.
                Where(c => c.TypeOfUser == TypeOfUser.COACH&& c.Coach.Verified&&
                                                         !string.IsNullOrEmpty(c.FirstName)).ToListAsync();
            return Ok(new ApiResponse()
            {
                Action = true, Message = "Coaches found", Result = result .Select(c => c.ToCoachForSearch()).ToList()
            });
        }

        [HttpGet("get_coach_profile/{coachId}")] //need to make it with id
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetCoachProfile([FromRoute] int coachId)
        {
            var coach = await context.Coaches.Include(c => c.User).
                Include(c => c.CoachingServices)
                .FirstOrDefaultAsync(c => c.Id == coachId);
            if (coach == null) return BadRequest(new ApiResponse() { Action = false, Message = "Coach not found" });
            var payments = await context.Payments.Include(p => p.Athlete).Include(p => p.WorkoutProgram).Where(p =>
                p.CoachId == coach.Id && p.WorkoutProgram != null &&
                p.WorkoutProgram.Status != WorkoutProgramStatus.WRITING).ToListAsync();
            var numberOfProgram = payments.Count(p => p.WorkoutProgram != null);
            var numberOfAthlete = payments.Select(x => x.AthleteId).Distinct().Count();

            return Ok(new ApiResponse()
            {
                Action = true, Message = "Coach found",
                Result = coach.ToCoachProfileForAthleteDto(numberOfProgram, numberOfAthlete)
            });
        }

        [HttpPut("update_goal_weight")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> UpdateGoalWeight(double  goalWeight)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.UpdateGoalWeight(phoneNumber, goalWeight);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("update_weight")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> UpdateWeight(double weight)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.UpdateWeight(phoneNumber, weight);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("update_hight_weight")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> UpdateHightWeight(double weight, int hight)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.UpdateHightWeight(phoneNumber, weight, hight);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("GetThisMonthWeightReport")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetLastMonthWeightReport()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.GetLastMonthWeightReport(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }


        [HttpPost("add_Activity")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> AddActivity([FromBody] AddActivityDto addActivityDto)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.AddActivity(phoneNumber, addActivityDto);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("getLastWeekActivity")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetLastWeekActivity()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.GetLastWeekActivity(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }


        [HttpGet("get_today_activity_report")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetTodayActivityReport()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.TodayActivityReport(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("delete_activity")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> DeleteActivity(int activityId)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.DeleteActivity(phoneNumber, activityId);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }


        [HttpPost("buy_Service/{serviceId:int}")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> BuyCoachingService([FromRoute] int serviceId)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.BuyCoachingService(phoneNumber, serviceId);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
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

        [HttpGet("get_lastQuestion")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetLastQuestion()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.GetLastQuestion(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("complete new challenge")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> CompleteNewChallenge([FromBody] string challenge)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.CompleteNewChallenge(phoneNumber, challenge);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("completed_challenge")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> CompletedChallenge()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.CompletedChallenge(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);

        }

        [HttpGet("get_achievements")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> GetAchievements()
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.GetAchievements(phoneNumber);
            if (!result.Action) return BadRequest(result);
            return Ok(result);
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

        // [HttpGet("get_AllProgram")]
        // [Authorize(Roles = "Athlete")]
        // public async Task<IActionResult> GetAllPrograms()
        // {
        //     var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        //     if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        //     var result = await athleteRepository.GetAllPrograms(phoneNumber);
        //     if (!result.Action) return BadRequest(result);
        //     return Ok(result);
        // }
        // [HttpGet("getProgram/{programId}")]
        // [Authorize(Roles = "Athlete")]
        // public async Task<IActionResult> GetProgram([FromRoute] int programId)
        // {
        //     var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
        //     if (phoneNumber is null) return BadRequest("PhoneNumber is null");
        //     var result = await athleteRepository.GetProgram(phoneNumber,programId);
        //     if (!result.Action) return BadRequest(result);
        //     return Ok(result);
        // }
        [HttpGet("ActiveProgram/{programId}")]
        [Authorize(Roles = "Athlete")]
        public async Task<IActionResult> ActiveProgram([FromRoute] int programId)
        {
            var phoneNumber = User.FindFirst(ClaimTypes.Name)?.Value;
            if (phoneNumber is null) return BadRequest("PhoneNumber is null");
            var result = await athleteRepository.ActiveProgram(phoneNumber, programId);
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
        [HttpGet("VerifyPayment")]
        
        public async Task<IActionResult> VerifyPayment([FromQuery] string authority, [FromQuery] string status)
        {
            if (!status.Equals("ok", StringComparison.CurrentCultureIgnoreCase))
                return BadRequest("!!پرداخت توسط کاربر لغو شد.");

            var verifyRequest = new ZarinPalVerifyRequestDto
            {
                Authority = authority,
            };

            var result = await athleteRepository.VerifyPaymentAsync(verifyRequest);

            if (result.Action)
            {
                return Ok(result);
            }

            return BadRequest(result);
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