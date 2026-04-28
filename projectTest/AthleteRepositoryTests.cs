using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Interface;
using sport_app_backend.Interface.Athlete;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Account.Athlete;
using sport_app_backend.Models.Actions;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Question.A_Question;
using sport_app_backend.Models.TrainingPlan;
using sport_app_backend.Repository.AthleteRepo;
using Xunit;

namespace sport_app_backend.projectTest
{
    public class AthleteRepositoryTests
    {
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<ICalculator> _mockCalculator;
        private readonly ApplicationDbContext _context;
        private readonly IAthleteRepository _repository;

        public AthleteRepositoryTests()
        {
            _mockTokenService = new Mock<ITokenService>();
            _mockCalculator = new Mock<ICalculator>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new AthleteRepository(_context, _mockTokenService.Object, _mockCalculator.Object);
        }

        #region GetFaq

        [Fact]
        public async Task GetFaq_ReturnsEmptyList_WhenNoFaqExists()
        {
            var result = await _repository.GetFaq();

            Assert.True(result.Action);
            Assert.Equal("get CoachFaq", result.Message);
            Assert.NotNull(result.Result);
            var faqList = result.Result as List<AthleteFaq>;
            Assert.NotNull(faqList);
            Assert.Empty(faqList);
        }

        [Fact]
        public async Task GetFaq_ReturnsAllFaqs_WhenMultipleFaqsExist()
        {
            var faqs = new List<AthleteFaq>
            {
                new AthleteFaq { Id = 1, Question = "Q1", Answer = "A1" },
                new AthleteFaq { Id = 2, Question = "Q2", Answer = "A2" }
            };
            _context.AthleteFaq.AddRange(faqs);
            await _context.SaveChangesAsync();

            var result = await _repository.GetFaq();

            Assert.True(result.Action);
            var resultFaqs = result.Result as List<AthleteFaq>;
            Assert.NotNull(resultFaqs);
            Assert.Equal(2, resultFaqs.Count);
        }

        #endregion

        #region AthleteFirstQuestions

