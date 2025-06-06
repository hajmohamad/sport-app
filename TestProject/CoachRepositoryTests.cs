using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.C_Question;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Question.A_Question;
using sport_app_backend.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace TestProject
{
    public class CoachRepositoryTests
    {
        #region Helper Methods

        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var dbContext = new ApplicationDbContext(options);
            dbContext.Database.EnsureCreated();
            return dbContext;
        }

        private async Task<Coach> CreateAndAddCoach(ApplicationDbContext context, string phoneNumber, string firstName = "Test", string lastName = "Coach")
        {
            var user = new User
            {
                PhoneNumber = phoneNumber,
                FirstName = firstName,
                LastName = lastName,
                TypeOfUser = TypeOfUser.COACH
            };
            var coach = new Coach
            {
                User = user,
                PhoneNumber = phoneNumber,
            };
            user.Coach = coach;

            await context.Users.AddAsync(user);
            await context.Coaches.AddAsync(coach);
            await context.SaveChangesAsync();
            return coach;
        }

        // متد کمکی برای ساخت و افزودن یک ورزشکار (Athlete) به دیتابیس
        private async Task<Athlete> CreateAndAddAthlete(ApplicationDbContext context, string phoneNumber, string firstName = "Test", string lastName = "Athlete")
        {
            var user = new User
            {
                PhoneNumber = phoneNumber,
                FirstName = firstName,
                LastName = lastName,
                TypeOfUser = TypeOfUser.ATHLETE
            };
            var athlete = new Athlete
            {
                User = user,
                PhoneNumber = phoneNumber
            };
            user.Athlete = athlete;

            await context.Users.AddAsync(user);
            await context.Athletes.AddAsync(athlete);
            await context.SaveChangesAsync();
            return athlete;
        }

        private CoachQuestionDto CreateCoachQuestionDto(string firstName, string lastName)
        {
            return new CoachQuestionDto
            {
                FirstName = firstName,
                LastName = lastName,
                Disciplines = new List<string> { "FITNESS", "CROSSFIT" },
                Motivations = new List<string> { "HELPING_PEOPLE" },
                WorkOnlineWithAthletes = true,
                PresentsPracticeProgram = new List<string> { "PDF_IMAGE" },
                TrackAthlete = "USING_SOFTWARE",
                ManagingRevenue = true,
                DifficultTrackAthletes = false,
                HardCommunicationWithAthletes = false
            };
        }

        private WorkoutProgramDto CreateWorkoutProgramDto(bool publish = false, int week = 4, string level = "Intermediate")
        {
            return new WorkoutProgramDto
            {
                Publish = publish,
                Week = week,
                GeneralWarmUp = new List<string> { "Jogging", "RopeSkipping" },
                DedicatedWarmUp = "TheSameMovementsWithLessIntensity",
                ProgramLevel = level,
                ProgramPriority = new List<string> { "INCREASE_STRENGTH", "LOSS_WEIGHT" },
                Days = new List<ProgramInDayDto>
                {
                    new ProgramInDayDto { ForWhichDay = 1, AllExerciseInDays = new List<SingleExerciseDto> { new SingleExerciseDto { Set = 3, Rep = 12, ExerciseId = 101 } } }
                }
            };
        }

        #endregion

        #region SubmitCoachQuestions Tests

        [Fact]
        public async Task SubmitCoachQuestions_UserNotFound_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var nonExistentPhoneNumber = "0000000000";
            var dto = CreateCoachQuestionDto("Ghost", "User");

            // Act
            var response = await repository.SubmitCoachQuestions(nonExistentPhoneNumber, dto);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("User not found", response.Message);
        }

        [Fact]
        public async Task SubmitCoachQuestions_UserIsNotACoach_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            // این کاربر مربی نیست، ورزشکار است
            var athlete = await CreateAndAddAthlete(dbContext, "1111111111");
            var dto = CreateCoachQuestionDto("Not", "ACoach");

            // Act
            var response = await repository.SubmitCoachQuestions(athlete.PhoneNumber, dto);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("User is not a coach", response.Message);
        }

        [Fact]
        public async Task SubmitCoachQuestions_SuccessfulSubmission_CreatesQuestionAndUpdatesUser()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "2222222222", "InitialFirst", "InitialLast");
            var dto = CreateCoachQuestionDto("UpdatedFirst", "UpdatedLast");

            // Act
            var response = await repository.SubmitCoachQuestions(coach.PhoneNumber, dto);

            // Assert (Response)
            Assert.True(response.Action);
            Assert.Equal("Coach questions submitted successfully", response.Message);

            // Assert (Database State)
            var updatedUserInDb = await dbContext.Users.FirstOrDefaultAsync(u => u.PhoneNumber == coach.PhoneNumber);
            Assert.Equal("UpdatedFirst", updatedUserInDb.FirstName);
            Assert.Equal("UpdatedLast", updatedUserInDb.LastName);

            var coachInDb = await dbContext.Coaches.Include(c => c.CoachQuestion).FirstOrDefaultAsync(c => c.Id == coach.Id);
            Assert.NotNull(coachInDb.CoachQuestion);
            Assert.True(coachInDb.CoachQuestion.WorkOnlineWithAthletes);
            Assert.Contains(CoachDispline.FITNESS, coachInDb.CoachQuestion.Disciplines);
            Assert.Equal(TrackAthlete.USING_SOFTWARE, coachInDb.CoachQuestion.TrackAthlete);
            Assert.Equal(1, await dbContext.CoachQuestions.CountAsync());
        }

        #endregion

        #region AddCoachingServices Tests

        [Fact]
        public async Task AddCoachingServices_CoachNotFound_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var serviceDto = new AddCoachServiceDto { Title = "Test", Description = "Desc", Price = 1, IsActive = true, HaveSupport = true, CommunicateType = "Email" };

            // Act
            var response = await repository.AddCoachingServices("0000000000", serviceDto);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("User is not a coach", response.Message);
            Assert.Equal(0, await dbContext.CoachServices.CountAsync());
        }

        [Theory]
        [InlineData("Gold Training Package", 199.99, true, "WhatsApp")]
        [InlineData("Silver Consultation", 49.50, false, "Email")]
        [InlineData("Free Initial Plan", 0, true, "In-App Chat")]
        public async Task AddCoachingServices_SuccessfulAdditionForVariousData_ReturnsTrueAndAddsService(string title, double price, bool isActive, string communicationType)
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "9876543210");

            var serviceDto = new AddCoachServiceDto
            {
                Title = title,
                Description = "A description for the service.",
                Price = price,
                IsActive = isActive,
                HaveSupport = true,
                CommunicateType = communicationType
            };

            // Act
            var response = await repository.AddCoachingServices(coach.PhoneNumber, serviceDto);

            // Assert
            Assert.True(response.Action);
            Assert.Equal("Coaching Service added successfully", response.Message);

            var addedService = await dbContext.CoachServices.FirstOrDefaultAsync(s => s.CoachId == coach.Id);
            Assert.NotNull(addedService);
            Assert.Equal(title, addedService.Title);
            Assert.Equal(price, addedService.Price);
            Assert.Equal(isActive, addedService.IsActive);
            Assert.Equal(communicationType, addedService.CommunicateType);
            Assert.Equal(coach.Id, addedService.CoachId);
        }

        #endregion

        #region SaveWorkoutProgram Tests

        [Fact]
        public async Task SaveWorkoutProgram_CoachNotFound_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var dto = CreateWorkoutProgramDto();

            // Act
            var response = await repository.SaveWorkoutProgram("0000000000", 1, dto);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("Coach not found", response.Message);
        }

        [Fact]
        public async Task SaveWorkoutProgram_PaymentNotFound_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "1111111111");
            var dto = CreateWorkoutProgramDto();

            // Act
            var response = await repository.SaveWorkoutProgram(coach.PhoneNumber, 999, dto);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("Payment not found", response.Message);
        }

        [Theory]
        [InlineData(4, "Beginner")]
        [InlineData(8, "Advanced")]
        public async Task SaveWorkoutProgram_SaveAsDraft_UpdatesProgramSuccessfully(int weeks, string level)
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "2222222222");
            var athlete = await CreateAndAddAthlete(dbContext, "3333333333");

            var service = new CoachService { Coach = coach, Title = "T", Description = "D", Price = 1, CommunicateType = "C" };
            var payment = new Payment { Id = 1, Coach = coach, Athlete = athlete, CoachService = service, AthleteQuestion = new AthleteQuestion { Athlete = athlete } };
            var program = new WorkoutProgram { Coach = coach, CoachId = coach.Id, Athlete = athlete, AthleteId = athlete.Id, Payment = payment, Status = WorkoutProgramStatus.NOTSTARTED, Title = "title", PaymentId = payment.Id };

            dbContext.AddRange(service, payment, program);
            await dbContext.SaveChangesAsync();

            var dto = CreateWorkoutProgramDto(publish: false, week: weeks, level: level);

            // Act
            var response = await repository.SaveWorkoutProgram(coach.PhoneNumber, payment.Id, dto);

            // Assert
            Assert.True(response.Action);
            Assert.Equal("workout program saved", response.Message);

            var updatedProgram = await dbContext.WorkoutPrograms.FirstAsync();
            Assert.Equal(WorkoutProgramStatus.WRITING, updatedProgram.Status);
            Assert.Equal(weeks, updatedProgram.ProgramDuration);
            Assert.Equal(level, updatedProgram.ProgramLevel);
            Assert.Single(updatedProgram.ProgramInDays);
        }

        [Fact]
        public async Task SaveWorkoutProgram_Publish_WhenAthleteHasNoActiveProgram_SetsProgramToActive()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "4444444444");
            var athlete = await CreateAndAddAthlete(dbContext, "5555555555");
            athlete.ActiveWorkoutProgramId = 0; // Explicitly set no active program

            var service = new CoachService { Coach = coach, Title = "T", Description = "D", Price = 1, CommunicateType = "C" };
            var payment = new Payment { Id = 1, Coach = coach, Athlete = athlete, CoachService = service, AthleteQuestion = new AthleteQuestion { Athlete = athlete, DaysPerWeekToExercise = 3 } };
            var program = new WorkoutProgram { Id = 10, Coach = coach, CoachId = coach.Id, Athlete = athlete, AthleteId = athlete.Id, Payment = payment, Status = WorkoutProgramStatus.WRITING, Title = "title", PaymentId = payment.Id };
            program.ProgramInDays.Add(new ProgramInDay { ForWhichDay = 1 });

            dbContext.AddRange(service, payment, program);
            await dbContext.SaveChangesAsync();

            var dto = CreateWorkoutProgramDto(publish: true);

            // Act
            var response = await repository.SaveWorkoutProgram(coach.PhoneNumber, payment.Id, dto);

            // Assert
            Assert.True(response.Action);
            Assert.Equal("workout program saved", response.Message);

            var updatedProgram = await dbContext.WorkoutPrograms.FindAsync(10);
            var updatedAthlete = await dbContext.Athletes.FindAsync(athlete.Id);
            Assert.Equal(WorkoutProgramStatus.ACTIVE, updatedProgram.Status);
            Assert.Equal(updatedProgram.Id, updatedAthlete.ActiveWorkoutProgramId);


            var trainingSessionCount = await dbContext.TrainingSessions.CountAsync(t => t.WorkoutProgramId == program.Id);
            Assert.Equal(12, trainingSessionCount);
        }

        [Fact]
        public async Task SaveWorkoutProgram_Publish_WhenAthleteHasActiveProgram_SetsProgramToNotActive()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "6666666666");
            var athlete = await CreateAndAddAthlete(dbContext, "7777777777");
            athlete.ActiveWorkoutProgramId = 99; // Athlete already has an active program

            var service = new CoachService { Coach = coach, Title = "T", Description = "D", Price = 1, CommunicateType = "C" };
            var payment = new Payment { Id = 1, Coach = coach, Athlete = athlete, CoachService = service, AthleteQuestion = new AthleteQuestion { Athlete = athlete } };
            var program = new WorkoutProgram { Id = 11, Coach = coach, CoachId = coach.Id, Athlete = athlete, AthleteId = athlete.Id, Payment = payment, Status = WorkoutProgramStatus.WRITING, Title = "title", PaymentId = payment.Id };

            dbContext.AddRange(service, payment, program);
            await dbContext.SaveChangesAsync();

            var dto = CreateWorkoutProgramDto(publish: true);

            // Act
            var response = await repository.SaveWorkoutProgram(coach.PhoneNumber, payment.Id, dto);

            // Assert
            Assert.True(response.Action);
            var updatedProgram = await dbContext.WorkoutPrograms.FindAsync(11);
            Assert.Equal(WorkoutProgramStatus.NOTACTIVE, updatedProgram.Status);
            Assert.Equal(99, athlete.ActiveWorkoutProgramId); // Should not change

            // در این حالت نباید هیچ جلسه تمرینی برای این برنامه ساخته شود
            var trainingSessionCount = await dbContext.TrainingSessions.CountAsync(t => t.WorkoutProgramId == program.Id);
            Assert.Equal(0, trainingSessionCount);
        }

        #endregion


        #region UpdateCoachingService Tests

        [Fact]
        public async Task UpdateCoachingService_CoachNotFound_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var nonExistentPhoneNumber = "0000000000";
            var dto = new AddCoachServiceDto { Title = "Updated Title", Price = 100, Description = "D", CommunicateType = "C", HaveSupport = true, IsActive = true };

            // Act
            var response = await repository.UpdateCoachingService(nonExistentPhoneNumber, 1, dto);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("User is not a coach", response.Message);
        }

        [Fact]
        public async Task UpdateCoachingService_ServiceNotFound_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "1111111111");
            var nonExistentServiceId = 999;
            var dto = new AddCoachServiceDto { Title = "Updated Title", Price = 100, Description = "D", CommunicateType = "C", HaveSupport = true, IsActive = true };

            // Act
            var response = await repository.UpdateCoachingService(coach.PhoneNumber, nonExistentServiceId, dto);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("سرویس کوچینگ یافت نشد یا قبلاً حذف شده است.", response.Message);
        }

        [Fact]
        public async Task UpdateCoachingService_ServiceIsDeleted_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "2222222222");

            // یک سرویس می‌سازیم که IsDeleted = true است
            var deletedService = new CoachService { Coach = coach, IsDeleted = true, Title = "Old", Description = "Old", Price = 10, CommunicateType = "Old" };
            coach.CoachingServices = new List<CoachService> { deletedService };
            dbContext.CoachServices.Add(deletedService);
            await dbContext.SaveChangesAsync();

            var dto = new AddCoachServiceDto { Title = "Updated Title", Price = 100, Description = "D", CommunicateType = "C", HaveSupport = true, IsActive = true };

            // Act
            var response = await repository.UpdateCoachingService(coach.PhoneNumber, deletedService.Id, dto);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("سرویس کوچینگ یافت نشد یا قبلاً حذف شده است.", response.Message);
        }

        [Fact]
        public async Task UpdateCoachingService_NoActivePayments_UpdatesExistingServiceInPlace()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "3333333333");

            var service = new CoachService { Coach = coach, Title = "Original Title", Price = 50, IsActive = true, Description = "O", CommunicateType = "O", HaveSupport = true };
            coach.CoachingServices = new List<CoachService> { service };
            dbContext.CoachServices.Add(service);
            await dbContext.SaveChangesAsync();
            var originalServiceId = service.Id;

            var dto = new AddCoachServiceDto
            {
                Title = "Updated Title",
                Price = 99.99,
                IsActive = false,
                Description = "U",
                CommunicateType = "U",
                HaveSupport = false
            };

            // Act
            var response = await repository.UpdateCoachingService(coach.PhoneNumber, originalServiceId, dto);

            // Assert
            Assert.True(response.Action);
            Assert.Equal("Coaching Service updated successfully", response.Message);

            // 1. باید فقط یک سرویس در دیتابیس وجود داشته باشد
            var serviceCount = await dbContext.CoachServices.CountAsync();
            Assert.Equal(1, serviceCount);

            // 2. سرویس موجود باید آپدیت شده باشد
            var updatedService = await dbContext.CoachServices.FindAsync(originalServiceId);
            Assert.NotNull(updatedService);
            Assert.Equal("Updated Title", updatedService.Title);
            Assert.Equal(99.99, updatedService.Price);
            Assert.False(updatedService.IsActive);
            Assert.False(updatedService.IsDeleted); // نباید حذف شده باشد
        }

        [Fact]
        public async Task UpdateCoachingService_WithActivePayments_DeletesOldAndCreatesNewService()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "4444444444");
            var athlete = await CreateAndAddAthlete(dbContext, "5555555555");

            var service = new CoachService { Coach = coach, Title = "Original Title", Price = 50, IsActive = true, Description = "O", CommunicateType = "O", HaveSupport = true, NumberOfSell = 3 };
            coach.CoachingServices = new List<CoachService> { service };
            dbContext.CoachServices.Add(service);

            // یک پرداخت فعال برای این سرویس ایجاد می‌کنیم
            var payment = new Payment { Coach = coach, Athlete = athlete, CoachService = service, Amount = 50, AthleteQuestion = new AthleteQuestion { Athlete = athlete } };
            dbContext.Payments.Add(payment);
            await dbContext.SaveChangesAsync();
            var originalServiceId = service.Id;

            var dto = new AddCoachServiceDto
            {
                Title = "New Version Title",
                Price = 120,
                IsActive = true,
                Description = "N",
                CommunicateType = "N",
                HaveSupport = true
            };

            // Act
            var response = await repository.UpdateCoachingService(coach.PhoneNumber, originalServiceId, dto);

            // Assert
            Assert.True(response.Action);
            Assert.Equal("Coaching Service updated successfully", response.Message);

            // 1. باید دو سرویس در دیتابیس وجود داشته باشد (یکی حذف شده، یکی جدید)
            var serviceCount = await dbContext.CoachServices.CountAsync();
            Assert.Equal(2, serviceCount);

            // 2. سرویس قدیمی باید IsDeleted = true شده باشد
            var oldService = await dbContext.CoachServices.FindAsync(originalServiceId);
            Assert.NotNull(oldService);
            Assert.True(oldService.IsDeleted);
            Assert.Equal("Original Title", oldService.Title); // محتوای قدیمی باید دست‌نخورده باقی بماند

            // 3. یک سرویس جدید باید با اطلاعات جدید ساخته شده باشد
            var newService = await dbContext.CoachServices.FirstOrDefaultAsync(s => !s.IsDeleted && s.CoachId == coach.Id);
            Assert.NotNull(newService);
            Assert.NotEqual(originalServiceId, newService.Id); // آیدی باید جدید باشد
            Assert.Equal("New Version Title", newService.Title);
            Assert.Equal(120, newService.Price);
            Assert.Equal(oldService.NumberOfSell, newService.NumberOfSell); // NumberOfSell باید منتقل شده باشد
        }
        #endregion


        #region DeleteCoachingService Tests

        [Fact]
        public async Task DeleteCoachingService_CoachNotFound_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var nonExistentPhoneNumber = "0000000000";
            var serviceIdToDelete = 1;

            // Act
            var response = await repository.DeleteCoachingService(nonExistentPhoneNumber, serviceIdToDelete);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("User is not a coach", response.Message);
        }

        [Fact]
        public async Task DeleteCoachingService_ServiceNotFound_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "1111111111");
            var nonExistentServiceId = 999;

            // Act
            var response = await repository.DeleteCoachingService(coach.PhoneNumber, nonExistentServiceId);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("Coaching Service not found", response.Message);
        }

        [Fact]
        public async Task DeleteCoachingService_SuccessfulDeletion_SetsIsDeletedToTrueAndReturnsSuccess()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "2222222222");

            // یک سرویس فعال برای حذف کردن ایجاد می‌کنیم
            var service = new CoachService { Coach = coach, Title = "Service To Delete", IsDeleted = false, Description = "D", Price = 1, CommunicateType = "C" };
            coach.CoachingServices = new List<CoachService> { service };
            dbContext.CoachServices.Add(service);
            await dbContext.SaveChangesAsync();
            var serviceIdToDelete = service.Id;

            // Act
            var response = await repository.DeleteCoachingService(coach.PhoneNumber, serviceIdToDelete);

            // Assert (Response)
            Assert.True(response.Action);
            Assert.Equal("Coaching Service deleted successfully", response.Message);

            // Assert (Database State)
            var serviceInDb = await dbContext.CoachServices.FindAsync(serviceIdToDelete);
            Assert.NotNull(serviceInDb);
            Assert.True(serviceInDb.IsDeleted); // مهم‌ترین بخش: فلگ باید true شده باشد

            // بررسی اینکه رکورد فیزیکی از دیتابیس حذف نشده است
            var anyServiceExists = await dbContext.CoachServices.AnyAsync(s => s.Id == serviceIdToDelete);
            Assert.True(anyServiceExists);
        }

        [Fact]
        public async Task DeleteCoachingService_ServiceAlreadyDeleted_ReturnsSuccessAndRemainsDeleted()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "3333333333");

            // یک سرویس که از قبل حذف شده ایجاد می‌کنیم
            var service = new CoachService { Coach = coach, Title = "Already Deleted Service", IsDeleted = true, Description = "D", Price = 1, CommunicateType = "C" };
            coach.CoachingServices = new List<CoachService> { service };
            dbContext.CoachServices.Add(service);
            await dbContext.SaveChangesAsync();
            var serviceIdToDelete = service.Id;

            // Act
            var response = await repository.DeleteCoachingService(coach.PhoneNumber, serviceIdToDelete);

            // Assert (Response)
            Assert.True(response.Action);
            Assert.Equal("Coaching Service deleted successfully", response.Message);

            // Assert (Database State)
            // وضعیت سرویس باید همچنان IsDeleted = true باقی بماند
            var serviceInDb = await dbContext.CoachServices.FindAsync(serviceIdToDelete);
            Assert.NotNull(serviceInDb);
            Assert.True(serviceInDb.IsDeleted);
        }


        [Fact]
        public async Task DeleteCoachingService_WhenCoachHasMultipleServices_DeletesOnlyTheTargetService()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "4444444444");

            // ایجاد چندین سرویس برای یک مربی
            var serviceToDelete = new CoachService { Coach = coach, Title = "Service To Delete", IsDeleted = false, Description = "D", Price = 1, CommunicateType = "C" };
            var serviceToKeep = new CoachService { Coach = coach, Title = "Service To Keep", IsDeleted = false, Description = "D", Price = 1, CommunicateType = "C" };
            coach.CoachingServices = new List<CoachService> { serviceToDelete, serviceToKeep };
            dbContext.CoachServices.AddRange(serviceToDelete, serviceToKeep);
            await dbContext.SaveChangesAsync();

            var serviceIdToDelete = serviceToDelete.Id;
            var serviceIdToKeep = serviceToKeep.Id;

            // Act
            var response = await repository.DeleteCoachingService(coach.PhoneNumber, serviceIdToDelete);

            // Assert (Response)
            Assert.True(response.Action);
            Assert.Equal("Coaching Service deleted successfully", response.Message);

            // Assert (Database State)
            var deletedServiceInDb = await dbContext.CoachServices.FindAsync(serviceIdToDelete);
            Assert.NotNull(deletedServiceInDb);
            Assert.True(deletedServiceInDb.IsDeleted);

            var keptServiceInDb = await dbContext.CoachServices.FindAsync(serviceIdToKeep);
            Assert.NotNull(keptServiceInDb);
            Assert.False(keptServiceInDb.IsDeleted);
        }

        #endregion


        #region GetAllPayment Tests

        // متد کمکی برای ساخت یک سناریوی کامل پرداخت برای تست‌ها
        private async Task<Payment> CreateFullPaymentScenario(
            ApplicationDbContext context,
            Coach coach,
            Athlete athlete,
            WorkoutProgramStatus programStatus,
            PaymentStatus paymentStatus = PaymentStatus.SUCCESS,
            string serviceTitle = "Test Service")
        {
            // 1. ساخت سرویس مربی
            var service = new CoachService
            {
                Coach = coach,
                Title = serviceTitle,
                Description = "Description",
                Price = 100,
                CommunicateType = "Email",
                IsActive = true,
                HaveSupport = true
            };
            await context.CoachServices.AddAsync(service);

            // 2. ساخت پرسشنامه ورزشکار (که برای پرداخت الزامی است)
            var athleteQuestion = new AthleteQuestion
            {
                Athlete = athlete,
                Weight = athlete.CurrentWeight,
                DaysPerWeekToExercise = 3
            };
            await context.AthleteQuestions.AddAsync(athleteQuestion);

            // 3. ساخت پرداخت
            var payment = new Payment
            {
                Coach = coach,
                Athlete = athlete,
                CoachService = service,
                Amount = 100,
                PaymentStatus = paymentStatus,
                AthleteQuestion = athleteQuestion
            };
            await context.Payments.AddAsync(payment);

            // 4. ساخت برنامه تمرینی مرتبط با پرداخت
            var program = new WorkoutProgram
            {
                Coach = coach,
                CoachId = coach.Id,
                Athlete = athlete,
                AthleteId = athlete.Id,
                Payment = payment,
                Status = programStatus,
                Title = $"Program for {serviceTitle}",
                PaymentId = payment.Id,
            };
            await context.WorkoutPrograms.AddAsync(program);

            // 5. اتصال برنامه به پرداخت (رابطه دوطرفه)
            payment.WorkoutProgram = program;

            await context.SaveChangesAsync();
            return payment;
        }


        [Fact]
        public async Task GetAllPayment_CoachNotFound_ReturnsEmptyList()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var nonExistentPhoneNumber = "0000000000";

            // Act
            var response = await repository.GetAllPayment(nonExistentPhoneNumber);

            // Assert
            Assert.True(response.Action);
            Assert.Equal("Payments found", response.Message);
            var resultList = Assert.IsAssignableFrom<IEnumerable<AllPaymentResponseDto>>(response.Result);
            Assert.Empty(resultList);
        }

        [Fact]
        public async Task GetAllPayment_CoachExistsButHasNoMatchingPayments_ReturnsEmptyList()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "1111111111");

            // Act
            var response = await repository.GetAllPayment(coach.PhoneNumber);

            // Assert
            Assert.True(response.Action);
            var resultList = Assert.IsAssignableFrom<IEnumerable<AllPaymentResponseDto>>(response.Result);
            Assert.Empty(resultList);
        }

        [Fact]
        public async Task GetAllPayment_ReturnsOnlyPaymentsWithCorrectStatus()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "2222222222");
            var athlete = await CreateAndAddAthlete(dbContext, "3333333333");

            // ایجاد پرداخت‌ها با وضعیت‌های مختلف با استفاده از متد کمکی
            // 1. معتبر: برنامه شروع نشده
            await CreateFullPaymentScenario(dbContext, coach, athlete, WorkoutProgramStatus.NOTSTARTED, PaymentStatus.SUCCESS, "Valid Service 1");

            // 2. معتبر: برنامه در حال نوشتن
            await CreateFullPaymentScenario(dbContext, coach, athlete, WorkoutProgramStatus.WRITING, PaymentStatus.SUCCESS, "Valid Service 2");

            // 3. نامعتبر: برنامه فعال است
            await CreateFullPaymentScenario(dbContext, coach, athlete, WorkoutProgramStatus.ACTIVE, PaymentStatus.SUCCESS, "Invalid Service - Active");

            // 4. نامعتبر: پرداخت ناموفق بوده
            await CreateFullPaymentScenario(dbContext, coach, athlete, WorkoutProgramStatus.NOTSTARTED, PaymentStatus.FAILED, "Invalid Service - Failed");

            // 5. نامعتبر: برنامه تمام شده است
            await CreateFullPaymentScenario(dbContext, coach, athlete, WorkoutProgramStatus.FINISHED, PaymentStatus.SUCCESS, "Invalid Service - Finished");


            // Act
            var response = await repository.GetAllPayment(coach.PhoneNumber);

            // Assert
            Assert.True(response.Action);
            var resultList = Assert.IsAssignableFrom<IEnumerable<AllPaymentResponseDto>>(response.Result).ToList();

            // فقط 2 پرداخت باید با شرایط مطابقت داشته باشند
            Assert.Equal(2, resultList.Count);

            // بررسی اینکه پرداخت‌های درست برگشت داده شده‌اند
            Assert.Contains(resultList, p => p.CoachServiceTitle == "Valid Service 1" && p.WorkoutProgramStatus == "NOTSTARTED");
            Assert.Contains(resultList, p => p.CoachServiceTitle == "Valid Service 2" && p.WorkoutProgramStatus == "WRITING");

            // بررسی اینکه پرداخت‌های نادرست برنگشته‌اند
            Assert.DoesNotContain(resultList, p => p.CoachServiceTitle == "Invalid Service - Active");
            Assert.DoesNotContain(resultList, p => p.CoachServiceTitle == "Invalid Service - Failed");
            Assert.DoesNotContain(resultList, p => p.CoachServiceTitle == "Invalid Service - Finished");
        }

        [Fact]
        public async Task GetAllPayment_ForDifferentCoaches_ReturnsOnlyCorrectCoachPayments()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach1 = await CreateAndAddCoach(dbContext, "12345");
            var coach2 = await CreateAndAddCoach(dbContext, "67890"); // مربی دیگر
            var athlete = await CreateAndAddAthlete(dbContext, "11111");

            // یک پرداخت معتبر برای مربی اول
            await CreateFullPaymentScenario(dbContext, coach1, athlete, WorkoutProgramStatus.NOTSTARTED, PaymentStatus.SUCCESS, "Coach1 Service");

            // یک پرداخت معتبر برای مربی دوم (که نباید در نتیجه بیاید)
            await CreateFullPaymentScenario(dbContext, coach2, athlete, WorkoutProgramStatus.WRITING, PaymentStatus.SUCCESS, "Coach2 Service");

            // Act - دریافت پرداخت‌ها فقط برای مربی اول
            var response = await repository.GetAllPayment(coach1.PhoneNumber);

            // Assert
            Assert.True(response.Action);
            var resultList = Assert.IsAssignableFrom<IEnumerable<AllPaymentResponseDto>>(response.Result).ToList();

            // باید فقط یک پرداخت برگردانده شود
            Assert.Single(resultList);
            Assert.Equal("Coach1 Service", resultList.First().CoachServiceTitle);
        }
        #endregion


        #region GetPayment Tests

        [Fact]
        public async Task GetPayment_PaymentNotFound_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "1111111111");
            var nonExistentPaymentId = 999;

            // Act
            var response = await repository.GetPayment(coach.PhoneNumber, nonExistentPaymentId);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("Payment not found", response.Message);
        }

        [Fact]
        public async Task GetPayment_PaymentBelongsToAnotherCoach_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach1 = await CreateAndAddCoach(dbContext, "12345");
            var coach2 = await CreateAndAddCoach(dbContext, "67890"); // مربی دیگر
            var athlete = await CreateAndAddAthlete(dbContext, "11111");

            // یک پرداخت برای مربی دوم ایجاد می‌کنیم
            var paymentForCoach2 = await CreateFullPaymentScenario(dbContext, coach2, athlete, WorkoutProgramStatus.NOTSTARTED);

            // Act - با شماره تلفن مربی اول، درخواست پرداخت مربی دوم را می‌دهیم
            var response = await repository.GetPayment(coach1.PhoneNumber, paymentForCoach2.Id);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("Payment not found", response.Message);
        }

        [Fact]
        public async Task GetPayment_SuccessfulRetrieval_ReturnsCorrectPaymentDetails()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "2222222222");
            var athlete = await CreateAndAddAthlete(dbContext, "3333333333", "Jane", "Smith");

            var payment = await CreateFullPaymentScenario(dbContext, coach, athlete, WorkoutProgramStatus.WRITING);

            // به برنامه تمرینی آن یک روز اضافه می‌کنیم
            payment.WorkoutProgram.ProgramInDays.Add(new ProgramInDay { ForWhichDay = 1 });
            await dbContext.SaveChangesAsync();

            // Act
            var response = await repository.GetPayment(coach.PhoneNumber, payment.Id);

            // Assert
            Assert.True(response.Action);
            Assert.Equal("Payment found", response.Message);

            var resultDto = Assert.IsType<PaymentResponseDto>(response.Result);
            Assert.Equal(payment.Id, resultDto.PaymentId);
            Assert.Equal("Jane Smith", resultDto.Name);
            Assert.Equal(payment.WorkoutProgram.Id, resultDto.WorkoutProgram.Id);

            // برنامه تمرینی باید یک روز داشته باشد و نباید خالی باشد
            Assert.Single(resultDto.WorkoutProgram.ProgramInDays);
            Assert.Equal(1, resultDto.WorkoutProgram.ProgramInDays.First().ForWhichDay);
        }

        [Fact]
        public async Task GetPayment_WhenProgramHasNoDays_AddsDefaultDayToResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "4444444444");
            var athlete = await CreateAndAddAthlete(dbContext, "5555555555");

            var payment = await CreateFullPaymentScenario(dbContext, coach, athlete, WorkoutProgramStatus.NOTSTARTED);

            // اطمینان از اینکه برنامه تمرینی هیچ روزی ندارد
            payment.WorkoutProgram.ProgramInDays.Clear();
            await dbContext.SaveChangesAsync();

            // Act
            var response = await repository.GetPayment(coach.PhoneNumber, payment.Id);

            // Assert
            Assert.True(response.Action);
            Assert.Equal("Payment found", response.Message);

            var resultDto = Assert.IsType<PaymentResponseDto>(response.Result);
            Assert.Equal(payment.Id, resultDto.PaymentId);

            // این بخش کلیدی است:
            // باید یک روز پیش‌فرض به خروجی اضافه شده باشد
            Assert.Single(resultDto.WorkoutProgram.ProgramInDays);
            var defaultDay = resultDto.WorkoutProgram.ProgramInDays.First();
            Assert.Equal(1, defaultDay.ForWhichDay);
            Assert.Empty(defaultDay.AllExerciseInDays);

            // بررسی اینکه تغییر در دیتابیس ذخیره نشده است (فقط در DTO خروجی اعمال شده)
            var programInDb = await dbContext.WorkoutPrograms.FindAsync(payment.WorkoutProgram.Id);
            Assert.Empty(programInDb.ProgramInDays);
        }

        #endregion



        #region GetProfile Tests

        [Fact]
        public async Task GetProfile_UserNotFound_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var nonExistentPhoneNumber = "0000000000";

            // Act
            var response = await repository.GetProfile(nonExistentPhoneNumber);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("Coach not found", response.Message);
        }

        [Fact]
        public async Task GetProfile_UserIsAthleteNotCoach_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            // یک ورزشکار ایجاد می‌کنیم، نه مربی
            var athlete = await CreateAndAddAthlete(dbContext, "1111111111");

            // Act
            var response = await repository.GetProfile(athlete.PhoneNumber);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("Coach not found", response.Message);
        }

        [Fact]
        public async Task GetProfile_CoachExistsWithNoServicesOrPayments_ReturnsSuccessWithEmptyLists()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "2222222222", "Test", "Coach");

            // Act
            var response = await repository.GetProfile(coach.PhoneNumber);

            // Assert
            Assert.True(response.Action);
            Assert.Equal("Coach found", response.Message);

            var resultDto = Assert.IsType<CoachProfileResponse>(response.Result);
            Assert.Equal("Test Coach", resultDto.FirstName + " " + resultDto.LastName);
            Assert.Empty(resultDto.CoachingServices);
            Assert.Equal(0, resultDto.NumberOfAthlete);
        }

        [Fact]
        public async Task GetProfile_ReturnsOnlyActiveServices()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "3333333333");

            // ایجاد سرویس‌ها: یکی فعال، یکی حذف شده
            var activeService = new CoachService { Coach = coach, Title = "Active Service", IsDeleted = false, Description = "D", Price = 1, CommunicateType = "C" };
            var deletedService = new CoachService { Coach = coach, Title = "Deleted Service", IsDeleted = true, Description = "D", Price = 1, CommunicateType = "C" };
            coach.CoachingServices = new List<CoachService> { activeService, deletedService };
            dbContext.CoachServices.AddRange(activeService, deletedService);
            await dbContext.SaveChangesAsync();

            // Act
            var response = await repository.GetProfile(coach.PhoneNumber);

            // Assert
            Assert.True(response.Action);
            var resultDto = Assert.IsType<CoachProfileResponse>(response.Result);

            // فقط سرویس فعال باید در خروجی باشد
            Assert.Single(resultDto.CoachingServices);
            // شما می‌توانید این بخش را با توجه به ساختار دقیق ToCoachingServiceResponse دقیق‌تر کنید
            // برای مثال: Assert.Equal("Active Service", (resultDto.CoachingServices.First() as dynamic).Title);
        }

        [Fact]
        public async Task GetProfile_ReturnsOnlyPaymentsForStartedPrograms()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "4444444444");
            var athlete1 = await CreateAndAddAthlete(dbContext, "5555555555");
            var athlete2 = await CreateAndAddAthlete(dbContext, "6666666666");

            // ایجاد پرداخت‌ها با برنامه‌هایی در وضعیت‌های مختلف
            // 1. برنامه فعال - باید در شمارش ورزشکاران فعال بیاید
            await CreateFullPaymentScenario(dbContext, coach, athlete1, WorkoutProgramStatus.ACTIVE);

            // 2. برنامه تمام شده - باید در شمارش ورزشکاران فعال بیاید
            await CreateFullPaymentScenario(dbContext, coach, athlete2, WorkoutProgramStatus.FINISHED);

            // 3. برنامه در حال نوشتن - نباید در شمارش ورزشکاران فعال بیاید
            await CreateFullPaymentScenario(dbContext, coach, athlete1, WorkoutProgramStatus.WRITING);

            // 4. برنامه شروع نشده - نباید در شمارش ورزشکاران فعال بیاید
            await CreateFullPaymentScenario(dbContext, coach, athlete2, WorkoutProgramStatus.NOTSTARTED);

            // Act
            var response = await repository.GetProfile(coach.PhoneNumber);

            // Assert
            Assert.True(response.Action);
            var resultDto = Assert.IsType<CoachProfileResponse>(response.Result);

            // ما دو ورزشکار (athlete1 و athlete2) داریم که برنامه‌های شروع شده دارند (ACTIVE و FINISHED)
            // با اینکه دو پرداخت معتبر داریم، اما چون ممکن است برای یک ورزشکار باشند، از Distinct استفاده می‌شود.
            // در این تست، دو ورزشکار متفاوت داریم، پس نتیجه باید 2 باشد.
            Assert.Equal(2, resultDto.NumberOfAthlete);
        }

        #endregion


        #region GetWorkoutProgram Tests

        [Fact]
        public async Task GetWorkoutProgram_CoachNotFound_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var nonExistentPhoneNumber = "0000000000";
            var programId = 1;

            // Act
            var response = await repository.GetWorkoutProgram(nonExistentPhoneNumber, programId);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("Coach not found", response.Message);
        }

        [Fact]
        public async Task GetWorkoutProgram_ProgramNotFound_ReturnsFalseApiResponse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "1111111111");
            var nonExistentProgramId = 999;

            // Act
            var response = await repository.GetWorkoutProgram(coach.PhoneNumber, nonExistentProgramId);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("Payment not found", response.Message);
        }

        [Fact]
        public async Task GetWorkoutProgram_ProgramExistsButHasNoDays_ReturnsSuccessWithEmptyDayList()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "2222222222");
            var athlete = await CreateAndAddAthlete(dbContext, "3333333333");

            // یک سناریوی کامل پرداخت و برنامه ایجاد می‌کنیم
            var payment = await CreateFullPaymentScenario(dbContext, coach, athlete, WorkoutProgramStatus.WRITING);
            var program = payment.WorkoutProgram;

            // اطمینان از اینکه برنامه هیچ روزی ندارد
            program.ProgramInDays.Clear();
            await dbContext.SaveChangesAsync();

            // Act - با Id برنامه تمرینی، متد را فراخوانی می‌کنیم
            var response = await repository.GetWorkoutProgram(coach.PhoneNumber, program.Id);

            // Assert
            Assert.True(response.Action);
            Assert.Equal("workout program found", response.Message);

            var resultList = Assert.IsAssignableFrom<List<ProgramInDayDto>>(response.Result);
            Assert.Empty(resultList);
        }

        [Fact]
        public async Task GetWorkoutProgram_ProgramExistsWithDaysAndExercises_ReturnsCorrectDto()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new CoachRepository(dbContext);
            var coach = await CreateAndAddCoach(dbContext, "4444444444");
            var athlete = await CreateAndAddAthlete(dbContext, "5555555555");

            var payment = await CreateFullPaymentScenario(dbContext, coach, athlete, WorkoutProgramStatus.WRITING);
            var program = payment.WorkoutProgram;

            // اضافه کردن روزها و تمرینات به برنامه
            var day1 = new ProgramInDay
            {
                ForWhichDay = 1,
                AllExerciseInDays = new List<SingleExercise>
        {
            new SingleExercise { ExerciseId = 101, Set = 3, Rep = 12 },
            new SingleExercise { ExerciseId = 102, Set = 4, Rep = 10 }
        }
            };
            var day2 = new ProgramInDay { ForWhichDay = 2 }; // یک روز خالی
            program.ProgramInDays.AddRange(new[] { day1, day2 });
            await dbContext.SaveChangesAsync();

            // Act
            var response = await repository.GetWorkoutProgram(coach.PhoneNumber, program.Id);

            // Assert
            Assert.True(response.Action);
            Assert.Equal("workout program found", response.Message);

            var resultList = Assert.IsAssignableFrom<List<ProgramInDayDto>>(response.Result);
            Assert.Equal(2, resultList.Count); // باید دو روز برگردانده شود

            // بررسی روز اول
            var resultDay1 = resultList.FirstOrDefault(d => d.ForWhichDay == 1);
            Assert.NotNull(resultDay1);
            Assert.Equal(2, resultDay1.AllExerciseInDays.Count);
            Assert.Contains(resultDay1.AllExerciseInDays, e => e.ExerciseId == 101 && e.Set == 3);

            // بررسی روز دوم
            var resultDay2 = resultList.FirstOrDefault(d => d.ForWhichDay == 2);
            Assert.NotNull(resultDay2);
            Assert.Empty(resultDay2.AllExerciseInDays);
        }

        #endregion
    }
}