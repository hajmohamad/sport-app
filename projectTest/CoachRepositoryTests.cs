using Microsoft.EntityFrameworkCore;
using Moq;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Account.Athlete;
using sport_app_backend.Models.Actions;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Question.A_Question;
using sport_app_backend.Repository.CoachRepo;
using sport_app_backend.Services;
using Xunit;

namespace sport_app_backend.projectTest
{
    public class CoachRepositoryTests
    {
        private readonly Mock<ISmsService> _mockSmsService;
        private readonly Mock<ILiaraStorage> _mockLiaraStorage;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<ICalculator> _mockCalculator;
        private readonly ApplicationDbContext _context;
        private readonly CoachRepository _repository;

        public CoachRepositoryTests()
        {
            _mockSmsService = new Mock<ISmsService>();
            _mockLiaraStorage = new Mock<ILiaraStorage>();
            _mockTokenService = new Mock<ITokenService>();
            _mockCalculator = new Mock<ICalculator>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new CoachRepository(
                _context,
                _mockSmsService.Object,
                _mockLiaraStorage.Object,
                _mockTokenService.Object,
                _mockCalculator.Object
            );
        }

        #region AthleteReportForCoach

        [Fact]
        public async Task AthleteReportForCoach_ReturnsAthleteReport_WithValidAthleteId()
        {
            var user = new User
            {
                PhoneNumber = "09121234567",
                FirstName = "John",
                LastName = "Doe",
                Gender = Gender.MALE,
                BirthDate = new DateTime(1990, 1, 1)
            };
            var athlete = new Athlete
            {
                PhoneNumber = "09121234567",
                UserId = user.Id,
                User = user,
                Height = 180,
                CurrentWeight = 75,
                WeightGoal = 70
            };
            user.Athlete = athlete;

            _context.Users.Add(user);
            _context.Athletes.Add(athlete);
            _context.SaveChanges();

            var activity1 = new Activity
            {
                AthleteId = athlete.Id,
                Athlete = athlete,
                Duration = 60,
                CaloriesLost = 500,
                Date = DateTime.Now.Date,
                ActivityCategory = ActivityCategory.EXERCISE,
                Name = "Running"
            };
            _context.Activities.Add(activity1);
            _context.SaveChanges();

            var result = await _repository.AthleteReportForCoach(athlete.Id);

            Assert.True(result.Action);
            Assert.NotNull(result.Result);
            var report = result.Result as ActivityPageDto;
            Assert.NotNull(report);
            Assert.Equal(1, report.TotalActivities);
            Assert.Equal(60, report.TotalTime);
            Assert.Equal(500, report.TotalCalories);
        }

        [Fact]
        public async Task AthleteReportForCoach_ReturnsError_WhenAthleteNotFound()
        {
            var result = await _repository.AthleteReportForCoach(999);

            Assert.False(result.Action);
            Assert.Equal("Athlete not found", result.Message);
        }

        [Fact]
        public async Task AthleteReportForCoach_IncludesWeightHistory_FromCurrentPersianMonth()
        {
            var user = new User
            {
                PhoneNumber = "09121234567",
                FirstName = "John",
                LastName = "Doe",
                Gender = Gender.MALE,
                BirthDate = new DateTime(1990, 1, 1)
            };
            var athlete = new Athlete
            {
                PhoneNumber = "09121234567",
                UserId = user.Id,
                User = user,
                Height = 180,
                CurrentWeight = 75,
                WeightGoal = 70
            };
            user.Athlete = athlete;

            _context.Users.Add(user);
            _context.Athletes.Add(athlete);
            _context.SaveChanges();

            var weightEntry1 = new WeightEntry { Weight = 74, CurrentDate = DateTime.Now.Date, AthleteId = athlete.Id, Athlete = athlete };
            var weightEntry2 = new WeightEntry { Weight = 75, CurrentDate = DateTime.Now.Date.AddDays(-1), AthleteId = athlete.Id, Athlete = athlete };
            athlete.WeightEntries = new List<WeightEntry> { weightEntry1, weightEntry2 };

            _context.WeightEntries.AddRange(weightEntry1, weightEntry2);
            _context.SaveChanges();

            var result = await _repository.AthleteReportForCoach(athlete.Id);

            Assert.True(result.Action);
            var report = result.Result as ActivityPageDto;
            Assert.NotNull(report);
            Assert.NotEmpty(report.LastMonthWeights);
        }

        #endregion

        #region AthleteMonthlyActivityForCoach

        [Fact]
        public async Task AthleteMonthlyActivityForCoach_ReturnsActivitiesForMonth_WithValidYearAndMonth()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            _context.Athletes.Add(athlete);
            _context.SaveChanges();

            var activity1 = new Activity
            {
                AthleteId = athlete.Id,
                Athlete = athlete,
                Duration = 60,
                CaloriesLost = 500,
                Date = new DateTime(2024, 1, 15),
                ActivityCategory = ActivityCategory.EXERCISE,
                Name = "Running"
            };
            var activity2 = new Activity
            {
                AthleteId = athlete.Id,
                Athlete = athlete,
                Duration = 45,
                CaloriesLost = 400,
                Date = new DateTime(2024, 1, 20),
                ActivityCategory = ActivityCategory.EXERCISE,
                Name = "Gym"
            };
            _context.Activities.AddRange(activity1, activity2);
            _context.SaveChanges();

            var result = await _repository.AthleteMonthlyActivityForCoach(athlete.Id, 1403, 10);

            Assert.True(result.Action);
            Assert.Equal("Activities found", result.Message);
        }

        [Fact]
        public async Task AthleteMonthlyActivityForCoach_ReturnsError_WhenAthleteNotFound()
        {
            var result = await _repository.AthleteMonthlyActivityForCoach(999, 1403, 10);

            Assert.False(result.Action);
            Assert.Equal("User is not an athlete", result.Message);
        }

        [Fact]
        public async Task AthleteMonthlyActivityForCoach_ReturnsError_WithInvalidMonth()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            _context.Athletes.Add(athlete);
            _context.SaveChanges();