        [Fact]
        public async Task AthleteFirstQuestions_SavesAthleteDataAndWeightEntry_WithValidInput()
        {
            var user = new User
            {
                PhoneNumber = "09121234567",
                FirstName = "John",
                LastName = "Doe",
                Athlete = new Athlete { PhoneNumber = "09121234567" }
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new AthleteFirstQuestionsDto
            {
                FirstName = "Jane",
                LastName = "Smith",
                CurrentWeight = 75,
                Height = 180
            };

            var result = await _repository.AthleteFirstQuestions("09121234567", dto);

            Assert.True(result.Action);
            Assert.Equal("Athlete first questions submitted successfully", result.Message);

            var savedUser = await _context.Users.Include(u => u.Athlete).FirstAsync();
            Assert.Equal("Jane", savedUser.FirstName);
            Assert.Equal("Smith", savedUser.LastName);
            Assert.NotNull(savedUser.Athlete);
            Assert.Equal(75, savedUser.Athlete.CurrentWeight);
            Assert.Equal(180, savedUser.Athlete.Height);

            var weightEntry = await _context.WeightEntries.FirstAsync();
            Assert.Equal(75, weightEntry.Weight);
        }

        [Fact]
        public async Task AthleteFirstQuestions_ReturnsError_WhenUserNotFound()
        {
            var dto = new AthleteFirstQuestionsDto
            {
                FirstName = "John",
                LastName = "Doe",
                CurrentWeight = 75,
                Height = 180
            };

            var result = await _repository.AthleteFirstQuestions("99999999999", dto);

            Assert.False(result.Action);
            Assert.Equal("User not found", result.Message);
        }

        [Fact]
        public async Task AthleteFirstQuestions_ReturnsError_WhenUserIsNotAthlete()
        {
            var user = new User { PhoneNumber = "09121234567", FirstName = "John" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new AthleteFirstQuestionsDto
            {
                FirstName = "Jane",
                LastName = "Smith",
                CurrentWeight = 75,
                Height = 180
            };

            var result = await _repository.AthleteFirstQuestions("09121234567", dto);

            Assert.False(result.Action);
            Assert.Equal("User is not an athlete", result.Message);
        }

        [Fact]
        public async Task AthleteFirstQuestions_UpdatesExistingAthleteData_WithNewValues()
        {
            var user = new User
            {
                PhoneNumber = "09121234567",
                FirstName = "Old",
                LastName = "Name",
            };
            var athlete = new Athlete { PhoneNumber = "09121234567", CurrentWeight = 80, Height = 170, User = user };
            user.Athlete = athlete;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new AthleteFirstQuestionsDto
            {
                FirstName = "New",
                LastName = "Updated",
                CurrentWeight = 75,
                Height = 180
            };

            var result = await _repository.AthleteFirstQuestions("09121234567", dto);

            Assert.True(result.Action);

            var updatedUser = await _context.Users.Include(u => u.Athlete).FirstAsync();
            Assert.Equal("New", updatedUser.FirstName);
            Assert.Equal("Updated", updatedUser.LastName);
            Assert.NotNull(updatedUser.Athlete);
            Assert.Equal(75, updatedUser.Athlete.CurrentWeight);
            Assert.Equal(180, updatedUser.Athlete.Height);
        }

        #endregion

        #region GetAllPayments

        [Fact]
        public async Task GetAllPayments_ReturnsEmptyList_WhenNoPaymentsExist()
        {
            var result = await _repository.GetAllPayments("09121234567");

            Assert.True(result.Action);
            Assert.Equal("No payment history found", result.Message);
            var payments = result.Result as List<AllPaymentResponseDto>;
            Assert.NotNull(payments);
            Assert.Empty(payments);
        }

        [Fact]
        public async Task GetAllPayments_ReturnsPayments_OrderedByDateDescending()
        {
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name" };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            var coachService = new CoachService 
            { 
                Title = "Service 1", 
                Coach = coach,
                CoachId = coach.Id,
                Description = "Test Service",
                Price = 100000
            };
            var payment1 = new Payment
            {
                Amount = 1000,
                PaymentDate = DateTime.Now.AddDays(-5),
                PaymentStatus = PaymentStatus.SUCCESS,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Coach = coach,
                CoachId = coach.Id,
                CoachService = coachService,
                CoachServiceId = coachService.Id
            };
            var payment2 = new Payment
            {
                Amount = 2000,
                PaymentDate = DateTime.Now,
                PaymentStatus = PaymentStatus.SUCCESS,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Coach = coach,
                CoachId = coach.Id,
                CoachService = coachService,
                CoachServiceId = coachService.Id
            };
            var workoutProgram1 = new WorkoutProgram
            {
                Status = WorkoutProgramStatus.ACTIVE,
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Payment = payment1,
                PaymentId = payment1.Id
            };
            var workoutProgram2 = new WorkoutProgram
            {
                Status = WorkoutProgramStatus.ACTIVE,
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Payment = payment2,
                PaymentId = payment2.Id
            };

            _context.Users.Add(coachUser);
            _context.Coaches.Add(coach);
            _context.Athletes.Add(athlete);
            _context.CoachServices.Add(coachService);
            _context.Payments.AddRange(payment1, payment2);
            _context.WorkoutPrograms.AddRange(workoutProgram1, workoutProgram2);
            await _context.SaveChangesAsync();

            _mockTokenService.Setup(ts => ts.HashEncode(It.IsAny<int>())).Returns("encoded");

            var result = await _repository.GetAllPayments("09121234567");

            Assert.True(result.Action);
            var payments = result.Result as List<AllPaymentResponseDto>;
            Assert.NotNull(payments);
            Assert.Equal(2, payments.Count);
            Assert.Equal("2000", payments[0].Amount);
            Assert.Equal("1000", payments[1].Amount);
        }

        [Fact]
        public async Task GetAllPayments_ExcludesRefundedPayments()
        {
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name" };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            var coachService = new CoachService 
            { 
                Title = "Service 1",
                Coach = coach,
                CoachId = coach.Id,
                Description = "Test Service",
                Price = 100000
            };
            var payment = new Payment
            {
                Amount = 1000,
                PaymentDate = DateTime.Now,
                PaymentStatus = PaymentStatus.SUCCESS,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Coach = coach,
                CoachId = coach.Id,
                CoachService = coachService,
                CoachServiceId = coachService.Id
            };
            var workoutProgram = new WorkoutProgram
            {
                Status = WorkoutProgramStatus.REFUND,
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Payment = payment,
                PaymentId = payment.Id
            };

            _context.Users.Add(coachUser);
            _context.Coaches.Add(coach);
            _context.Athletes.Add(athlete);
            _context.CoachServices.Add(coachService);
            _context.Payments.Add(payment);
            _context.WorkoutPrograms.Add(workoutProgram);
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllPayments("09121234567");

            Assert.True(result.Action);
            var payments = result.Result as List<AllPaymentResponseDto>;
            Assert.NotNull(payments);
            Assert.Empty(payments);
        }

        #endregion

        #region GetPayment

        [Fact]
        public async Task GetPayment_ReturnsPaymentDetails_WithValidPaymentId()
        {
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name", Gender = Gender.MALE, BirthDate = new DateTime(1990, 1, 1) };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var athleteUser = new User { PhoneNumber = "09121234567", FirstName = "Athlete", LastName = "User", Gender = Gender.MALE, BirthDate = new DateTime(1995, 1, 1) };
            var athlete = new Athlete { PhoneNumber = "09121234567", Height = 180, CurrentWeight = 75, User = athleteUser };
            athleteUser.Athlete = athlete;
            var coachService = new CoachService { Title = "Service 1", Coach = coach, CoachId = coach.Id, Description = "Test", Price = 1000 };
            var athleteQuestion = new AthleteQuestion
            {
                ActivityLevel = ActivityLevel.Moderate,
                DaysPerWeekToExercise = 3,
                InjuryArea = new InjuryArea()
                {
                    None = true
                },
                ExerciseLocation = ExerciseLocation.HOME
            };
            var payment = new Payment
            {
                Id = 1,
                Amount = 1000,
                PaymentDate = DateTime.Now,
                PaymentStatus = PaymentStatus.SUCCESS,
                Authority = "auth123",
                Athlete = athlete,
                AthleteId = athlete.Id,
                Coach = coach,
                CoachId = coach.Id,
                CoachService = coachService,
                CoachServiceId = coachService.Id,
                AthleteQuestion = athleteQuestion,
                AthleteQuestionId = athleteQuestion.Id
            };
            var programInDay = new ProgramInDay { AllExerciseInDays = new List<SingleExercise>() };
            var workoutProgram = new WorkoutProgram
            {
                Status = WorkoutProgramStatus.ACTIVE,
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Payment = payment,
                PaymentId = payment.Id,
                ProgramInDays = new List<ProgramInDay> { programInDay }
            };

            _context.Users.AddRange(coachUser, athleteUser);
            _context.Coaches.Add(coach);
            _context.Athletes.Add(athlete);
            _context.CoachServices.Add(coachService);
            _context.AthleteQuestions.Add(athleteQuestion);
            _context.Payments.Add(payment);
            _context.ProgramInDays.Add(programInDay);
            _context.WorkoutPrograms.Add(workoutProgram);
            await _context.SaveChangesAsync();

            _mockTokenService.Setup(ts => ts.HashEncode(It.IsAny<int>())).Returns("encoded");
            _mockCalculator.Setup(c => c.BmrCalculator(It.IsAny<BmrRequestDto>())).Returns(2000);

            var result = await _repository.GetPayment("09121234567", 1);

            Assert.True(result.Action);
            Assert.Equal("Payment details found", result.Message);
            var paymentDto = result.Result as AthletePaymentResponseDto;
            Assert.NotNull(paymentDto);
            Assert.Equal(1, paymentDto.PaymentId);
        }

        [Fact]
        public async Task GetPayment_ReturnsError_WhenPaymentNotFound()
        {
            var result = await _repository.GetPayment("09121234567", 999);

            Assert.False(result.Action);
            Assert.Equal("Payment not found for this user", result.Message);
        }

        [Fact]
        public async Task GetPayment_ReturnsError_WhenPaymentBelongsToAnotherUser()
        {
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name" };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var athlete = new Athlete { PhoneNumber = "09189999999" };
            var coachService = new CoachService { Title = "Service 1", Coach = coach, CoachId = coach.Id, Description = "Test", Price = 1000 };
            var payment = new Payment
            {
                Id = 1,
                Amount = 1000,
                PaymentDate = DateTime.Now,
                PaymentStatus = PaymentStatus.SUCCESS,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Coach = coach,
                CoachId = coach.Id,
                CoachService = coachService,
                CoachServiceId = coachService.Id
            };

            _context.Users.Add(coachUser);
            _context.Coaches.Add(coach);
            _context.Athletes.Add(athlete);
            _context.CoachServices.Add(coachService);
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            var result = await _repository.GetPayment("09121234567", 1);

            Assert.False(result.Action);
            Assert.Equal("Payment not found for this user", result.Message);
        }

        #endregion

        #region ActiveProgram

        [Fact]
        public async Task ActiveProgram_ActivatesProgram_WhenProgramIsNotActive()
        {
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name" };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            var athleteQuestion = new AthleteQuestion
            {
                ActivityLevel = ActivityLevel.Moderate,
                DaysPerWeekToExercise = 3,
                InjuryArea = new InjuryArea()
                {
                    None = true
                },
                ExerciseLocation = ExerciseLocation.GYM
            };
            var payment = new Payment { Id = 1, Athlete = athlete, AthleteId = athlete.Id, Coach = coach, CoachId = coach.Id, CoachService = new CoachService { Title = "S", Coach = coach, CoachId = coach.Id, Description = "D", Price = 1 }, CoachServiceId = 1, AthleteQuestion = athleteQuestion, AthleteQuestionId = athleteQuestion.Id };
            var programInDay = new ProgramInDay { AllExerciseInDays = new List<SingleExercise>() };
            var workoutProgram = new WorkoutProgram
            {
                Status = WorkoutProgramStatus.NOTACTIVE,
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                PaymentId = 1,
                Payment = payment,
                ProgramDuration = 4,
                ProgramInDays = new List<ProgramInDay> { programInDay }
            };

            _context.Users.Add(coachUser);
            _context.Coaches.Add(coach);
            _context.Athletes.Add(athlete);
            _context.AthleteQuestions.Add(athleteQuestion);
            _context.Payments.Add(payment);
            _context.ProgramInDays.Add(programInDay);
            _context.WorkoutPrograms.Add(workoutProgram);
            await _context.SaveChangesAsync();

            var result = await _repository.ActiveProgram("09121234567", 1);

            Assert.True(result.Action);
            var updatedProgram = await _context.WorkoutPrograms.FirstAsync();
            Assert.Equal(WorkoutProgramStatus.ACTIVE, updatedProgram.Status);
        }

        [Fact]
        public async Task ActiveProgram_ReturnsError_WhenAthleteNotFound()
        {
            var result = await _repository.ActiveProgram("99999999999", 1);

            Assert.False(result.Action);
            Assert.Equal("Athlete not found", result.Message);
        }

        [Fact]
        public async Task ActiveProgram_ReturnsError_WhenWorkoutProgramNotFound()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            _context.Athletes.Add(athlete);
            await _context.SaveChangesAsync();

            var result = await _repository.ActiveProgram("09121234567", 999);

            Assert.False(result.Action);
            Assert.Equal("Athlete not found", result.Message);
        }

        #endregion

        #region ExerciseFeedBack

        [Fact]
        public async Task ExerciseFeedBack_SavesFeedback_WithValidInput()
        {
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name" };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            var programInDay = new ProgramInDay { AllExerciseInDays = new List<SingleExercise>() };
            var singleExercise = new SingleExercise { Id = 1, ProgramInDay = programInDay, Reps = new List<int> { 10 }, RepType = RepType.Count, Description = "Test" };
            programInDay.AllExerciseInDays.Add(singleExercise);
            var workoutProgram = new WorkoutProgram
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                ProgramInDays = new List<ProgramInDay> { programInDay },
                Payment = new Payment { Athlete = athlete, AthleteId = athlete.Id, Coach = coach, CoachId = coach.Id, CoachService = new CoachService { Title = "S", Coach = coach, CoachId = coach.Id, Description = "D", Price = 1 }, CoachServiceId = 1 },
                PaymentId = 1
            };
            var trainingSession = new TrainingSession
            {
                Id = 1,
                WorkoutProgram = workoutProgram,
                WorkoutProgramId = workoutProgram.Id,
                ProgramInDay = programInDay,
                ProgramInDayId = programInDay.Id,
                ExerciseCompletionBitmap = new byte[1]
            };

            _context.Users.Add(coachUser);
            _context.Coaches.Add(coach);
            _context.Athletes.Add(athlete);
            _context.ProgramInDays.Add(programInDay);
            _context.SingleExercises.Add(singleExercise);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.TrainingSessions.Add(trainingSession);
            await _context.SaveChangesAsync();

            var dto = new ExerciseFeedbackDto
            {
                TrainingSessionId = 1,
                SingleExerciseId = 1,
                NegativeReason = "TooHard"
            };

            var result = await _repository.ExerciseFeedBack("09121234567", dto);

            Assert.True(result.Action);
            Assert.Equal("فیدبک شما با موفقیت ثبت شد.", result.Message);
            var savedFeedback = await _context.ExerciseFeedbacks.FirstAsync();
            Assert.NotNull(savedFeedback);
        }

