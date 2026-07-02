// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Linq.Expressions;
// using System.Security.Claims;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using Moq;
// using sport_app_backend.Data;
// using sport_app_backend.Dtos;
// using sport_app_backend.Dtos.ProgramDto;
// using sport_app_backend.Interface.Athlete;
// using sport_app_backend.Models;
// using sport_app_backend.Models.Account;
// using sport_app_backend.Models.Account.Athlete;
// using Xunit;
//
// namespace sport_app_backend.Controller
// {
//     public class AthleteControllerTests
//         {
//             private readonly Mock<IAthleteRepository> _mockAthleteRepository;
//             private readonly ApplicationDbContext _context;
//             private readonly AthleteController _controller;
//
//             public AthleteControllerTests()
//             {
//                 _mockAthleteRepository = new Mock<IAthleteRepository>();
//
//                 var options = new DbContextOptionsBuilder<ApplicationDbContext>()
//                     .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
//                     .Options;
//
//                 _context = new ApplicationDbContext(options);
//
//                 _controller = new AthleteController(
//                     _mockAthleteRepository.Object,
//                     _context
//                 );
//
//                 SetupUserClaims();
//             }
//
//             private void SetupUserClaims(string? phoneNumber = "09121234567")
//             {
//                 var claims = new List<Claim>();
//
//                 if (!string.IsNullOrEmpty(phoneNumber))
//                 {
//                     claims.Add(new Claim(ClaimTypes.Name, phoneNumber));
//                 }
//
//                 var identity = new ClaimsIdentity(claims);
//                 var principal = new ClaimsPrincipal(identity);
//
//                 _controller.ControllerContext = new ControllerContext
//                 {
//                     HttpContext = new DefaultHttpContext { User = principal }
//                 };
//             }
//
//
//         #region AddFirstQuestions
//
//         [Fact]
//         public async Task AddFirstQuestions_WithValidData_ReturnsOkResult()
//         {
//             var dto = new AthleteFirstQuestionsDto { FirstName = "John", LastName = "Doe", CurrentWeight = 75, Height = 180 };
//             var response = new ApiResponse { Action = true, Message = "Success" };
//             _mockAthleteRepository
//                 .Setup(r => r.AthleteFirstQuestions("09121234567", dto))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.AddFirstQuestions(dto);
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal(response, okResult.Value);
//         }
//
//         [Fact]
//         public async Task AddFirstQuestions_WithValidData_CallsRepositoryWithCorrectPhoneNumber()
//         {
//             var dto = new AthleteFirstQuestionsDto { FirstName = "John", LastName = "Doe", CurrentWeight = 75, Height = 180 };
//             var response = new ApiResponse { Action = true, Message = "Success" };
//             _mockAthleteRepository
//                 .Setup(r => r.AthleteFirstQuestions("09121234567", dto))
//                 .ReturnsAsync(response);
//
//             await _controller.AddFirstQuestions(dto);
//
//             _mockAthleteRepository.Verify(r => r.AthleteFirstQuestions("09121234567", dto), Times.Once);
//         }
//
//         [Fact]
//         public async Task AddFirstQuestions_WhenRepositoryReturnsFalse_ReturnsBadRequest()
//         {
//             var dto = new AthleteFirstQuestionsDto { FirstName = "John", LastName = "Doe", CurrentWeight = 75, Height = 180 };
//             var response = new ApiResponse { Action = false, Message = "Failed" };
//             _mockAthleteRepository
//                 .Setup(r => r.AthleteFirstQuestions("09121234567", dto))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.AddFirstQuestions(dto);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal(response, badResult.Value);
//         }
//
//         [Fact]
//         public async Task AddFirstQuestions_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims("");
//             var dto = new AthleteFirstQuestionsDto { FirstName = "John", LastName = "Doe", CurrentWeight = 75, Height = 180 };
//
//             var result = await _controller.AddFirstQuestions(dto);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("PhoneNumber is null", badResult.Value);
//         }
//
//         #endregion
//
//         #region GetAthleteProfile
//
//         [Fact]
//         public async Task GetAthleteProfile_WithValidPhoneNumber_ReturnsOkResultWithProfile()
//         {
//             var athlete = new Athlete
//             {
//                 PhoneNumber = "09121234567",
//                 Height = 180,
//                 CurrentWeight = 75,
//                 WeightGoal = 70,
//                 UserId = 1,
//                 User = new User { PhoneNumber = "09121234567", FirstName = "John" ,                 BirthDate = new DateTime(2000, 1, 1), }
//             };
//             athlete.WaterInTake = new WaterInTake { DailyCupOfWater = 8 };
//
//             var user = new User
//             {
//                 Id = 1,
//                 PhoneNumber = "09121234567",
//                 FirstName = "John",
//                 Athlete = athlete,
//                 Gender = Gender.MALE,
//                 BirthDate = new DateTime(2000, 1, 1),
//             };
//             athlete.User = user;
//
//             _context.Users.Add(user);
//             _context.SaveChanges();
//             var result = await _controller.GetAthleteProfile();
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             var response = okResult.Value as ApiResponse;
//             Assert.NotNull(response);
//             Assert.True(response.Action);
//             Assert.Equal("User found", response.Message);
//         }
//
//         [Fact]
//         public async Task GetAthleteProfile_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims("");
//
//             var result = await _controller.GetAthleteProfile();
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             var response = badResult.Value as ApiResponse;
//             Assert.NotNull(response);
//             Assert.False(response.Action);
//             Assert.Equal("PhoneNumber is null", response.Message);
//         }
//
//         [Fact]
//         public async Task GetAthleteProfile_WhenUserNotFound_ReturnsBadRequest()
//         {
//        
//             _context.Users.Add(new User
//             {
//               
//                 PhoneNumber = "",
//                
//             });
//             _context.SaveChanges();
//             var result = await _controller.GetAthleteProfile();
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             var response = badResult.Value as ApiResponse;
//             Assert.NotNull(response);
//             Assert.False(response.Action);
//             Assert.Equal("User not found", response.Message);
//         }
//
//         #endregion
//
//         #region UpdateTimeBeforeWorkout
//
//         [Fact]
//         public async Task UpdateTimeBeforeWorkout_WithValidTimeValue_UpdatesAthleteAndReturnsOk()
//         {
//             var athlete = new Athlete
//             {
//                 PhoneNumber = "09121234567",
//                 TimeBeforeWorkout = 10,
//                 UserId = 1,
//                 User = new User { PhoneNumber = "09121234567" }
//             };
//
//             // ✅ ذخیره واقعی در InMemory DB
//             _context.Athletes.Add(athlete);
//             await _context.SaveChangesAsync();
//
//             var result = await _controller.UpdateTimeBeforeWorkout(20);
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             var response = okResult.Value as ApiResponse;
//
//             Assert.NotNull(response);
//             Assert.True(response.Action);
//             Assert.Equal("TimeBeforeWorkout updated", response.Message);
//
//             // ✅ به‌جای Verify → بررسی تغییر واقعی دیتا
//             var updatedAthlete = await _context.Athletes.FirstAsync();
//             Assert.Equal(20, updatedAthlete.TimeBeforeWorkout);
//         }
//
//         [Fact]
//         public async Task UpdateTimeBeforeWorkout_WhenAthleteNotFound_ReturnsBadRequest()
//         {
//  
//             var result = await _controller.UpdateTimeBeforeWorkout(20);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("User not found", badResult.Value);
//         }
//
//         [Fact]
//         public async Task UpdateTimeBeforeWorkout_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims("");
//
//             var result = await _controller.UpdateTimeBeforeWorkout(20);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("PhoneNumber is null", badResult.Value);
//         }
//         [Fact]
//         public async Task UpdateTimeBeforeWorkout_WithZeroValue_UpdatesSuccessfully()
//         {
//             var athlete = new Athlete
//             {
//                 PhoneNumber = "09121234567",
//                 TimeBeforeWorkout = 10,
//                 UserId = 1,
//                 User = new User { PhoneNumber = "09121234567" }
//             };
//
//             // ✅ ذخیره واقعی در دیتابیس InMemory
//             _context.Athletes.Add(athlete);
//             await _context.SaveChangesAsync();
//
//             var result = await _controller.UpdateTimeBeforeWorkout(0);
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//
//             // ✅ بررسی دیتای واقعی بعد از آپدیت
//             var updatedAthlete = await _context.Athletes.FirstAsync();
//             Assert.Equal(0, updatedAthlete.TimeBeforeWorkout);
//         }
//
//         #endregion
//
//         #region UpdateRestTime
//
//         [Fact]
//         public async Task UpdateRestTime_WithValidTimeValue_UpdatesAthleteAndReturnsOk()
//         {
//             var athlete = new Athlete
//             {
//                 PhoneNumber = "09121234567",
//                 RestTime = 30,
//                 UserId = 1,
//                 User = new User { PhoneNumber = "09121234567" }
//             };
//
//             // Arrange
//             _context.Athletes.Add(athlete);
//             await _context.SaveChangesAsync();
//
//             // Act
//             var result = await _controller.UpdateRestTime(45);
//
//             // Assert
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             var response = okResult.Value as ApiResponse;
//
//             Assert.NotNull(response);
//             Assert.True(response.Action);
//             Assert.Equal("RestTime updated", response.Message);
//
//             var updatedAthlete = await _context.Athletes.FirstAsync();
//             Assert.Equal(45, updatedAthlete.RestTime);
//         }
//         [Fact]
//         public async Task UpdateRestTime_WhenAthleteNotFound_ReturnsBadRequest()
//         {
//             // دیتابیس عمداً خالیه
//
//             var result = await _controller.UpdateRestTime(45);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("User not found", badResult.Value);
//         }
//
//
//         [Fact]
//         public async Task UpdateRestTime_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims("");
//
//             var result = await _controller.UpdateRestTime(45);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("PhoneNumber is null", badResult.Value);
//         }
//
//         [Fact]
//         public async Task UpdateRestTime_WithLargeValue_UpdatesSuccessfully()
//         {
//             var athlete = new Athlete
//             {
//                 PhoneNumber = "09121234567",
//                 RestTime = 30,
//                 UserId = 1,
//                 User = new User { PhoneNumber = "09121234567" }
//             };
//
//             // Arrange
//             _context.Athletes.Add(athlete);
//             await _context.SaveChangesAsync();
//
//             // Act
//             var result = await _controller.UpdateRestTime(999);
//
//             // Assert
//             var okResult = Assert.IsType<OkObjectResult>(result);
//
//             var updatedAthlete = await _context.Athletes.FirstAsync();
//             Assert.Equal(999, updatedAthlete.RestTime);
//         }
//
//         #endregion
//
//         #region GetAllPayments
//
//         [Fact]
//         public async Task GetAllPayments_WithValidPhoneNumber_ReturnsOkResult()
//         {
//             var response = new ApiResponse { Action = true, Message = "Payments retrieved" };
//             _mockAthleteRepository
//                 .Setup(r => r.GetAllPayments("09121234567"))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.GetAllPayments();
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal(response, okResult.Value);
//         }
//
//         [Fact]
//         public async Task GetAllPayments_WhenRepositoryReturnsFalse_ReturnsBadRequest()
//         {
//             var response = new ApiResponse { Action = false, Message = "No payments found" };
//             _mockAthleteRepository
//                 .Setup(r => r.GetAllPayments("09121234567"))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.GetAllPayments();
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal(response, badResult.Value);
//         }
//
//         [Fact]
//         public async Task GetAllPayments_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims(null);
//
//             var result = await _controller.GetAllPayments();
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("PhoneNumber is null", badResult.Value);
//         }
//
//         #endregion
//
//         #region GetPayment
//
//         [Fact]
//         public async Task GetPayment_WithValidPaymentId_ReturnsOkResult()
//         {
//             var response = new ApiResponse { Action = true, Message = "Payment found" };
//             _mockAthleteRepository
//                 .Setup(r => r.GetPayment("09121234567", 1))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.GetPayment(1);
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal(response, okResult.Value);
//         }
//
//         [Fact]
//         public async Task GetPayment_WhenPaymentNotFound_ReturnsBadRequest()
//         {
//             var response = new ApiResponse { Action = false, Message = "Payment not found" };
//             _mockAthleteRepository
//                 .Setup(r => r.GetPayment("09121234567", 999))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.GetPayment(999);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal(response, badResult.Value);
//         }
//
//         [Fact]
//         public async Task GetPayment_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims(null);
//
//             var result = await _controller.GetPayment(1);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("PhoneNumber is null", badResult.Value);
//         }
//
//         [Fact]
//         public async Task GetPayment_CallsRepositoryWithCorrectParameters()
//         {
//             var response = new ApiResponse { Action = true, Message = "Payment found" };
//             _mockAthleteRepository
//                 .Setup(r => r.GetPayment("09121234567", 5))
//                 .ReturnsAsync(response);
//
//             await _controller.GetPayment(5);
//
//             _mockAthleteRepository.Verify(r => r.GetPayment("09121234567", 5), Times.Once);
//         }
//
//         #endregion
//
//         #region ActiveProgram
//
//         [Fact]
//         public async Task ActiveProgram_WithValidPaymentId_ReturnsOkResult()
//         {
//             var response = new ApiResponse { Action = true, Message = "Program activated" };
//             _mockAthleteRepository
//                 .Setup(r => r.ActiveProgram("09121234567", 1))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.ActiveProgram(1);
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal(response, okResult.Value);
//         }
//
//         [Fact]
//         public async Task ActiveProgram_WhenActivationFails_ReturnsBadRequest()
//         {
//             var response = new ApiResponse { Action = false, Message = "Cannot activate program" };
//             _mockAthleteRepository
//                 .Setup(r => r.ActiveProgram("09121234567", 1))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.ActiveProgram(1);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal(response, badResult.Value);
//         }
//
//         [Fact]
//         public async Task ActiveProgram_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims(null);
//
//             var result = await _controller.ActiveProgram(1);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("PhoneNumber is null", badResult.Value);
//         }
//
//         #endregion
//
//         #region FeedbackExercise
//
//         [Fact]
//         public async Task FeedbackExercise_WithValidFeedback_ReturnsOkResult()
//         {
//             var dto = new ExerciseFeedbackDto();
//             var response = new ApiResponse { Action = true, Message = "Feedback submitted" };
//             _mockAthleteRepository
//                 .Setup(r => r.ExerciseFeedBack("09121234567", dto))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.FeedbackExercise(dto);
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal(response, okResult.Value);
//         }
//
//         [Fact]
//         public async Task FeedbackExercise_WhenFeedbackSubmissionFails_ReturnsBadRequest()
//         {
//             var dto = new ExerciseFeedbackDto();
//             var response = new ApiResponse { Action = false, Message = "Cannot submit feedback" };
//             _mockAthleteRepository
//                 .Setup(r => r.ExerciseFeedBack("09121234567", dto))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.FeedbackExercise(dto);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal(response, badResult.Value);
//         }
//
//         [Fact]
//         public async Task FeedbackExercise_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims(null);
//             var dto = new ExerciseFeedbackDto();
//
//             var result = await _controller.FeedbackExercise(dto);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("PhoneNumber is null", badResult.Value);
//         }
//
//         #endregion
//
//         #region ChangeExerciseRequest
//
//         [Fact]
//         public async Task ChangeExerciseRequest_WithValidChangeDto_ReturnsOkResult()
//         {
//             var dto = new ExerciseChangeDto();
//             var response = new ApiResponse { Action = true, Message = "Exercise changed" };
//             _mockAthleteRepository
//                 .Setup(r => r.ChangeExercise("09121234567", dto))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.ChangeExerciseRequest(dto);
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal(response, okResult.Value);
//         }
//
//         [Fact]
//         public async Task ChangeExerciseRequest_WhenChangeOperationFails_ReturnsBadRequest()
//         {
//             var dto = new ExerciseChangeDto();
//             var response = new ApiResponse { Action = false, Message = "Cannot change exercise" };
//             _mockAthleteRepository
//                 .Setup(r => r.ChangeExercise("09121234567", dto))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.ChangeExerciseRequest(dto);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal(response, badResult.Value);
//         }
//
//         [Fact]
//         public async Task ChangeExerciseRequest_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims(null);
//             var dto = new ExerciseChangeDto();
//
//             var result = await _controller.ChangeExerciseRequest(dto);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("PhoneNumber is null", badResult.Value);
//         }
//
//         #endregion
//
//         #region GetAllTrainingSession
//
//         [Fact]
//         public async Task GetAllTrainingSession_WithValidPhoneNumber_ReturnsOkResult()
//         {
//             var response = new ApiResponse { Action = true, Message = "Sessions retrieved" };
//             _mockAthleteRepository
//                 .Setup(r => r.GetAllTrainingSession("09121234567"))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.GetAllTrainingSession();
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal(response, okResult.Value);
//         }
//
//         [Fact]
//         public async Task GetAllTrainingSession_WhenNoSessionsFound_ReturnsBadRequest()
//         {
//             var response = new ApiResponse { Action = false, Message = "No sessions found" };
//             _mockAthleteRepository
//                 .Setup(r => r.GetAllTrainingSession("09121234567"))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.GetAllTrainingSession();
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal(response, badResult.Value);
//         }
//
//         [Fact]
//         public async Task GetAllTrainingSession_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims(null);
//
//             var result = await _controller.GetAllTrainingSession();
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("PhoneNumber is null", badResult.Value);
//         }
//
//         #endregion
//
//         #region GetTrainingSession
//
//         [Fact]
//         public async Task GetTrainingSession_WithValidSessionId_ReturnsOkResult()
//         {
//             var response = new ApiResponse { Action = true, Message = "Session found" };
//             _mockAthleteRepository
//                 .Setup(r => r.GetTrainingSession("09121234567", 1))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.GetTrainingSession(1);
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal(response, okResult.Value);
//         }
//
//         [Fact]
//         public async Task GetTrainingSession_WhenSessionNotFound_ReturnsBadRequest()
//         {
//             var response = new ApiResponse { Action = false, Message = "Session not found" };
//             _mockAthleteRepository
//                 .Setup(r => r.GetTrainingSession("09121234567", 999))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.GetTrainingSession(999);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal(response, badResult.Value);
//         }
//
//         [Fact]
//         public async Task GetTrainingSession_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims(null);
//
//             var result = await _controller.GetTrainingSession(1);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("PhoneNumber is null", badResult.Value);
//         }
//
//         #endregion
//
//         #region DoTrainingSession
//
//         [Fact]
//         public async Task DoTrainingSession_WithValidSessionAndExerciseNumber_ReturnsOkResult()
//         {
//             var response = new ApiResponse { Action = true, Message = "Exercise completed" };
//             _mockAthleteRepository
//                 .Setup(r => r.DoTrainingSession("09121234567", 1, 1))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.DoTrainingSession(1, 1);
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal(response, okResult.Value);
//         }
//
//         [Fact]
//         public async Task DoTrainingSession_WhenSessionExecutionFails_ReturnsBadRequest()
//         {
//             var response = new ApiResponse { Action = false, Message = "Cannot execute session" };
//             _mockAthleteRepository
//                 .Setup(r => r.DoTrainingSession("09121234567", 1, 1))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.DoTrainingSession(1, 1);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal(response, badResult.Value);
//         }
//
//         [Fact]
//         public async Task DoTrainingSession_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims(null);
//
//             var result = await _controller.DoTrainingSession(1, 1);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("PhoneNumber is null", badResult.Value);
//         }
//
//         [Fact]
//         public async Task DoTrainingSession_WithMultipleExerciseNumber_CallsRepositoryCorrectly()
//         {
//             var response = new ApiResponse { Action = true, Message = "Exercise completed" };
//             _mockAthleteRepository
//                 .Setup(r => r.DoTrainingSession("09121234567", 2, 5))
//                 .ReturnsAsync(response);
//
//             await _controller.DoTrainingSession(2, 5);
//
//             _mockAthleteRepository.Verify(r => r.DoTrainingSession("09121234567", 2, 5), Times.Once);
//         }
//
//         #endregion
//
//         #region FinishTrainingSession
//
//         [Fact]
//         public async Task FinishTrainingSession_WithValidData_ReturnsOkResult()
//         {
//             var dto = new FinishTrainingSessionDto { TrainingSessionName = "Session1", TrainingSessionId = 1, Duration = 60, CaloriesLost = 200 };
//             var response = new ApiResponse { Action = true, Message = "Session finished" };
//             _mockAthleteRepository
//                 .Setup(r => r.FinishTrainingSession("09121234567", dto))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.FinishTrainingSession(dto);
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal(response, okResult.Value);
//         }
//
//         [Fact]
//         public async Task FinishTrainingSession_WhenOperationFails_ReturnsBadRequest()
//         {
//             var dto = new FinishTrainingSessionDto { TrainingSessionName = "Session1", TrainingSessionId = 1, Duration = 60, CaloriesLost = 200 };
//             var response = new ApiResponse { Action = false, Message = "Cannot finish session" };
//             _mockAthleteRepository
//                 .Setup(r => r.FinishTrainingSession("09121234567", dto))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.FinishTrainingSession(dto);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal(response, badResult.Value);
//         }
//
//         [Fact]
//         public async Task FinishTrainingSession_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims("");
//             var dto = new FinishTrainingSessionDto { TrainingSessionName = "Session1", TrainingSessionId = 1, Duration = 60, CaloriesLost = 200 };
//
//             var result = await _controller.FinishTrainingSession(dto);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("PhoneNumber is null", badResult.Value);
//         }
//
//         #endregion
//
//         #region FeedbackTrainingSession
//
//         [Fact]
//         public async Task FeedbackTrainingSession_WithValidFeedback_ReturnsOkResult()
//         {
//             var dto = new FeedbackTrainingSessionDto();
//             var response = new ApiResponse { Action = true, Message = "Feedback submitted" };
//             _mockAthleteRepository
//                 .Setup(r => r.FeedbackTrainingSession("09121234567", dto))
//                 .ReturnsAsync(response);
//         
//             var result = await _controller.FeedbackTrainingSession(dto);
//         
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal(response, okResult.Value);
//         }
//
//         [Fact]
//         public async Task FeedbackTrainingSession_WhenSubmissionFails_ReturnsBadRequest()
//         {
//             var dto = new FeedbackTrainingSessionDto();
//             var response = new ApiResponse { Action = false, Message = "Cannot submit feedback" };
//             _mockAthleteRepository
//                 .Setup(r => r.FeedbackTrainingSession("09121234567", dto))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.FeedbackTrainingSession(dto);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal(response, badResult.Value);
//         }
//
//         [Fact]
//         public async Task FeedbackTrainingSession_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims(null);
//             var dto = new FeedbackTrainingSessionDto();
//
//             var result = await _controller.FeedbackTrainingSession(dto);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("PhoneNumber is null", badResult.Value);
//         }
//
//         #endregion
//
//         #region CalculateCalories
//
//         [Fact]
//         public async Task CalculateCalories_WithValidSessionId_ReturnsOkResult()
//         {
//             var response = new ApiResponse { Action = true, Message = "Calories calculated" };
//             _mockAthleteRepository
//                 .Setup(r => r.CalculateCalories("09121234567", 1))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.CalculateCalories(1);
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal(response, okResult.Value);
//         }
//
//         [Fact]
//         public async Task CalculateCalories_WhenCalculationFails_ReturnsBadRequest()
//         {
//             var response = new ApiResponse { Action = false, Message = "Cannot calculate calories" };
//             _mockAthleteRepository
//                 .Setup(r => r.CalculateCalories("09121234567", 1))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.CalculateCalories(1);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal(response, badResult.Value);
//         }
//
//         [Fact]
//         public async Task CalculateCalories_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims(null);
//
//             var result = await _controller.CalculateCalories(1);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("PhoneNumber is null", badResult.Value);
//         }
//
//         #endregion
//
//         #region ResetTrainingSession
//
//         [Fact]
//         public async Task ResetTrainingSession_WithValidSessionId_ReturnsOkResult()
//         {
//             var response = new ApiResponse { Action = true, Message = "Session reset" };
//             _mockAthleteRepository
//                 .Setup(r => r.ResetTrainingSession("09121234567", 1))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.ResetTrainingSession(1);
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal(response, okResult.Value);
//         }
//
//         [Fact]
//         public async Task ResetTrainingSession_WhenResetFails_ReturnsBadRequest()
//         {
//             var response = new ApiResponse { Action = false, Message = "Cannot reset session" };
//             _mockAthleteRepository
//                 .Setup(r => r.ResetTrainingSession("09121234567", 1))
//                 .ReturnsAsync(response);
//
//             var result = await _controller.ResetTrainingSession(1);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal(response, badResult.Value);
//         }
//
//         [Fact]
//         public async Task ResetTrainingSession_WithoutPhoneNumberClaim_ReturnsBadRequest()
//         {
//             SetupUserClaims(null);
//
//             var result = await _controller.ResetTrainingSession(1);
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal("PhoneNumber is null", badResult.Value);
//         }
//
//         #endregion
//
//         #region GetFaq
//
//         [Fact]
//         public async Task GetFaq_WithValidPhoneNumber_ReturnsOkResult()
//         {
//             var response = new ApiResponse { Action = true, Message = "FAQ retrieved" };
//             _mockAthleteRepository
//                 .Setup(r => r.GetFaq())
//                 .ReturnsAsync(response);
//
//             var result = await _controller.GetFaq();
//
//             var okResult = Assert.IsType<OkObjectResult>(result);
//             Assert.Equal(response, okResult.Value);
//         }
//
//         [Fact]
//         public async Task GetFaq_WhenFaqRetrievalFails_ReturnsBadRequest()
//         {
//             var response = new ApiResponse { Action = false, Message = "Cannot retrieve FAQ" };
//             _mockAthleteRepository
//                 .Setup(r => r.GetFaq())
//                 .ReturnsAsync(response);
//
//             var result = await _controller.GetFaq();
//
//             var badResult = Assert.IsType<BadRequestObjectResult>(result);
//             Assert.Equal(response, badResult.Value);
//         }
//
//         [Fact]
//         public async Task GetFaq_WithoutPhoneNumberClaim_ReturnsUnauthorized()
//         {
//             SetupUserClaims(null);
//
//             var result = await _controller.GetFaq();
//
//             var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
//             var response = unauthorizedResult.Value as ApiResponse;
//             Assert.NotNull(response);
//             Assert.False(response.Action);
//             Assert.Equal("خطای احراز هویت.", response.Message);
//         }
//
//         [Fact]
//         public async Task GetFaq_WithEmptyPhoneNumberClaim_ReturnsUnauthorized()
//         {
//             SetupUserClaims("");
//
//             var result = await _controller.GetFaq();
//
//             var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
//             var response = unauthorizedResult.Value as ApiResponse;
//             Assert.NotNull(response);
//             Assert.False(response.Action);
//         }
//
//         [Fact]
//         public async Task GetFaq_DoesNotRequirePhoneNumberFromRequest()
//         {
//             var response = new ApiResponse { Action = true, Message = "FAQ retrieved" };
//             _mockAthleteRepository
//                 .Setup(r => r.GetFaq())
//                 .ReturnsAsync(response);
//
//             var result = await _controller.GetFaq();
//
//             _mockAthleteRepository.Verify(r => r.GetFaq(), Times.Once);
//         }
//
//         #endregion
//
//         #region Helper Methods
//
//         private static Mock<IQueryable<T>> GetMockDbSet<T>(IEnumerable<T> data) where T : class
//         {
//             var queryableData = data.AsQueryable();
//             var mockDbSet = new Mock<DbSet<T>>();
//
//             mockDbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryableData.Provider);
//             mockDbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryableData.Expression);
//             mockDbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryableData.ElementType);
//             mockDbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryableData.GetEnumerator());
//
//             mockDbSet.Setup(m => m.FirstOrDefaultAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
//                 .Returns((Expression<Func<T, bool>> predicate, CancellationToken ct) =>
//                 {
//                     var func = predicate.Compile();
//                     var item = queryableData.FirstOrDefault(func);
//                     return Task.FromResult(item);
//                 });
//
//             return new Mock<IQueryable<T>>(mockDbSet);
//         }
//
//         #endregion
//     }
// }
//