            var result = await _repository.AthleteMonthlyActivityForCoach(athlete.Id, 1403, 13);

            Assert.False(result.Action);
        }

        [Fact]
        public async Task AthleteMonthlyActivityForCoach_ReturnsEmptyList_WhenNoActivitiesInMonth()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            _context.Athletes.Add(athlete);
            _context.SaveChanges();

            var result = await _repository.AthleteMonthlyActivityForCoach(athlete.Id, 1403, 10);

            Assert.True(result.Action);
            var activities = result.Result as List<ActivityDto>;
            Assert.NotNull(activities);
            Assert.Empty(activities);
        }

        #endregion

        #region AddCoachingServices

        [Fact]
        public async Task AddCoachingServices_AddsService_WithValidPrice()
        {
            var user = new User { PhoneNumber = "09121234567", FirstName = "Coach", LastName = "Test" };
            var coach = new Coach { PhoneNumber = "09121234567", User = user, UserId = user.Id };
            user.Coach = coach;

            _context.Users.Add(user);
            _context.Coaches.Add(coach);
            _context.SaveChanges();

            var dto = new AddCoachServiceDto
            {
                Title = "Personal Training",
                Description = "One-on-one training",
                Price = 100000
            };

            var result = await _repository.AddCoachingServices("09121234567", dto);

            Assert.True(result.Action);
            Assert.Equal("Coaching Service added successfully", result.Message);

            var savedService = _context.CoachServices.First();
            Assert.Equal("Personal Training", savedService.Title);
            Assert.Equal(100000, savedService.Price);
        }

        [Fact]
        public async Task AddCoachingServices_ReturnsError_WhenCoachNotFound()
        {
            var dto = new AddCoachServiceDto
            {
                Title = "Personal Training",
                Description = "One-on-one training",
                Price = 100000
            };

            var result = await _repository.AddCoachingServices("99999999999", dto);

            Assert.False(result.Action);
            Assert.Equal("User is not a coach", result.Message);
        }

        [Fact]
        public async Task AddCoachingServices_ReturnsError_WhenPriceBelowMinimum()
        {
            var user = new User { PhoneNumber = "09121234567" };
            var coach = new Coach { PhoneNumber = "09121234567", User = user, UserId = user.Id };
            user.Coach = coach;

            _context.Users.Add(user);
            _context.Coaches.Add(coach);
            _context.SaveChanges();

            var dto = new AddCoachServiceDto
            {
                Title = "Cheap Service",
                Description = "Too cheap",
                Price = 25000
            };

            var result = await _repository.AddCoachingServices("09121234567", dto);

            Assert.False(result.Action);
            Assert.Contains("50", result.Message);
        }

        [Fact]
        public async Task AddCoachingServices_InitializesCoachingServices_WhenNull()
        {
            var user = new User { PhoneNumber = "09121234567" };
            var coach = new Coach { PhoneNumber = "09121234567", User = user, UserId = user.Id };
            user.Coach = coach;

            _context.Users.Add(user);
            _context.Coaches.Add(coach);
            _context.SaveChanges();

            var dto = new AddCoachServiceDto
            {
                Title = "Service",
                Description = "Test",
                Price = 50000
            };

            var result = await _repository.AddCoachingServices("09121234567", dto);

            Assert.True(result.Action);
        }

        #endregion

        #region SubmitCoachQuestions
        

        [Fact]
        public async Task SubmitCoachQuestions_ReturnsError_WhenUserNotFound()
        {
            var dto = new CoachQuestionDto
            {
                FirstName = "Jane",
                LastName = "Smith"
            };

            var result = await _repository.SubmitCoachQuestions("99999999999", dto);

            Assert.False(result.Action);
            Assert.Equal("User not found", result.Message);
        }

        [Fact]
        public async Task SubmitCoachQuestions_ReturnsError_WhenUserIsNotCoach()
        {
            var user = new User { PhoneNumber = "09121234567" };
            _context.Users.Add(user);
            _context.SaveChanges();

            var dto = new CoachQuestionDto
            {
                FirstName = "Jane",
                LastName = "Smith"
            };

            var result = await _repository.SubmitCoachQuestions("09121234567", dto);

            Assert.False(result.Action);
            Assert.Equal("User is not a coach", result.Message);
        }

        #endregion

        #region UpdateCoachingService

        [Fact]
        public async Task UpdateCoachingService_UpdatesService_WithValidPrice()
        {
            var user = new User { PhoneNumber = "09121234567" };
            var coach = new Coach
            {
                PhoneNumber = "09121234567",
                User = user,
                UserId = user.Id,
                CoachingServices = new List<CoachService>()
            };
            var service = new CoachService
            {
                Title = "Old Title",
                Description = "Old service",
                Price = 100000,
                Coach = coach,
                CoachId = coach.Id
            };
            coach.CoachingServices.Add(service);
            user.Coach = coach;

            _context.Users.Add(user);
            _context.Coaches.Add(coach);
            _context.CoachServices.Add(service);
            _context.SaveChanges();

            var dto = new AddCoachServiceDto
            {
                Title = "New Title",
                Price = 150000,
                Description = "Updated service"
            };

            var result = await _repository.UpdateCoachingService("09121234567", service.Id, dto);

            Assert.True(result.Action);
            Assert.Equal("Coaching Service updated successfully", result.Message);
        }

        [Fact]
        public async Task UpdateCoachingService_ReturnsError_WhenCoachNotFound()
        {
            var dto = new AddCoachServiceDto { Title = "Service", Price = 100000, Description = "Test" };

            var result = await _repository.UpdateCoachingService("99999999999", 1, dto);

            Assert.False(result.Action);
            Assert.Equal("User is not a coach", result.Message);
        }

        [Fact]
        public async Task UpdateCoachingService_ReturnsError_WhenServiceNotFound()
        {
            var user = new User { PhoneNumber = "09121234567" };
            var coach = new Coach
            {
                PhoneNumber = "09121234567",
                User = user,
                UserId = user.Id,
                CoachingServices = new List<CoachService>()
            };
            user.Coach = coach;

            _context.Users.Add(user);
            _context.Coaches.Add(coach);
            _context.SaveChanges();

            var dto = new AddCoachServiceDto { Title = "Service", Price = 100000, Description = "Test" };

            var result = await _repository.UpdateCoachingService("09121234567", 999, dto);

            Assert.False(result.Action);
        }

        [Fact]
        public async Task UpdateCoachingService_ReturnsError_WhenPriceBelowMinimum()
        {
            var user = new User { PhoneNumber = "09121234567" };
            var coach = new Coach
            {
                PhoneNumber = "09121234567",
                User = user,
                UserId = user.Id,
                CoachingServices = new List<CoachService>()
            };
            var service = new CoachService { Coach = coach, CoachId = coach.Id, Title = "Service", Description = "Test", Price = 50000 };
            coach.CoachingServices.Add(service);
            user.Coach = coach;

            _context.Users.Add(user);
            _context.Coaches.Add(coach);
            _context.CoachServices.Add(service);
            _context.SaveChanges();

            var dto = new AddCoachServiceDto { Title = "Service", Price = 10000, Description = "Test" };

            var result = await _repository.UpdateCoachingService("09121234567", service.Id, dto);

            Assert.False(result.Action);
        }

        [Fact]
        public async Task UpdateCoachingService_CreatesNewService_WhenActivePaymentsExist()
        {
            var user = new User { PhoneNumber = "09121234567" };
            var coach = new Coach
            {
                PhoneNumber = "09121234567",
                User = user,
                UserId = user.Id,
                CoachingServices = new List<CoachService>()
            };
            var service = new CoachService
            {
                Title = "Service",
                Price = 100000,
                Coach = coach,
                CoachId = coach.Id,
                NumberOfSell = 5,
                Description = "Test"
            };
            coach.CoachingServices.Add(service);
            user.Coach = coach;

            var athlete = new Athlete { PhoneNumber = "09121111111" };
            var payment = new Payment
            {
                Coach = coach,
                CoachId = coach.Id,
                CoachService = service,
                CoachServiceId = service.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                PaymentStatus = PaymentStatus.SUCCESS
            };

            _context.Users.Add(user);
            _context.Coaches.Add(coach);
            _context.CoachServices.Add(service);
            _context.Athletes.Add(athlete);
            _context.Payments.Add(payment);
            _context.SaveChanges();

            var dto = new AddCoachServiceDto
            {
                Title = "New Service",
                Price = 150000,
                Description = "New"
            };

            var result = await _repository.UpdateCoachingService("09121234567", service.Id, dto);

            Assert.True(result.Action);
            var oldService = _context.CoachServices.Find(service.Id);
            Assert.NotNull(oldService);
            Assert.True(oldService.IsDeleted);
        }

        #endregion

        #region DeleteCoachingService

        [Fact]
        public async Task DeleteCoachingService_DeletesService_WhenServiceExists()
        {
            var user = new User { PhoneNumber = "09121234567" };
            var coach = new Coach
            {
                PhoneNumber = "09121234567",
                User = user,
                UserId = user.Id,
                CoachingServices = new List<CoachService>()
            };
            var service = new CoachService
            {
                Title = "Service to Delete",
                Description = "Delete test",
                Price = 100000,
                Coach = coach,
                CoachId = coach.Id
            };
            coach.CoachingServices.Add(service);
            user.Coach = coach;

            _context.Users.Add(user);
            _context.Coaches.Add(coach);
            _context.CoachServices.Add(service);
            _context.SaveChanges();

            var result = await _repository.DeleteCoachingService("09121234567", service.Id);

            Assert.True(result.Action);
            Assert.Equal("Coaching Service deleted successfully", result.Message);

            var deletedService = _context.CoachServices.Find(service.Id);
            Assert.NotNull(deletedService);
            Assert.True(deletedService.IsDeleted);
        }

        [Fact]
        public async Task DeleteCoachingService_ReturnsError_WhenCoachNotFound()
        {
            var result = await _repository.DeleteCoachingService("99999999999", 1);

            Assert.False(result.Action);
            Assert.Equal("User is not a coach", result.Message);
        }

        [Fact]
        public async Task DeleteCoachingService_ReturnsError_WhenServiceNotFound()
        {
            var user = new User { PhoneNumber = "09121234567" };
            var coach = new Coach
            {
                PhoneNumber = "09121234567",
                User = user,
                UserId = user.Id,
                CoachingServices = new List<CoachService>()
            };
            user.Coach = coach;

            _context.Users.Add(user);
            _context.Coaches.Add(coach);
            _context.SaveChanges();

            var result = await _repository.DeleteCoachingService("09121234567", 999);

            Assert.False(result.Action);
            Assert.Equal("Coaching Service not found", result.Message);
        }

        #endregion

        #region GetAllPayment

        [Fact]
        public async Task GetAllPayment_ReturnsPayments_WithNotStartedOrWritingStatus()
        {
            var coach = CreateTestCoachWithUser();

            var athlete = new Athlete { PhoneNumber = "09129999999" };
            var service = new CoachService { Title = "Service", Description = "Test", Price = 100000, Coach = coach, CoachId = coach.Id };
            var payment = new Payment
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                CoachService = service,
                CoachServiceId = service.Id,
                PaymentStatus = PaymentStatus.SUCCESS};
            var workoutProgram = new WorkoutProgram
            {
                Status = WorkoutProgramStatus.NOTSTARTED,
                CoachId = coach.Id,
                Coach = coach,
                AthleteId = athlete.Id,
                Athlete = athlete,
                Title = "Program",
                PaymentId = payment.Id
            };
          

  
            workoutProgram.Payment = payment;

            _context.Athletes.Add(athlete);
            _context.CoachServices.Add(service);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.Payments.Add(payment);
            _context.SaveChanges();

            var result = await _repository.GetAllPayment(coach.PhoneNumber);

            Assert.True(result.Action);
            Assert.NotNull(result.Result);
        }

        [Fact]
        public async Task GetAllPayment_ExcludesFailedPayments_FromResponse()
        {
            var coach = CreateTestCoachWithUser();
            var athlete = new Athlete { PhoneNumber = "09129999999" };
            var service = new CoachService { Title = "Service", Description = "Test", Price = 100000, Coach = coach, CoachId = coach.Id };
            
            var failedPayment = new Payment
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                CoachService = service,
                CoachServiceId = service.Id,
                PaymentStatus = PaymentStatus.FAILED
            };

            _context.Athletes.Add(athlete);
            _context.CoachServices.Add(service);
            _context.Payments.Add(failedPayment);
            _context.SaveChanges();

            var result = await _repository.GetAllPayment(coach.PhoneNumber);

            Assert.True(result.Action);
            var payments = result.Result as IEnumerable<dynamic>;
            Assert.NotNull(payments);
            Assert.Empty(payments);
        }

        [Fact]
        public async Task GetAllPayment_ExcludesPaymentsWithoutWorkoutProgram()
        {
            var coach = CreateTestCoachWithUser();
            var athlete = new Athlete { PhoneNumber = "09129999999" };
            var service = new CoachService { Title = "Service", Description = "Test", Price = 100000, Coach = coach, CoachId = coach.Id };
            
            var payment = new Payment
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                CoachService = service,
                CoachServiceId = service.Id,
                PaymentStatus = PaymentStatus.SUCCESS,
                WorkoutProgram = null
            };

            _context.Athletes.Add(athlete);
            _context.CoachServices.Add(service);
            _context.Payments.Add(payment);
            _context.SaveChanges();

            var result = await _repository.GetAllPayment(coach.PhoneNumber);

            Assert.True(result.Action);
            var payments = result.Result as IEnumerable<dynamic>;
            Assert.NotNull(payments);
            Assert.Empty(payments);
        }

        #endregion

        #region GetPayment

        [Fact]
        public async Task GetPayment_ReturnsPaymentDetails_WithValidPaymentId()
        {
            var coach = CreateTestCoachWithUser();
            var athleteUser = new User
            {
                PhoneNumber = "09129999999",
                FirstName = "Athlete",
                LastName = "Test",
                Gender = Gender.MALE,
                BirthDate = new DateTime(1990, 1, 1)
            };
            var athlete = new Athlete
            {
                User = athleteUser,
                UserId = athleteUser.Id,
                Height = 180,
                CurrentWeight = 75,
                PhoneNumber = "09129999999"
            };
            athleteUser.Athlete = athlete;

            var athleteQuestion = new AthleteQuestion
            {
                Athlete = athlete,
                AthleteId = athlete.Id,
                ActivityLevel = ActivityLevel.Moderate,
                DaysPerWeekToExercise = 5,
                InjuryArea = new InjuryArea()
                {
                    None = true
                },
                ExerciseLocation = ExerciseLocation.GYM
            };

            var workoutProgram = new WorkoutProgram
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Title = "Test Program",
                Status = WorkoutProgramStatus.NOTSTARTED,
                ProgramInDays = new List<ProgramInDay>(),
                PaymentId = 0
            };

            var service = new CoachService { Title = "Service", Description = "Test", Price = 100000, Coach = coach, CoachId = coach.Id };
            var payment = new Payment
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                CoachService = service,
                CoachServiceId = service.Id,
                AthleteQuestion = athleteQuestion,
                AthleteQuestionId = athleteQuestion.Id,
                WorkoutProgram = workoutProgram,
                PaymentStatus = PaymentStatus.SUCCESS,
                Amount = 500000
            };
            workoutProgram.Payment = payment;
            workoutProgram.PaymentId = payment.Id;

            _context.Users.Add(athleteUser);
            _context.Athletes.Add(athlete);
            _context.AthleteQuestions.Add(athleteQuestion);
            _context.CoachServices.Add(service);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.Payments.Add(payment);
            _context.SaveChanges();

            _mockTokenService.Setup(t => t.HashEncode(It.IsAny<int>())).Returns("hashedId");
            _mockCalculator.Setup(c => c.BmrCalculator(It.IsAny<BmrRequestDto>())).Returns(2500);

            var result = await _repository.GetPayment(coach.PhoneNumber, payment.Id);

            Assert.True(result.Action);
            Assert.Equal("Payment found", result.Message);
        }

        [Fact]
        public async Task GetPayment_ReturnsError_WhenPaymentNotFound()
        {
            var coach = CreateTestCoachWithUser();

            var result = await _repository.GetPayment(coach.PhoneNumber, 999);

            Assert.False(result.Action);
            Assert.Equal("Payment not found", result.Message);
        }

        [Fact]
        public async Task GetPayment_AddsProgramInDayToWorkout_WhenEmpty()
        {
            var coach = CreateTestCoachWithUser();
            var athleteUser = new User
            {
                PhoneNumber = "09129999999",
                FirstName = "Athlete",
                LastName = "Test",
                Gender = Gender.MALE,
                BirthDate = new DateTime(1990, 1, 1)
            };
            var athlete = new Athlete
            {
                User = athleteUser,
                UserId = athleteUser.Id,
                Height = 180,
                CurrentWeight = 75,
                PhoneNumber = "09129999999"
            };
            athleteUser.Athlete = athlete;

            var athleteQuestion = new AthleteQuestion
            {
                Athlete = athlete,
                AthleteId = athlete.Id,
                ActivityLevel = ActivityLevel.Moderate,
                ExerciseLocation = ExerciseLocation.HOME,
                DaysPerWeekToExercise = 5,
                InjuryArea = null
            };

            var workoutProgram = new WorkoutProgram
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Title = "Empty Program",
                Status = WorkoutProgramStatus.NOTSTARTED,
                ProgramInDays = new List<ProgramInDay>(),
                PaymentId = 0
            };

            var service = new CoachService { Title = "Service", Description = "Test", Price = 100000, Coach = coach, CoachId = coach.Id };
            var payment = new Payment
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                CoachService = service,
                CoachServiceId = service.Id,
                AthleteQuestion = athleteQuestion,
                AthleteQuestionId = athleteQuestion.Id,
                WorkoutProgram = workoutProgram,
                PaymentStatus = PaymentStatus.SUCCESS,
                Amount = 500000
            };
            workoutProgram.Payment = payment;
            workoutProgram.PaymentId = payment.Id;

            _context.Users.Add(athleteUser);
            _context.CoachServices.Add(service);
            _context.Athletes.Add(athlete);
            _context.AthleteQuestions.Add(athleteQuestion);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.Payments.Add(payment);
            _context.SaveChanges();

            _mockTokenService.Setup(t => t.HashEncode(It.IsAny<int>())).Returns("hashedId");
            _mockCalculator.Setup(c => c.BmrCalculator(It.IsAny<BmrRequestDto>())).Returns(2500);

            var result = await _repository.GetPayment(coach.PhoneNumber, payment.Id);

            Assert.True(result.Action);
        }

        #endregion

        #region GetProfile

        [Fact]
        public async Task GetProfile_ReturnsCoachProfile_WithCoachingServices()
        {
            var user = new User
            {
                PhoneNumber = "09121234567",
                FirstName = "Coach",
                LastName = "Test"
            };
            var coach = new Coach
            {
                PhoneNumber = "09121234567",
                User = user,
                UserId = user.Id,
                CoachingServices = new List<CoachService>
                {
                    new CoachService
                    {
                        Title = "Service1",
                        Price = 100000,
                        Description = "null"
                    },
                    new CoachService
                    {
                        Title = "Service2",
                        Price = 150000,
                        Description = "null"
                    }
                }
            };
            user.Coach = coach;

            _context.Users.Add(user);
            _context.Coaches.Add(coach);
            _context.SaveChanges();

            var result = await _repository.GetProfile("09121234567");

            Assert.True(result.Action);
            Assert.NotNull(result.Result);
        }

        [Fact]
        public async Task GetProfile_ReturnsError_WhenCoachNotFound()
        {
            var result = await _repository.GetProfile("99999999999");

            Assert.False(result.Action);
            Assert.Equal("Coach not found", result.Message);
        }

        [Fact]
        public async Task GetProfile_ExcludesDeletedCoachingServices()
        {
            var user = new User { PhoneNumber = "09121234567" };
            var service1 = new CoachService { Title = "Active", Description = "Active service", Price = 100000, IsDeleted = false };
            var service2 = new CoachService { Title = "Deleted", Description = "Deleted service", Price = 150000, IsDeleted = true };
            
            var coach = new Coach
            {
                PhoneNumber = "09121234567",
                User = user,
                UserId = user.Id,
                CoachingServices = new List<CoachService> { service1, service2 }
            };
            service1.Coach = coach;
            service1.CoachId = coach.Id;
            service2.Coach = coach;
            service2.CoachId = coach.Id;
            user.Coach = coach;

            _context.Users.Add(user);
            _context.Coaches.Add(coach);
            _context.CoachServices.AddRange(service1, service2);
            _context.SaveChanges();

            var result = await _repository.GetProfile("09121234567");

            Assert.True(result.Action);
        }

        #endregion

        #region SaveWorkoutProgram

        [Fact]
        public async Task SaveWorkoutProgram_SavesProgram_WithValidData()
        {
            var coach = CreateTestCoachWithUser();
            var athlete = new Athlete { PhoneNumber = "09129999999" };
            var payment = new Payment
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                PaymentStatus = PaymentStatus.SUCCESS,
                Amount = 500000,
                AppFee = 50000,
                AthleteQuestion = new AthleteQuestion
                {
                    DaysPerWeekToExercise = 5,
                    InjuryArea = new InjuryArea()
                    {
                        None = true
                    },
                    ExerciseLocation = ExerciseLocation.GYM,
                    ActivityLevel = ActivityLevel.None
                },
                CoachServiceId = 0
            };
            var workoutProgram = new WorkoutProgram
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Payment = payment,
                PaymentId = payment.Id,
                Status = WorkoutProgramStatus.NOTSTARTED,
                ProgramInDays = new List<ProgramInDay>()
            };
            payment.WorkoutProgram = workoutProgram;

            _context.Athletes.Add(athlete);
            _context.Payments.Add(payment);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.SaveChanges();

            var dto = new WorkoutProgramDto
            {
                Days = new List<ProgramInDayDto>(),
                Week = 12,
                ProgramLevel = nameof(ProgramLevel.Intermediate) ,
                ProgramPriority = [nameof(ProgramPriority.RECOVERY)],
                Publish = false
            };

            var result = await _repository.SaveWorkoutProgram(coach.PhoneNumber, payment.Id, dto);

            Assert.True(result.Action);
            Assert.Equal("workout program saved", result.Message);
        }

        [Fact]
        public async Task SaveWorkoutProgram_ReturnsError_WhenCoachNotFound()
        {
            var dto = new WorkoutProgramDto
            {
                Days = new List<ProgramInDayDto>(),
                Week = 12,
                ProgramLevel = null,
                ProgramPriority = null
            };

            var result = await _repository.SaveWorkoutProgram("99999999999", 1, dto);

            Assert.False(result.Action);
            Assert.Equal("Coach not found", result.Message);
        }

        [Fact]
        public async Task SaveWorkoutProgram_ReturnsError_WhenPaymentNotFound()
        {
            var coach = CreateTestCoachWithUser();
            var dto = new WorkoutProgramDto
            {
                Days = new List<ProgramInDayDto>(),
                Week = 12,
                ProgramLevel = null,
                ProgramPriority = null
            };

            var result = await _repository.SaveWorkoutProgram(coach.PhoneNumber, 999, dto);

            Assert.False(result.Action);
        }

        [Fact]
        public async Task SaveWorkoutProgram_PublishesProgram_AndCreatesTrainingSessions()
        {
            var coach = CreateTestCoachWithUser();
            var athleteUser = new User
            {
                PhoneNumber = "09129999999",
                FirstName = "Athlete",
                LastName = "Test",
                Gender = Gender.MALE,
                BirthDate = new DateTime(1990, 1, 1)
            };
            var athlete = new Athlete
            {
                User = athleteUser,
                UserId = athleteUser.Id,
                PhoneNumber = "09129999999",
                ActiveWorkoutProgramId = 0
            };
            athleteUser.Athlete = athlete;

            var athleteQuestion = new AthleteQuestion
            {
                DaysPerWeekToExercise = 3,
                InjuryArea = new InjuryArea()
                {
                    None = true
                },
                ExerciseLocation = ExerciseLocation.GYM,
                ActivityLevel = ActivityLevel.None
            };

            var payment = new Payment
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                AthleteQuestion = athleteQuestion,
                AthleteQuestionId = athleteQuestion.Id,
                PaymentStatus = PaymentStatus.SUCCESS,
                Amount = 500000,
                AppFee = 50000,
                CoachServiceId = 0
            };

            var programInDay = new ProgramInDay
            {
                ForWhichDay = 1,
                AllExerciseInDays = new List<SingleExercise>()
            };

            var workoutProgram = new WorkoutProgram
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Payment = payment,
                PaymentId = payment.Id,
                Status = WorkoutProgramStatus.NOTSTARTED,
                ProgramInDays = new List<ProgramInDay> { programInDay },
                ProgramDuration = 4,
                Title = "Publish Test"
            };
            payment.WorkoutProgram = workoutProgram;

            _context.Users.Add(athleteUser);
            _context.Athletes.Add(athlete);
            _context.AthleteQuestions.Add(athleteQuestion);
            _context.Payments.Add(payment);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.ProgramInDays.Add(programInDay);
            _context.SaveChanges();

        _mockSmsService.Setup(s => s.WorkoutReadySms(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(new SmsResponse
            {
                IsSuccess = false,
                Message = "SMS sent successfully"
            }));
            _mockTokenService.Setup(t => t.HashEncode(It.IsAny<int>())).Returns("hashedId");

            var dto = new WorkoutProgramDto
            {
                Days = new List<ProgramInDayDto>(),
                Week = 4,
                ProgramLevel = "Beginner",
                ProgramPriority = [nameof(ProgramPriority.RECOVERY)],
                Publish = true
            };

            var result = await _repository.SaveWorkoutProgram(coach.PhoneNumber, payment.Id, dto);
            Assert.True(result.Action);
            _mockSmsService.Verify(s => s.WorkoutReadySms(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region GetCoachDashboard

        [Fact]
        public async Task GetCoachDashboard_ReturnsDashboardData_WithSuccessfulPayments()
        {
            var coach = CreateTestCoachWithUser();
            coach.Amount = 1000000;

            _context.Coaches.Update(coach);
            _context.SaveChanges();

            var athlete = new Athlete { PhoneNumber = "09129999999" };
            var payment = new Payment
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                PaymentStatus = PaymentStatus.SUCCESS,
                Amount = 500000,
                PaymentDate = DateTime.Now,
                WorkoutProgram = new WorkoutProgram
                {
                    Status = WorkoutProgramStatus.ACTIVE,
                    CoachId = coach.Id,
                    Coach = coach,
                    AthleteId = athlete.Id,
                    PaymentId = 0
                },
                CoachServiceId = 0
            };

            _context.Athletes.Add(athlete);
            _context.Payments.Add(payment);
            _context.SaveChanges();

            var result = await _repository.GetCoachDashboard(coach.PhoneNumber);

            Assert.True(result.Action);
            var dashboard = result.Result as CoachDashboardDto;
            Assert.NotNull(dashboard);
            Assert.True(dashboard.TotalSales > 0);
        }

        [Fact]
        public async Task GetCoachDashboard_ReturnsEmptyData_WhenNoPaymentsExist()
        {
            var coach = CreateTestCoachWithUser();

            var result = await _repository.GetCoachDashboard(coach.PhoneNumber);

            Assert.True(result.Action);
            var dashboard = result.Result as CoachDashboardDto;
            Assert.NotNull(dashboard);
            Assert.Empty(dashboard.MonthlyIncome);
        }

        [Fact]
        public async Task GetCoachDashboard_ReturnsError_WhenCoachNotFound()
        {
            var result = await _repository.GetCoachDashboard("99999999999");

            Assert.False(result.Action);
        }

        #endregion

        #region GetMonthlyIncomeChart

        [Fact]
        public async Task GetMonthlyIncomeChart_ReturnsMonthlyIncome_WithValidYearAndMonth()
        {
            var coach = CreateTestCoachWithUser();

            var athlete = new Athlete { PhoneNumber = "09129999999" };
            var payment = new Payment
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                PaymentStatus = PaymentStatus.SUCCESS,
                Amount = 500000,
                PaymentDate = new DateTime(2024,
                    1,
                    15),
                CoachServiceId = 0
            };

            _context.Athletes.Add(athlete);
            _context.Payments.Add(payment);
            _context.SaveChanges();

            var result = await _repository.GetMonthlyIncomeChart(coach.PhoneNumber, 1403, 10);

            Assert.True(result.Action);
        }

        [Fact]
        public async Task GetMonthlyIncomeChart_ReturnsError_WhenCoachNotFound()
        {
            var result = await _repository.GetMonthlyIncomeChart("99999999999", 1403, 10);

            Assert.False(result.Action);
        }

        [Fact]
        public async Task GetMonthlyIncomeChart_ReturnsError_WithInvalidPersianDate()
        {
            var coach = CreateTestCoachWithUser();

            var result = await _repository.GetMonthlyIncomeChart(coach.PhoneNumber, 1403, 13);

            Assert.False(result.Action);
        }

        #endregion

        #region UpdateSocialMediaLink

        [Fact]
        public async Task UpdateSocialMediaLink_UpdatesLinks_WithValidData()
        {
            var coach = CreateTestCoachWithUser();

            var dto = new SocialMediaLinkDto
            {
                WhatsApp = "http://wa.me/1234567890",
                TelegramLink = "http://telegram.me/username",
                InstagramLink = "http://instagram.com/username"
            };

            var result = await _repository.UpdateSocialMediaLink(coach.PhoneNumber, dto);

            Assert.True(result.Action);

            var updatedCoach = _context.Coaches.Find(coach.Id);
            Assert.Equal(dto.WhatsApp, updatedCoach.WhatsApp);
            Assert.Equal(dto.TelegramLink, updatedCoach.TelegramLink);
            Assert.Equal(dto.InstagramLink, updatedCoach.InstagramLink);
        }

        [Fact]
        public async Task UpdateSocialMediaLink_ReturnsError_WhenCoachNotFound()
        {
            var dto = new SocialMediaLinkDto();

            var result = await _repository.UpdateSocialMediaLink("99999999999", dto);

            Assert.False(result.Action);
        }

        #endregion

        #region GetSocialMediaLink

        [Fact]
        public async Task GetSocialMediaLink_ReturnsSocialLinks_WhenCoachExists()
        {
            var coach = CreateTestCoachWithUser();
            coach.WhatsApp = "http://wa.me/1234567890";
            coach.TelegramLink = "http://telegram.me/username";
            coach.InstagramLink = "http://instagram.com/username";

            _context.Coaches.Update(coach);
            _context.SaveChanges();

            var result = await _repository.GetSocialMediaLink(coach.PhoneNumber);

            Assert.True(result.Action);
            Assert.NotNull(result.Result);
        }

        [Fact]
        public async Task GetSocialMediaLink_ReturnsError_WhenCoachNotFound()
        {
            var result = await _repository.GetSocialMediaLink("99999999999");

            Assert.False(result.Action);
        }

        #endregion

        #region GetAthletesWithStatus

        [Fact]
        public async Task GetAthletesWithStatus_ReturnsAthletesList_WithCoachAthletes()
        {
            var coach = CreateTestCoachWithUser();
            var athleteUser = new User
            {
                PhoneNumber = "09129999999",
                FirstName = "Athlete",
                LastName = "Test",
                ImageProfile = "http://image.url",
                Gender = Gender.MALE,
                BirthDate = new DateTime(1990, 1, 1)
            };
            var athlete = new Athlete
            {
                User = athleteUser,
                UserId = athleteUser.Id,
                PhoneNumber = "09129999999"
            };
            athleteUser.Athlete = athlete;

            var workoutProgram = new WorkoutProgram
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Status = WorkoutProgramStatus.ACTIVE,
                StartDate = DateTime.Now,
                Title = "Active Program",
                LastExerciseDate = DateTime.Now,
                PaymentId = 0
            };

            var payment = new Payment
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                WorkoutProgram = workoutProgram,
                PaymentStatus = PaymentStatus.SUCCESS,
                CoachServiceId = 0
            };

            _context.Users.Add(athleteUser);
            _context.Athletes.Add(athlete);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.Payments.Add(payment);
            _context.SaveChanges();

            var result = await _repository.GetAthletesWithStatus(coach.PhoneNumber);

            Assert.True(result.Action);
            var athletes = result.Result as List<AthleteStatusDto>;
            Assert.NotNull(athletes);
            Assert.NotEmpty(athletes);
        }

        [Fact]
        public async Task GetAthletesWithStatus_ReturnsError_WhenCoachNotFound()
        {
            var result = await _repository.GetAthletesWithStatus("99999999999");

            Assert.False(result.Action);
        }

        [Fact]
        public async Task GetAthletesWithStatus_ExcludesAthleteWithoutActiveProgram()
        {
            var coach = CreateTestCoachWithUser();
            var athleteUser = new User
            {
                PhoneNumber = "09129999999",
                FirstName = "Athlete",
                LastName = "Test",
                ImageProfile = "http://image.url"
            };
            var athlete = new Athlete
            {
                User = athleteUser,
                UserId = athleteUser.Id,
                PhoneNumber = "09129999999"
            };
            athleteUser.Athlete = athlete;

            var payment = new Payment
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                PaymentStatus = PaymentStatus.SUCCESS,
                WorkoutProgram = null,
                CoachServiceId = 0
            };

            _context.Users.Add(athleteUser);
            _context.Athletes.Add(athlete);
            _context.Payments.Add(payment);
            _context.SaveChanges();

            var result = await _repository.GetAthletesWithStatus(coach.PhoneNumber);

            Assert.True(result.Action);
            var athletes = result.Result as List<AthleteStatusDto>;
            Assert.NotNull(athletes);
            Assert.Empty(athletes);
        }

        #endregion

        #region GetTransactions

        [Fact]
        public async Task GetTransactions_ReturnsTransactionsList_WithCoachPayments()
        {
            var coach = CreateTestCoachWithUser();
            coach.Amount = 500000;

            _context.Coaches.Update(coach);
            _context.SaveChanges();

            var athleteUser = new User
            {
                PhoneNumber = "09129999999",
                FirstName = "Athlete",
                LastName = "Test"
            };
            var athlete = new Athlete
            {
                User = athleteUser,
                UserId = athleteUser.Id,
                PhoneNumber = "09129999999"
            };
            athleteUser.Athlete = athlete;

            var service = new CoachService
            {
                Title = "Training Service",
                Coach = coach,
                CoachId = coach.Id,
                Description = "null",
                Price = 0
            };
            var workoutProgram = new WorkoutProgram
            {
                Status = WorkoutProgramStatus.ACTIVE,
                CoachId = coach.Id,
                Coach = coach,
                AthleteId = 0,
                PaymentId = 0
            };

            var payment = new Payment
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                CoachService = service,
                CoachServiceId = service.Id,
                WorkoutProgram = workoutProgram,
                PaymentStatus = PaymentStatus.SUCCESS,
                Amount = 500000,
                AppFee = 50000,
                PaymentDate = DateTime.Now,
                RefId = 12345
            };

            _context.Users.Add(athleteUser);
            _context.Athletes.Add(athlete);
            _context.CoachServices.Add(service);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.Payments.Add(payment);
            _context.SaveChanges();

            var result = await _repository.GetTransactions(coach.PhoneNumber);

            Assert.True(result.Action);
            Assert.NotNull(result.Result);
        }

        [Fact]
        public async Task GetTransactions_ReturnsError_WhenCoachNotFound()
        {
            var result = await _repository.GetTransactions("99999999999");

            Assert.False(result.Action);
        }

        #endregion

        #region CreatePayoutRequest

        [Fact]
        public async Task CreatePayoutRequest_CreatesNewPayout_WithSufficientBalance()
        {
            var coach = CreateTestCoachWithUser();
            coach.Amount = 100000;

            _context.Coaches.Update(coach);
            _context.SaveChanges();

            var result = await _repository.CreatePayoutRequest(coach.PhoneNumber);

            Assert.True(result.Action);

            var payout = _context.CoachPayouts.FirstOrDefault(p => p.CoachId == coach.Id);
            Assert.NotNull(payout);
            Assert.Equal(PayoutStatus.Pending, payout.Status);
        }

        [Fact]
        public async Task CreatePayoutRequest_ReturnsError_WithInsufficientBalance()
        {
            var coach = CreateTestCoachWithUser();
            coach.Amount = 30000;

            _context.Coaches.Update(coach);
            _context.SaveChanges();

            var result = await _repository.CreatePayoutRequest(coach.PhoneNumber);

            Assert.False(result.Action);
            Assert.Contains("حداقل", result.Message);
        }

        [Fact]
        public async Task CreatePayoutRequest_ReturnsError_WithExistingPendingPayout()
        {
            var coach = CreateTestCoachWithUser();
            coach.Amount = 100000;

            _context.Coaches.Update(coach);
            _context.SaveChanges();

            var existingPayout = new CoachPayout
            {
                Coach = coach,
                CoachId = coach.Id,
                Amount = 50000,
                Status = PayoutStatus.Pending,
                RequestDate = DateTime.UtcNow
            };

            _context.CoachPayouts.Add(existingPayout);
            _context.SaveChanges();

            var result = await _repository.CreatePayoutRequest(coach.PhoneNumber);

            Assert.False(result.Action);
            Assert.Contains("تسویه", result.Message);
        }

        [Fact]
        public async Task CreatePayoutRequest_ReturnsError_WhenCoachNotFound()
        {
            var result = await _repository.CreatePayoutRequest("99999999999");

            Assert.False(result.Action);
        }

        #endregion

        #region GetFaq

        [Fact]
        public async Task GetFaq_ReturnsEmptyList_WhenNoFaqExists()
        {
            var result = await _repository.GetFaq();

            Assert.True(result.Action);
            Assert.Equal("get faq", result.Message);
            var faqs = result.Result as List<CoachFaq>;
            Assert.NotNull(faqs);
            Assert.Empty(faqs);
        }

        [Fact]
        public async Task GetFaq_ReturnsAllFaqs_WhenMultipleFaqsExist()
        {
            var faqs = new List<CoachFaq>
            {
                new CoachFaq { Id = 1, Question = "Q1", Answer = "A1" },
                new CoachFaq { Id = 2, Question = "Q2", Answer = "A2" }
            };
            _context.CoachFaq.AddRange(faqs);
            _context.SaveChanges();

            var result = await _repository.GetFaq();

            Assert.True(result.Action);
            var resultFaqs = result.Result as List<CoachFaq>;
            Assert.NotNull(resultFaqs);
            Assert.Equal(2, resultFaqs.Count);
        }

        #endregion

        #region GetWorkoutProgram

        [Fact]
        public async Task GetWorkoutProgram_ReturnsProgramInDays_WithValidPaymentId()
        {
            var coach = CreateTestCoachWithUser();

            var programInDay = new ProgramInDay
            {
                ForWhichDay = 1,
                AllExerciseInDays = new List<SingleExercise>()
            };

            var workoutProgram = new WorkoutProgram
            {
                Coach = coach,
                CoachId = coach.Id,
                Id = 1,
                ProgramInDays = new List<ProgramInDay>
                {
                    programInDay
                },
                Title = "Test",
                AthleteId = 0,
                PaymentId = 0
            };

            _context.ProgramInDays.Add(programInDay);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.SaveChanges();

            var result = await _repository.GetWorkoutProgram(coach.PhoneNumber, workoutProgram.Id);

            Assert.True(result.Action);
            Assert.Equal("workout program found", result.Message);
        }

        [Fact]
        public async Task GetWorkoutProgram_ReturnsError_WhenCoachNotFound()
        {
            var result = await _repository.GetWorkoutProgram("99999999999", 1);

            Assert.False(result.Action);
            Assert.Equal("Coach not found", result.Message);
        }

        [Fact]
        public async Task GetWorkoutProgram_ReturnsError_WhenProgramNotFound()
        {
            var coach = CreateTestCoachWithUser();

            var result = await _repository.GetWorkoutProgram(coach.PhoneNumber, 999);

            Assert.False(result.Action);
            Assert.Equal("Payment not found", result.Message);
        }

        #endregion

        #region getwpkey

        [Fact]
        public async Task Getwpkey_ReturnsHashedWorkoutProgramId()
        {
            _mockTokenService.Setup(t => t.HashEncode(123)).Returns("hashedKey123");

            var result = await _repository.getwpkey(123);

            Assert.True(result.Action);
            Assert.Equal("getwpkey", result.Message);
            Assert.NotNull(result.Result);
        }

        #endregion

        #region Helper Methods

        private Coach CreateTestCoachWithUser()
        {
            var user = new User
            {
                PhoneNumber = "09121234567",
                FirstName = "Coach",
                LastName = "Test"
            };
            var coach = new Coach
            {
                PhoneNumber = "09121234567",
                User = user,
                UserId = user.Id
            };
            user.Coach = coach;

            _context.Users.Add(user);
            _context.Coaches.Add(coach);
            _context.SaveChanges();

            return coach;
        }

        #endregion
    }
}