        [Fact]
        public async Task ExerciseFeedBack_ReturnsError_WhenAthleteNotFound()
        {
            var dto = new ExerciseFeedbackDto
            {
                TrainingSessionId = 1,
                SingleExerciseId = 1,
                NegativeReason = "Good"
            };

            var result = await _repository.ExerciseFeedBack("99999999999", dto);

            Assert.False(result.Action);
            Assert.Equal("ورزشکار یافت نشد!", result.Message);
        }

        [Fact]
        public async Task ExerciseFeedBack_ReturnsError_WhenTrainingSessionNotFound()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            _context.Athletes.Add(athlete);
            await _context.SaveChangesAsync();

            var dto = new ExerciseFeedbackDto
            {
                TrainingSessionId = 999,
                SingleExerciseId = 1,
                NegativeReason = "Good"
            };

            var result = await _repository.ExerciseFeedBack("09121234567", dto);

            Assert.False(result.Action);
            Assert.Equal("جلسه تمرینی یافت نشد!", result.Message);
        }

        #endregion

        #region ChangeExercise

        [Fact]
        public async Task ChangeExercise_SavesChangeRequest_WithValidInput()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name" };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var programInDay = new ProgramInDay();
            var workoutProgram = new WorkoutProgram { Coach = coach, CoachId = coach.Id, Athlete = athlete, AthleteId = athlete.Id, Payment = new Payment { Athlete = athlete, AthleteId = athlete.Id, Coach = coach, CoachId = coach.Id, CoachService = new CoachService { Title = "S", Coach = coach, CoachId = coach.Id, Description = "D", Price = 1 }, CoachServiceId = 1 }, PaymentId = 1, ProgramInDays = new List<ProgramInDay> { programInDay } };
            var singleExercise = new SingleExercise { Id = 1, ProgramInDay = programInDay, Reps = new List<int> { 10 }, RepType = RepType.Count, Description = "Test" };

            _context.Athletes.Add(athlete);
            _context.Users.Add(coachUser);
            _context.Coaches.Add(coach);
            _context.ProgramInDays.Add(programInDay);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.SingleExercises.Add(singleExercise);
            await _context.SaveChangesAsync();

            var dto = new ExerciseChangeDto
            {
                SingleExerciseId = 1,
                TrainingSessionId = 1,
                Reason = "NeedSimplerAlternative"
            };

            var result = await _repository.ChangeExercise("09121234567", dto);

            Assert.True(result.Action);
            Assert.Equal("Exercise change request saved.", result.Message);
            var changeRequest = await _context.ExerciseChangeRequests.FirstAsync();
            Assert.NotNull(changeRequest);
            Assert.Equal("09121234567", changeRequest.Athlete.PhoneNumber);
        }

        [Fact]
        public async Task ChangeExercise_ReturnsError_WhenAthleteNotFound()
        {
            var dto = new ExerciseChangeDto
            {
                SingleExerciseId = 1,
                TrainingSessionId = 1,
                Reason = "Too hard"
            };

            var result = await _repository.ChangeExercise("99999999999", dto);

            Assert.False(result.Action);
            Assert.Equal("Athlete not found", result.Message);
        }

        #endregion

        #region GetAllTrainingSession

        [Fact]
        public async Task GetAllTrainingSession_ReturnsTrainingSessions_WhenActiveProgramExists()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name" };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var programInDay = new ProgramInDay { AllExerciseInDays = new List<SingleExercise>() };
            var payment = new Payment { Athlete = athlete, AthleteId = athlete.Id, Coach = coach, CoachId = coach.Id, CoachService = new CoachService { Title = "S", Coach = coach, CoachId = coach.Id, Description = "D", Price = 1 }, CoachServiceId = 1 };
            var workoutProgram = new WorkoutProgram
            {
                Title = "My Program",
                Status = WorkoutProgramStatus.ACTIVE,
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Payment = payment,
                PaymentId = payment.Id,
                ProgramInDays = new List<ProgramInDay> { programInDay },
                TrainingSessions = new List<TrainingSession>()
            };
            var trainingSession = new TrainingSession
            {
                DayNumber = 1,
                TrainingSessionStatus = TrainingSessionStatus.NOTSTARTED,
                ExerciseCompletionBitmap = new byte[3],
                WorkoutProgram = workoutProgram,
                WorkoutProgramId = workoutProgram.Id,
                ProgramInDay = programInDay,
                ProgramInDayId = programInDay.Id
            };
            workoutProgram.TrainingSessions!.Add(trainingSession);

            _context.Athletes.Add(athlete);
            _context.Users.Add(coachUser);
            _context.Coaches.Add(coach);
            _context.ProgramInDays.Add(programInDay);
            _context.Payments.Add(payment);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.TrainingSessions.Add(trainingSession);
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllTrainingSession("09121234567");

            Assert.True(result.Action);
            Assert.Equal("Training sessions retrieved successfully", result.Message);
            Assert.NotNull(result.Result);
        }

        [Fact]
        public async Task GetAllTrainingSession_ReturnsNull_WhenNoActiveProgramExists()
        {
            var result = await _repository.GetAllTrainingSession("09121234567");

            Assert.True(result.Action);
            Assert.Null(result.Result);
        }

        #endregion

        #region GetTrainingSession

        [Fact]
        public async Task GetTrainingSession_ReturnsSessionDetails_WithValidInput()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567", CurrentWeight = 75 };
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name" };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var exercise = new Exercise { Id = 1, Met = 5.0, EnglishName = "Push up", PersianName = "شنه سینه", Description = "Test" };
            var singleExercise = new SingleExercise
            {
                Id = 1,
                Exercise = exercise,
                ExerciseId = exercise.Id,
                Reps = new List<int> { 10, 10, 10 },
                RepType = RepType.Count,
                Description = "Test"
            };
            var programInDay = new ProgramInDay
            {
                AllExerciseInDays = new List<SingleExercise> { singleExercise }
            };
            var payment = new Payment { Athlete = athlete, AthleteId = athlete.Id, Coach = coach, CoachId = coach.Id, CoachService = new CoachService { Title = "S", Coach = coach, CoachId = coach.Id, Description = "D", Price = 1 }, CoachServiceId = 1 };
            var workoutProgram = new WorkoutProgram
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Payment = payment,
                PaymentId = payment.Id,
                ProgramInDays = new List<ProgramInDay> { programInDay },
                ProgramPriorities = new List<ProgramPriority>() { ProgramPriority.RECOVERY }
            };
            var trainingSession = new TrainingSession
            {
                Id = 1,
                WorkoutProgram = workoutProgram,
                WorkoutProgramId = workoutProgram.Id,
                ProgramInDay = programInDay,
                ProgramInDayId = programInDay.Id,
                ExerciseCompletionBitmap = new byte[1]
            };

            _context.Athletes.Add(athlete);
            _context.Users.Add(coachUser);
            _context.Coaches.Add(coach);
            _context.Exercises.Add(exercise);
            _context.SingleExercises.Add(singleExercise);
            _context.ProgramInDays.Add(programInDay);
            _context.Payments.Add(payment);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.TrainingSessions.Add(trainingSession);
            await _context.SaveChangesAsync();

            var result = await _repository.GetTrainingSession("09121234567", 1);

            Assert.True(result.Action);
            Assert.Equal("get TrainingSession", result.Message);
            Assert.NotNull(result.Result);
        }

        [Fact]
        public async Task GetTrainingSession_ReturnsError_WhenAthleteNotFound()
        {
            var result = await _repository.GetTrainingSession("99999999999", 1);

            Assert.False(result.Action);
            Assert.Equal("Athlete not found", result.Message);
        }

        [Fact]
        public async Task GetTrainingSession_ReturnsError_WhenSessionNotFound()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            _context.Athletes.Add(athlete);
            await _context.SaveChangesAsync();

            var result = await _repository.GetTrainingSession("09121234567", 999);

            Assert.False(result.Action);
            Assert.Equal("trainingSession not found", result.Message);
        }

        #endregion

        #region DoTrainingSession

        [Fact]
        public async Task DoTrainingSession_MarksExerciseAsCompleted_AndUpdatesSessionStatus()
        {
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name" };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            var payment = new Payment { Athlete = athlete, AthleteId = athlete.Id, Coach = coach, CoachId = coach.Id, CoachService = new CoachService { Title = "S", Coach = coach, CoachId = coach.Id, Description = "D", Price = 1 }, CoachServiceId = 1 };
            var programInDay = new ProgramInDay { AllExerciseInDays = new List<SingleExercise>() };
            var workoutProgram = new WorkoutProgram { Coach = coach, CoachId = coach.Id, Athlete = athlete, AthleteId = athlete.Id, Payment = payment, PaymentId = payment.Id, ProgramInDays = new List<ProgramInDay> { programInDay } };
            var trainingSession = new TrainingSession
            {
                Id = 1,
                ExerciseCompletionBitmap = new byte[3],
                TrainingSessionStatus = TrainingSessionStatus.NOTSTARTED,
                WorkoutProgram = workoutProgram,
                WorkoutProgramId = workoutProgram.Id,
                ProgramInDay = programInDay,
                ProgramInDayId = programInDay.Id
            };

            _context.Users.Add(coachUser);
            _context.Coaches.Add(coach);
            _context.Athletes.Add(athlete);
            _context.Payments.Add(payment);
            _context.ProgramInDays.Add(programInDay);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.TrainingSessions.Add(trainingSession);
            await _context.SaveChangesAsync();

            var result = await _repository.DoTrainingSession("09121234567", 1, 0);

            Assert.True(result.Action);
            Assert.Equal("Do Training session", result.Message);

            var updatedSession = await _context.TrainingSessions.FirstAsync();
            Assert.Equal(TrainingSessionStatus.INPROGRESS, updatedSession.TrainingSessionStatus);
            Assert.Equal(0xFF, updatedSession.ExerciseCompletionBitmap[0]);
        }

        [Fact]
        public async Task DoTrainingSession_ReturnsError_WhenSessionNotFound()
        {
            var result = await _repository.DoTrainingSession("09121234567", 999, 0);

            Assert.False(result.Action);
            Assert.Equal("trainingSession not found", result.Message);
        }

        #endregion

        #region FinishTrainingSession

        [Fact]
        public async Task FinishTrainingSession_CreatesActivityAndUpdatesProgram_WithValidInput()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567", CurrentWeight = 75 };
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name" };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var programInDay = new ProgramInDay { AllExerciseInDays = new List<SingleExercise>() };
            var payment = new Payment { Athlete = athlete, AthleteId = athlete.Id, Coach = coach, CoachId = coach.Id, CoachService = new CoachService { Title = "S", Coach = coach, CoachId = coach.Id, Description = "D", Price = 1 }, CoachServiceId = 1 };
            var workoutProgram = new WorkoutProgram
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                CompletedSessionCount = 0,
                Payment = payment,
                PaymentId = payment.Id,
                ProgramInDays = new List<ProgramInDay> { programInDay }
            };
            var trainingSession = new TrainingSession
            {
                Id = 1,
                WorkoutProgram = workoutProgram,
                WorkoutProgramId = workoutProgram.Id,
                ProgramInDay = programInDay,
                ProgramInDayId = programInDay.Id,
                TrainingSessionStatus = TrainingSessionStatus.INPROGRESS,
                ExerciseCompletionBitmap = new byte[1]
            };

            _context.Athletes.Add(athlete);
            _context.Users.Add(coachUser);
            _context.Coaches.Add(coach);
            _context.ProgramInDays.Add(programInDay);
            _context.Payments.Add(payment);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.TrainingSessions.Add(trainingSession);
            await _context.SaveChangesAsync();

            var dto = new FinishTrainingSessionDto
            {
                TrainingSessionId = 1,
                Duration = 60,
                CaloriesLost = 500,
                TrainingSessionName = "Chest Day"
            };

            var result = await _repository.FinishTrainingSession("09121234567", dto);

            Assert.True(result.Action);
            Assert.Equal("Finish Training session", result.Message);

            var activity = await _context.Activities.FirstAsync();
            Assert.Equal(60, activity.Duration);
            Assert.Equal(500, activity.CaloriesLost);

            var updatedSession = await _context.TrainingSessions.FirstAsync();
            Assert.Equal(TrainingSessionStatus.COMPLETED, updatedSession.TrainingSessionStatus);

            var updatedProgram = await _context.WorkoutPrograms.FirstAsync();
            Assert.Equal(1, updatedProgram.CompletedSessionCount);
        }

        [Fact]
        public async Task FinishTrainingSession_ReturnsError_WhenAthleteNotFound()
        {
            var dto = new FinishTrainingSessionDto
            {
                TrainingSessionId = 1,
                Duration = 60,
                CaloriesLost = 500,
                TrainingSessionName = "Chest Day"
            };

            var result = await _repository.FinishTrainingSession("99999999999", dto);

            Assert.False(result.Action);
            Assert.Equal("Athlete not found", result.Message);
        }

        [Fact]
        public async Task FinishTrainingSession_ReturnsError_WhenSessionNotFound()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            _context.Athletes.Add(athlete);
            await _context.SaveChangesAsync();

            var dto = new FinishTrainingSessionDto
            {
                TrainingSessionId = 999,
                Duration = 60,
                CaloriesLost = 500,
                TrainingSessionName = "Chest Day"
            };

            var result = await _repository.FinishTrainingSession("09121234567", dto);

            Assert.False(result.Action);
            Assert.Equal("trainingSession not found", result.Message);
        }

        #endregion

        #region FeedbackTrainingSession

        [Fact]
        public async Task FeedbackTrainingSession_SavesFeedback_WhenSessionCompleted()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name" };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var programInDay = new ProgramInDay { AllExerciseInDays = new List<SingleExercise>() };
            var payment = new Payment { Athlete = athlete, AthleteId = athlete.Id, Coach = coach, CoachId = coach.Id, CoachService = new CoachService { Title = "S", Coach = coach, CoachId = coach.Id, Description = "D", Price = 1 }, CoachServiceId = 1 };
            var workoutProgram = new WorkoutProgram { Coach = coach, CoachId = coach.Id, Athlete = athlete, AthleteId = athlete.Id, Payment = payment, PaymentId = payment.Id, ProgramInDays = new List<ProgramInDay> { programInDay } };
            var trainingSession = new TrainingSession
            {
                Id = 1,
                WorkoutProgram = workoutProgram,
                WorkoutProgramId = workoutProgram.Id,
                ProgramInDay = programInDay,
                ProgramInDayId = programInDay.Id,
                TrainingSessionStatus = TrainingSessionStatus.COMPLETED,
                ExerciseFeeling = ExerciseFeeling.Good,
                ExerciseCompletionBitmap = new byte[1]
            };

            _context.Athletes.Add(athlete);
            _context.Users.Add(coachUser);
            _context.Coaches.Add(coach);
            _context.ProgramInDays.Add(programInDay);
            _context.Payments.Add(payment);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.TrainingSessions.Add(trainingSession);
            await _context.SaveChangesAsync();

            var dto = new FeedbackTrainingSessionDto
            {
                TrainingSessionId = 1,
                ExerciseFeeling = "Good"
            };

            var result = await _repository.FeedbackTrainingSession("09121234567", dto);

            Assert.True(result.Action);
            Assert.Equal("Feedback Training session", result.Message);

            var updatedSession = await _context.TrainingSessions.FirstAsync();
            Assert.Equal(ExerciseFeeling.Good, updatedSession.ExerciseFeeling);
        }

        [Fact]
        public async Task FeedbackTrainingSession_ReturnsError_WhenAthleteNotFound()
        {
            var dto = new FeedbackTrainingSessionDto
            {
                TrainingSessionId = 1,
                ExerciseFeeling = "GOOD"
            };

            var result = await _repository.FeedbackTrainingSession("99999999999", dto);

            Assert.False(result.Action);
            Assert.Equal("Athlete not found", result.Message);
        }

        [Fact]
        public async Task FeedbackTrainingSession_ReturnsError_WhenSessionNotFound()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            _context.Athletes.Add(athlete);
            await _context.SaveChangesAsync();

            var dto = new FeedbackTrainingSessionDto
            {
                TrainingSessionId = 999,
                ExerciseFeeling = "GOOD"
            };

            var result = await _repository.FeedbackTrainingSession("09121234567", dto);

            Assert.False(result.Action);
            Assert.Equal("trainingSession not found", result.Message);
        }

        [Fact]
        public async Task FeedbackTrainingSession_ReturnsError_WhenSessionNotCompleted()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name" };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var programInDay = new ProgramInDay { AllExerciseInDays = new List<SingleExercise>() };
            var payment = new Payment { Athlete = athlete, AthleteId = athlete.Id, Coach = coach, CoachId = coach.Id, CoachService = new CoachService { Title = "S", Coach = coach, CoachId = coach.Id, Description = "D", Price = 1 }, CoachServiceId = 1 };
            var workoutProgram = new WorkoutProgram { Coach = coach, CoachId = coach.Id, Athlete = athlete, AthleteId = athlete.Id, Payment = payment, PaymentId = payment.Id, ProgramInDays = new List<ProgramInDay> { programInDay } };
            var trainingSession = new TrainingSession
            {
                Id = 1,
                WorkoutProgram = workoutProgram,
                WorkoutProgramId = workoutProgram.Id,
                ProgramInDay = programInDay,
                ProgramInDayId = programInDay.Id,
                TrainingSessionStatus = TrainingSessionStatus.INPROGRESS,
                ExerciseCompletionBitmap = new byte[1]
            };

            _context.Athletes.Add(athlete);
            _context.Users.Add(coachUser);
            _context.Coaches.Add(coach);
            _context.ProgramInDays.Add(programInDay);
            _context.Payments.Add(payment);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.TrainingSessions.Add(trainingSession);
            await _context.SaveChangesAsync();

            var dto = new FeedbackTrainingSessionDto
            {
                TrainingSessionId = 1,
                ExerciseFeeling = "GOOD"
            };

            var result = await _repository.FeedbackTrainingSession("09121234567", dto);

            Assert.False(result.Action);
            Assert.Equal("trainingSession not completed", result.Message);
        }

        #endregion

        #region ResetTrainingSession

        [Fact]
        public async Task ResetTrainingSession_ResetsSessionToNotStarted_WithValidInput()
        {
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name" };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            var programInDay = new ProgramInDay { AllExerciseInDays = new List<SingleExercise>() };
            var payment = new Payment { Athlete = athlete, AthleteId = athlete.Id, Coach = coach, CoachId = coach.Id, CoachService = new CoachService { Title = "S", Coach = coach, CoachId = coach.Id, Description = "D", Price = 1 }, CoachServiceId = 1 };
            var workoutProgram = new WorkoutProgram { Coach = coach, CoachId = coach.Id, Athlete = athlete, AthleteId = athlete.Id, Payment = payment, PaymentId = payment.Id, ProgramInDays = new List<ProgramInDay> { programInDay } };
            var trainingSession = new TrainingSession
            {
                Id = 1,
                TrainingSessionStatus = TrainingSessionStatus.COMPLETED,
                ExerciseCompletionBitmap = new byte[] { 0xFF, 0xFF, 0xFF },
                WorkoutProgram = workoutProgram,
                WorkoutProgramId = workoutProgram.Id,
                ProgramInDay = programInDay,
                ProgramInDayId = programInDay.Id
            };

            _context.Users.Add(coachUser);
            _context.Coaches.Add(coach);
            _context.Athletes.Add(athlete);
            _context.ProgramInDays.Add(programInDay);
            _context.Payments.Add(payment);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.TrainingSessions.Add(trainingSession);
            await _context.SaveChangesAsync();

            var result = await _repository.ResetTrainingSession("09121234567", 1);

            Assert.True(result.Action);
            Assert.Equal("Training session Retested", result.Message);

            var updatedSession = await _context.TrainingSessions.FirstAsync();
            Assert.Equal(TrainingSessionStatus.NOTSTARTED, updatedSession.TrainingSessionStatus);
            Assert.All(updatedSession.ExerciseCompletionBitmap, b => Assert.Equal(0, b));
        }

        [Fact]
        public async Task ResetTrainingSession_ReturnsError_WhenSessionNotFound()
        {
            var result = await _repository.ResetTrainingSession("09121234567", 999);

            Assert.False(result.Action);
            Assert.Equal("trainingSession not found", result.Message);
        }

        #endregion

        #region CalculateCalories

        [Fact]
        public async Task CalculateCalories_CalculatesCorrectly_ForCompletedExercises()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567", CurrentWeight = 75 };
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name" };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var exercise = new Exercise { Id = 1, Met = 5.0, EnglishName = "Push up", PersianName = "شنه سینه", Description = "Test" };
            var singleExercise = new SingleExercise
            {
                Id = 1,
                Exercise = exercise,
                ExerciseId = exercise.Id,
                Reps = new List<int> { 10, 10 },
                RepType = RepType.Count,
                Description = "Test"
            };
            var programInDay = new ProgramInDay
            {
                AllExerciseInDays = new List<SingleExercise> { singleExercise }
            };
            var payment = new Payment { Athlete = athlete, AthleteId = athlete.Id, Coach = coach, CoachId = coach.Id, CoachService = new CoachService { Title = "S", Coach = coach, CoachId = coach.Id, Description = "D", Price = 1 }, CoachServiceId = 1 };
            var workoutProgram = new WorkoutProgram
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Payment = payment,
                PaymentId = payment.Id,
                ProgramInDays = new List<ProgramInDay> { programInDay },
                ProgramPriorities = new List<ProgramPriority>() { ProgramPriority.RECOVERY }
            };
            var trainingSession = new TrainingSession
            {
                Id = 1,
                WorkoutProgram = workoutProgram,
                WorkoutProgramId = workoutProgram.Id,
                ProgramInDay = programInDay,
                ProgramInDayId = programInDay.Id,
                ExerciseCompletionBitmap = new byte[] { 0xFF }
            };

            _context.Athletes.Add(athlete);
            _context.Users.Add(coachUser);
            _context.Coaches.Add(coach);
            _context.Exercises.Add(exercise);
            _context.SingleExercises.Add(singleExercise);
            _context.ProgramInDays.Add(programInDay);
            _context.Payments.Add(payment);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.TrainingSessions.Add(trainingSession);
            await _context.SaveChangesAsync();

            var result = await _repository.CalculateCalories("09121234567", 1);

            Assert.True(result.Action);
            Assert.Equal("Calories calculated successfully for completed exercises.", result.Message);
            Assert.NotNull(result.Result);
            var calories = (double)result.Result;
            Assert.True(calories >= 0);
        }

        [Fact]
        public async Task CalculateCalories_ReturnsZero_WhenNoExercisesCompleted()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567", CurrentWeight = 75 };
            var coachUser = new User { PhoneNumber = "09129999999", FirstName = "Coach", LastName = "Name" };
            var coach = new Coach { PhoneNumber = "09129999999", User = coachUser };
            var exercise = new Exercise { Id = 1, Met = 5.0, EnglishName = "Push up", PersianName = "شنه سینه", Description = "Test" };
            var singleExercise = new SingleExercise
            {
                Id = 1,
                Exercise = exercise,
                ExerciseId = exercise.Id,
                Reps = new List<int> { 10 },
                RepType = RepType.Count,
                Description = "Test"
            };
            var programInDay = new ProgramInDay
            {
                AllExerciseInDays = new List<SingleExercise> { singleExercise }
            };
            var payment = new Payment { Athlete = athlete, AthleteId = athlete.Id, Coach = coach, CoachId = coach.Id, CoachService = new CoachService { Title = "S", Coach = coach, CoachId = coach.Id, Description = "D", Price = 1 }, CoachServiceId = 1 };
            var workoutProgram = new WorkoutProgram
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Payment = payment,
                PaymentId = payment.Id,
                ProgramInDays = new List<ProgramInDay> { programInDay },
                ProgramPriorities = new List<ProgramPriority>() { ProgramPriority.RECOVERY }
            };
            var trainingSession = new TrainingSession
            {
                Id = 1,
                WorkoutProgram = workoutProgram,
                WorkoutProgramId = workoutProgram.Id,
                ProgramInDay = programInDay,
                ProgramInDayId = programInDay.Id,
                ExerciseCompletionBitmap = new byte[1]
            };

            _context.Athletes.Add(athlete);
            _context.Users.Add(coachUser);
            _context.Coaches.Add(coach);
            _context.Exercises.Add(exercise);
            _context.SingleExercises.Add(singleExercise);
            _context.ProgramInDays.Add(programInDay);
            _context.Payments.Add(payment);
            _context.WorkoutPrograms.Add(workoutProgram);
            _context.TrainingSessions.Add(trainingSession);
            await _context.SaveChangesAsync();

            var result = await _repository.CalculateCalories("09121234567", 1);

            Assert.True(result.Action);
            var calories = (double)result.Result;
            Assert.Equal(0.0, calories);
        }

        [Fact]
        public async Task CalculateCalories_ReturnsError_WhenAthleteNotFound()
        {
            var result = await _repository.CalculateCalories("99999999999", 1);

            Assert.False(result.Action);
            Assert.Equal("Athlete not found", result.Message);
        }

        [Fact]
        public async Task CalculateCalories_ReturnsError_WhenSessionNotFound()
        {
            var athlete = new Athlete { PhoneNumber = "09121234567" };
            _context.Athletes.Add(athlete);
            await _context.SaveChangesAsync();

            var result = await _repository.CalculateCalories("09121234567", 999);

            Assert.False(result.Action);
            Assert.Equal("trainingSession not found", result.Message);
        }

        #endregion
    }
}

