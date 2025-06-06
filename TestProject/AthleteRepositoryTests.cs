using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Actions;
using sport_app_backend.Repository;
using sport_app_backend.Controller; // For AddActivityDto
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Payments;
using sport_app_backend.Dtos.ProgramDto;
using sport_app_backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using sport_app_backend.Models.Question.A_Question;
using System.Globalization;
using sport_app_backend.Dtos.ZarinPal.Verify;
using sport_app_backend.Models.Challenge_Achievement;

namespace TestProject
{
    public class AthleteRepositoryTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique name for each test
                .Options;
            var dbContext = new ApplicationDbContext(options);
            dbContext.Database.EnsureCreated();
            return dbContext;
        }

        // Helper to create a basic Athlete with User
        private async Task<Athlete> CreateAndAddAthlete(ApplicationDbContext context, string phoneNumber, string firstName = "Test", string lastName = "User")
        {
            var user = new User
            {
                PhoneNumber = phoneNumber,
                FirstName = firstName,
                LastName = lastName,
                TypeOfUser = TypeOfUser.ATHLETE,
                Gender = Gender.MALE, // Default
                BirthDate = new DateTime(2000, 1, 1),
                ImageProfile = "default.jpg",
                Bio = "Test bio"
            };
            var athlete = new Athlete
            {
                User = user,
                PhoneNumber = phoneNumber,
                Height = 170,
                CurrentWeight = 70,
                WeightGoal = 65
            };
            user.Athlete = athlete;

            await context.Users.AddAsync(user);
            await context.Athletes.AddAsync(athlete);
            await context.SaveChangesAsync();
            return athlete;
        }

        // Helper to create a basic Coach with User
        private async Task<Coach> CreateAndAddCoach(ApplicationDbContext context, string phoneNumber, string firstName = "TestCoach", string lastName = "UserCoach")
        {
            var user = new User
            {
                PhoneNumber = phoneNumber,
                FirstName = firstName,
                LastName = lastName,
                TypeOfUser = TypeOfUser.COACH,
                Gender = Gender.MALE,
                BirthDate = new DateTime(1990, 1, 1),
                 ImageProfile = "coach.jpg",
                Bio = "Coach bio"
            };
            var coach = new Coach
            {
                User = user,
                PhoneNumber = phoneNumber,
                Email = $"{firstName}@example.com",
                HeadLine = "Experienced Coach"
            };
            user.Coach = coach;

            await context.Users.AddAsync(user);
            await context.Coaches.AddAsync(coach);
            await context.SaveChangesAsync();
            return coach;
        }


        [Fact]
        public async Task GetLastWeekActivity_AthleteNotFound_ReturnsFalseApiResponse() //
        {
            var dbContext = GetInMemoryDbContext();
            var service = new AthleteRepository(dbContext);
            var phoneNumber = "1234567890";

            var response = await service.GetLastWeekActivity(phoneNumber);

            Assert.False(response.Action);
            Assert.Equal("User is not an athlete", response.Message);
            Assert.Null(response.Result);
        }

        [Fact]
        public async Task GetLastWeekActivity_AthleteFound_NoActivitiesLastWeek_ReturnsTrueApiResponseWithEmptyList() //
        {
            var dbContext = GetInMemoryDbContext();
            var service = new AthleteRepository(dbContext);
            var phoneNumber = "1112223333";
            await CreateAndAddAthlete(dbContext, phoneNumber);


            var response = await service.GetLastWeekActivity(phoneNumber);

            Assert.True(response.Action);
            Assert.Equal("Activities found", response.Message);
            var resultList = Assert.IsAssignableFrom<List<ActivityDto>>(response.Result);
            Assert.Empty(resultList);
        }


        [Fact]
        public async Task GetLastWeekActivity_AthleteFound_WithActivitiesLastWeek_ReturnsCorrectActivities() //
        {
            var dbContext = GetInMemoryDbContext();
            var service = new AthleteRepository(dbContext);
            var phoneNumber = "4445556666";
            var athlete = await CreateAndAddAthlete(dbContext, phoneNumber);


            var today = DateTime.Now.Date;
            var lastSaturday = service.GetLastSaturday(today);

            var activitiesToAdd = new List<Activity>
            {
                new Activity
                {
                    AthleteId = athlete.Id,
                    Name = "Morning Run",
                    Date = lastSaturday,
                    CaloriesLost = 300,
                    Duration = 30,
                    ActivityCategory = ActivityCategory.RUNNING,
                    Athlete = athlete
                },
                new Activity
                {
                    AthleteId = athlete.Id,
                    Name = "Evening Walk",
                    Date = lastSaturday.AddDays(1),
                    CaloriesLost = 150,
                    Duration = 45,
                    ActivityCategory = ActivityCategory.WALKING,
                    Athlete = athlete
                },
                new Activity // This activity should NOT be included as it's before last Saturday
                {
                    AthleteId = athlete.Id,
                    Name = "Old Swim",
                    Date = lastSaturday.AddDays(-1), // One day before last Saturday
                    CaloriesLost = 400,
                    Duration =60 ,
                    ActivityCategory = ActivityCategory.SWIMMING,
                    Athlete = athlete
                }
            };
            await dbContext.Activities.AddRangeAsync(activitiesToAdd);
            await dbContext.SaveChangesAsync();

            var response = await service.GetLastWeekActivity(phoneNumber);

            Assert.True(response.Action);
            Assert.Equal("Activities found", response.Message);
            var resultList = Assert.IsAssignableFrom<List<ActivityDto>>(response.Result);
            Assert.Equal(2, resultList.Count); // Only 2 activities should be from last week

            var firstActivity = resultList.FirstOrDefault(a => a.Id == activitiesToAdd[0].Id);
            Assert.NotNull(firstActivity);
            Assert.Equal(lastSaturday.ToString("yyyy-MM-dd"), firstActivity.Date);
            Assert.Equal(300, firstActivity.CaloriesLost);
            Assert.Equal(ActivityCategory.RUNNING.ToString(), firstActivity.ActivityCategory);

            var secondActivity = resultList.FirstOrDefault(a => a.Id == activitiesToAdd[1].Id);
            Assert.NotNull(secondActivity);
            Assert.Equal(lastSaturday.AddDays(1).ToString("yyyy-MM-dd"), secondActivity.Date);
        }


        [Fact]
        public async Task AddActivity_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var phoneNumber = "09123456789";
            var addActivityDto = new AddActivityDto
            {
                ActivityCategory = ActivityCategory.RUNNING.ToString(),
                Duration = 30,
                CaloriesLost = 200,
                Distance = 5,
                Date = DateTime.Now.ToString("yyyy-MM-dd")
            };

            var response = await repository.AddActivity(phoneNumber, addActivityDto);

            Assert.False(response.Action);
            Assert.Equal("User is not an athlete", response.Message);
        }

        [Fact]
        public async Task AddActivity_ValidInput_ReturnsTrueAndAddsActivity()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var phoneNumber = "09123456789";
            var athlete = await CreateAndAddAthlete(dbContext, phoneNumber);

            var addActivityDto = new AddActivityDto
            {
                ActivityCategory = ActivityCategory.CYCLING.ToString(),
                Duration = 60,
                CaloriesLost = 500,
                Distance = 20,
                Date = "2024-01-15"
            };

            var response = await repository.AddActivity(phoneNumber, addActivityDto);

            Assert.True(response.Action);
            Assert.Equal("Sport added successfully", response.Message);

            var activityInDb = await dbContext.Activities.FirstOrDefaultAsync(a => a.AthleteId == athlete.Id);
            Assert.NotNull(activityInDb);
            Assert.Equal(ActivityCategory.CYCLING, activityInDb.ActivityCategory);
            Assert.Equal(60, activityInDb.Duration);
            Assert.Equal(500, activityInDb.CaloriesLost);
            Assert.Equal(20, activityInDb.Distance);
            Assert.Equal(new DateTime(2024,1,15), activityInDb.Date);
        }


        [Fact]
        public async Task AddWaterIntake_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var phoneNumber = "09123456789";
            var waterInTakeDto = new WaterInTakeDto { DailyCupOfWater = 8, Reminder = 60 };

            var response = await repository.AddWaterIntake(phoneNumber, waterInTakeDto);

            Assert.False(response.Action);
            Assert.Equal("User is not an athlete", response.Message);
        }

        [Fact]
        public async Task AddWaterIntake_NewIntake_ReturnsTrueAndAddsIntake()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var phoneNumber = "09123456789";
            var athlete = await CreateAndAddAthlete(dbContext, phoneNumber);

            var waterInTakeDto = new WaterInTakeDto { DailyCupOfWater = 10, Reminder = 30 };

            var response = await repository.AddWaterIntake(phoneNumber, waterInTakeDto);

            Assert.True(response.Action);
            Assert.Equal("WaterIntake added successfully", response.Message);

            var intakeInDb = await dbContext.WaterInTakes.FirstOrDefaultAsync(wi => wi.AthleteId == athlete.Id);
            Assert.NotNull(intakeInDb);
            Assert.Equal(10, intakeInDb.DailyCupOfWater);
            Assert.Equal(30, intakeInDb.Reminder);
        }

        [Fact]
        public async Task AddWaterIntake_ExistingIntake_ReturnsTrueAndUpdateIntake()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var phoneNumber = "09123456789";
            var athlete = await CreateAndAddAthlete(dbContext, phoneNumber);

            // Add initial intake
            var initialDto = new WaterInTakeDto { DailyCupOfWater = 5, Reminder = 90 };
            await repository.AddWaterIntake(phoneNumber, initialDto);

            // Update intake
            var updatedDto = new WaterInTakeDto { DailyCupOfWater = 12, Reminder = 45 };
            var response = await repository.AddWaterIntake(phoneNumber, updatedDto);

            Assert.True(response.Action);
            Assert.Equal("WaterIntake added successfully", response.Message);

            var intakeInDb = await dbContext.WaterInTakes.FirstOrDefaultAsync(wi => wi.AthleteId == athlete.Id);
            Assert.NotNull(intakeInDb);
            Assert.Equal(12, intakeInDb.DailyCupOfWater);
            Assert.Equal(45, intakeInDb.Reminder);
            Assert.Equal(1, await dbContext.WaterInTakes.CountAsync(wi => wi.AthleteId == athlete.Id)); // Ensure only one record
        }


        [Fact]
        public async Task SearchCoaches_NoMatchingCoaches_ReturnsEmptyList()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            await CreateAndAddCoach(dbContext, "09111111111", "John", "Doe"); // Add some coaches

            var searchDto = new CoachNameSearchDto { FullName = "NonExistent Name" };
            var response = await repository.SearchCoaches(searchDto);

            Assert.True(response.Action);
            var resultList = Assert.IsAssignableFrom<List<CoachForSearch>>(response.Result);
            Assert.Empty(resultList);
        }

        [Fact]
        public async Task SearchCoaches_MatchingCoaches_ReturnsCorrectList()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var coach1User = await CreateAndAddCoach(dbContext, "09111111111", "Alice", "Smith");
            var coach2User = await CreateAndAddCoach(dbContext, "09222222222", "Bob", "Johnson");
            var coach3User = await CreateAndAddCoach(dbContext, "09333333333", "Alicia", "Keys");


            var searchDto = new CoachNameSearchDto { FullName = "Alice" };
            var response = await repository.SearchCoaches(searchDto);

            Assert.True(response.Action);
            var resultList = Assert.IsAssignableFrom<List<CoachForSearch>>(response.Result);
            Assert.Single(resultList); // Alice Smith and Alicia Keys
            Assert.Contains(resultList, c => c.FirstName == "Alice" && c.LastName == "Smith");
            Assert.DoesNotContain(resultList, c => c.FirstName == "Alicia" && c.LastName == "Keys");
            Assert.DoesNotContain(resultList, c => c.FirstName == "Bob");
        }

        [Fact]
        public async Task DeleteActivity_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var response = await repository.DeleteActivity("09000000000", 1);
            Assert.False(response.Action);
            Assert.Equal("User is not an athlete", response.Message);
        }

        [Fact]
        public async Task DeleteActivity_ActivityNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");

            var response = await repository.DeleteActivity(athlete.PhoneNumber, 999); // Non-existent activity ID
            Assert.False(response.Action);
            Assert.Equal("Activity not found", response.Message);
        }

         [Fact]
        public async Task DeleteActivity_ValidInput_DeletesActivityAndReturnsTrue()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            var activity = new Activity { AthleteId = athlete.Id, Name = "To Delete", Date = DateTime.Now, Athlete = athlete, ActivityCategory = ActivityCategory.EXERCISE};
            await dbContext.Activities.AddAsync(activity);
            await dbContext.SaveChangesAsync();
            var activityIdToDelete = activity.Id;

            var response = await repository.DeleteActivity(athlete.PhoneNumber, activityIdToDelete);

            Assert.True(response.Action);
            Assert.Equal("Activity deleted successfully", response.Message);
            Assert.Null(await dbContext.Activities.FindAsync(activityIdToDelete));
        }

        [Fact]
        public async Task SubmitAthleteQuestions_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var dto = new AthleteQuestionDto { /* fill with valid data */ BirthDay = "2000-01-01", FitnessLevel = FitnessLevel.BEGINNER.ToString(), ExerciseGoal = ExerciseGoal.STAYFIT.ToString() };

            var response = await repository.SubmitAthleteQuestions("09000000000", dto);

            Assert.False(response.Action);
            Assert.Equal("User is not an athlete", response.Message);
        }

        [Fact]
        public async Task SubmitAthleteQuestions_ValidInput_AddsQuestionAndReturnsTrue()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            var birthDate = "1995-05-10";
            var dto = new AthleteQuestionDto
            {
                DaysPerWeekToExercise = 3,
                CurrentBodyForm = 2,
                CurrentWeight = athlete.CurrentWeight,
                ExerciseGoal = ExerciseGoal.LOSEWEIGHT.ToString(),
                InjuryArea = new InjuryAreaDto { None = true },
                FitnessLevel = FitnessLevel.INTERMEDIATE.ToString(),
                BirthDay = birthDate
            };

            var response = await repository.SubmitAthleteQuestions(athlete.PhoneNumber, dto);

            Assert.True(response.Action);
            Assert.Equal("Athlete questions submitted successfully", response.Message);

            var questionInDb = await dbContext.AthleteQuestions.Include(q => q.InjuryArea).FirstOrDefaultAsync(q => q.AthleteId == athlete.Id);
            Assert.NotNull(questionInDb);
            Assert.Equal(3, questionInDb.DaysPerWeekToExercise);
            Assert.Equal(FitnessLevel.INTERMEDIATE, questionInDb.FitnessLevel);
            Assert.NotNull(questionInDb.InjuryArea);
            Assert.True(questionInDb.InjuryArea.None);

            var userInDb = await dbContext.Users.FindAsync(athlete.UserId);
            Assert.NotNull(userInDb);
            Assert.Equal(DateTime.Parse(birthDate), userInDb.BirthDate);
        }


        [Fact]
        public async Task AthleteFirstQuestions_UserNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var dto = new AthleteFirstQuestionsDto { FirstName = "New", LastName = "User", CurrentWeight = 60, Height = 160 };

            var response = await repository.AthleteFirstQuestions("09000000000", dto);

            Assert.False(response.Action);
            Assert.Equal("User not found", response.Message);
        }


        [Fact]
        public async Task AthleteFirstQuestions_UserIsNotAthlete_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
             var user = new User { PhoneNumber = "09123456789", TypeOfUser = TypeOfUser.COACH, BirthDate = DateTime.Now.AddYears(-20) }; // Not an athlete
            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            var dto = new AthleteFirstQuestionsDto { FirstName = "New", LastName = "User", CurrentWeight = 60, Height = 160 };
            var response = await repository.AthleteFirstQuestions(user.PhoneNumber, dto);

            Assert.False(response.Action);
            Assert.Equal("User is not an athlete", response.Message);
        }

        [Fact]
        public async Task AthleteFirstQuestions_ValidInput_UpdatesAthleteAndUser_ReturnsTrue()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789", "OldFirst", "OldLast");
            var originalHeight = athlete.Height;
            var originalWeight = athlete.CurrentWeight;


            var dto = new AthleteFirstQuestionsDto { FirstName = "UpdatedFirst", LastName = "UpdatedLast", CurrentWeight = 75.5, Height = 180 };
            var response = await repository.AthleteFirstQuestions(athlete.PhoneNumber, dto);

            Assert.True(response.Action);
            Assert.Equal("Athlete first questions submitted successfully", response.Message);
             var resultData = response.Result as dynamic; // Be careful with dynamic or create a specific DTO for this
            Assert.NotNull(resultData);


            var updatedAthlete = await dbContext.Athletes.FindAsync(athlete.Id);
            Assert.NotNull(updatedAthlete);
            Assert.Equal(180, updatedAthlete.Height);
            Assert.Equal(75.5, updatedAthlete.CurrentWeight);

            var updatedUser = await dbContext.Users.FindAsync(athlete.UserId);
            Assert.NotNull(updatedUser);
            Assert.Equal("UpdatedFirst", updatedUser.FirstName);
            Assert.Equal("UpdatedLast", updatedUser.LastName);
        }

         [Fact]
        public async Task UpdateWaterInDay_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var response = await repository.UpdateWaterInDay("09000000000", 1);
            Assert.False(response.Action);
            Assert.Equal("User is not athlete", response.Message);
        }

        [Fact]
        public async Task UpdateWaterInDay_NewEntry_PositiveCups_AddsNewRecord()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");

            var response = await repository.UpdateWaterInDay(athlete.PhoneNumber, 1);

            Assert.True(response.Action);
            Assert.Equal("WaterInDay added successfully", response.Message);
            var waterInDay = await dbContext.WaterInDays.FirstOrDefaultAsync(w => w.AthleteId == athlete.Id && w.Date.Date == DateTime.Now.Date);
            Assert.NotNull(waterInDay);
            Assert.Equal(1, waterInDay.NumberOfCupsDrinked);
        }

        [Fact]
        public async Task UpdateWaterInDay_ExistingEntry_PositiveCups_UpdatesRecord()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            // Add initial record
            await repository.UpdateWaterInDay(athlete.PhoneNumber, 1);

            var response = await repository.UpdateWaterInDay(athlete.PhoneNumber, 2); // Add 2 more cups

            Assert.True(response.Action);
            Assert.Equal("WaterInDay updated successfully", response.Message);
            var waterInDay = await dbContext.WaterInDays.FirstOrDefaultAsync(w => w.AthleteId == athlete.Id && w.Date.Date == DateTime.Now.Date);
            Assert.NotNull(waterInDay);
            Assert.Equal(1 + 2, waterInDay.NumberOfCupsDrinked);
        }

        [Fact]
        public async Task UpdateWaterInDay_ExistingEntry_NegativeCups_UpdatesRecord()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            await repository.UpdateWaterInDay(athlete.PhoneNumber, 3); // Initial 3 cups

            var response = await repository.UpdateWaterInDay(athlete.PhoneNumber, -1); // Remove 1 cup

            Assert.True(response.Action);
            Assert.Equal("WaterInDay updated successfully", response.Message);
            var waterInDay = await dbContext.WaterInDays.FirstOrDefaultAsync(w => w.AthleteId == athlete.Id && w.Date.Date == DateTime.Now.Date);
            Assert.NotNull(waterInDay);
            Assert.Equal(3 - 1, waterInDay.NumberOfCupsDrinked);
        }

        [Fact]
        public async Task UpdateWaterInDay_NewEntry_NegativeCups_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");

            var response = await repository.UpdateWaterInDay(athlete.PhoneNumber, -1);

            Assert.False(response.Action);
            Assert.Equal("WaterInDay is zero", response.Message);
            Assert.Null(await dbContext.WaterInDays.FirstOrDefaultAsync(w => w.AthleteId == athlete.Id && w.Date.Date == DateTime.Now.Date));
        }

        [Fact]
        public async Task UpdateGoalWeight_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var response = await repository.UpdateGoalWeight("09000000000", 60.0);
            Assert.False(response.Action);
            Assert.Equal("User is not an athlete", response.Message);
        }

        [Fact]
        public async Task UpdateGoalWeight_ValidInput_UpdatesGoalWeight()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            var newGoalWeight = 62.5;

            var response = await repository.UpdateGoalWeight(athlete.PhoneNumber, newGoalWeight);

            Assert.True(response.Action);
            Assert.Equal("Goal weight updated successfully", response.Message);
            var updatedAthlete = await dbContext.Athletes.FindAsync(athlete.Id);
            Assert.Equal(newGoalWeight, updatedAthlete.WeightGoal);
        }

        [Fact]
        public async Task UpdateWeight_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var response = await repository.UpdateWeight("09000000000", 70.0);
            Assert.False(response.Action);
            Assert.Equal("User is not an athlete", response.Message);
        }

        [Fact]
        public async Task UpdateWeight_NewWeightEntry_AddsEntryAndUpdateCurrentWeight()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            var newWeight = 72.0;
            var today = DateTime.Now.Date;

            var response = await repository.UpdateWeight(athlete.PhoneNumber, newWeight);

            Assert.True(response.Action);
            Assert.Equal("Weight updated successfully", response.Message);

            var updatedAthlete = await dbContext.Athletes.FindAsync(athlete.Id);
            Assert.Equal(newWeight, updatedAthlete.CurrentWeight);

            var weightEntry = await dbContext.WeightEntries.FirstOrDefaultAsync(we => we.AthleteId == athlete.Id && we.CurrentDate.Date == today);
            Assert.NotNull(weightEntry);
            Assert.Equal(newWeight, weightEntry.Weight);
        }

        [Fact]
        public async Task UpdateWeight_ExistingWeightEntryForToday_UpdatesEntryAndCurrentWeight()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            var today = DateTime.Now.Date;

            // Add initial entry for today
            await dbContext.WeightEntries.AddAsync(new WeightEntry { AthleteId = athlete.Id, Athlete = athlete, Weight = 69.0, CurrentDate = today });
            await dbContext.SaveChangesAsync();

            var newWeight = 71.5;
            var response = await repository.UpdateWeight(athlete.PhoneNumber, newWeight);

            Assert.True(response.Action);
            var updatedAthlete = await dbContext.Athletes.FindAsync(athlete.Id);
            Assert.Equal(newWeight, updatedAthlete.CurrentWeight);

            var weightEntriesForToday = await dbContext.WeightEntries.Where(we => we.AthleteId == athlete.Id && we.CurrentDate.Date == today).ToListAsync();
            Assert.Single(weightEntriesForToday); // Should still be one entry for today
            Assert.Equal(newWeight, weightEntriesForToday.First().Weight);
        }

        [Fact]
        public async Task GetLastMonthWeightReport_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var response = await repository.GetLastMonthWeightReport("09000000000");
            Assert.False(response.Action);
            Assert.Equal("User is not an athlete", response.Message);
        }

        [Fact]
        public async Task GetLastMonthWeightReport_NoEntries_ReturnsEmptyList()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");

            var response = await repository.GetLastMonthWeightReport(athlete.PhoneNumber);

            Assert.True(response.Action);
            var resultList = Assert.IsAssignableFrom<List<WeightReportDto>>(response.Result); // It returns List of anonymous type
            Assert.Empty(resultList);
        }

        [Fact]
        public async Task GetLastMonthWeightReport_WithEntries_ReturnsCorrectEntries()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");

            var pc = new PersianCalendar();
            var today = DateTime.Now;
            var firstDayOfPersianMonth = pc.ToDateTime(pc.GetYear(today), pc.GetMonth(today), 1, 0, 0, 0, 0);

            var entries = new List<WeightEntry>
            {
                new WeightEntry { AthleteId = athlete.Id, Athlete = athlete, Weight = 70.1, CurrentDate = firstDayOfPersianMonth.AddDays(2) },
                new WeightEntry { AthleteId = athlete.Id, Athlete = athlete, Weight = 69.8, CurrentDate = firstDayOfPersianMonth.AddDays(5) },
                new WeightEntry { AthleteId = athlete.Id, Athlete = athlete, Weight = 70.5, CurrentDate = firstDayOfPersianMonth.AddDays(-3) } // Should not be included
            };
            await dbContext.WeightEntries.AddRangeAsync(entries);
            await dbContext.SaveChangesAsync();

            var response = await repository.GetLastMonthWeightReport(athlete.PhoneNumber);

            Assert.True(response.Action);
            var resultList = Assert.IsAssignableFrom<List<WeightReportDto>>(response.Result);
            Assert.Equal(2, resultList.Count);

            // Check if the correct entries are returned (order might vary based on OrderByDescending)
            bool entry1Found = false;
            bool entry2Found = false;
            foreach (var item in resultList)
            {
                if (item.Weight == 70.1 && item.Date == firstDayOfPersianMonth.AddDays(2).ToString("yyyy-MM-dd")) entry1Found = true;
                if (item.Weight == 69.8 && item.Date == firstDayOfPersianMonth.AddDays(5).ToString("yyyy-MM-dd")) entry2Found = true;
            }
            Assert.True(entry1Found);
            Assert.True(entry2Found);
        }

        // ... More tests for other methods ...
        // Example for ActiveProgram (simplified, focusing on status changes)

        [Fact]
        public async Task ActiveProgram_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var response = await repository.ActiveProgram("09000000000", 1);
            Assert.False(response.Action);
            Assert.Equal("Athlete not found", response.Message);
        }

        [Fact]
        public async Task ActiveProgram_ProgramNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");

            var response = await repository.ActiveProgram(athlete.PhoneNumber, 999); // Non-existent program ID
            Assert.False(response.Action);
            Assert.Equal("Workout program not found", response.Message);
        }

        [Fact]
        public async Task ActiveProgram_ProgramStatusWritingOrNotStarted_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            var coach = await CreateAndAddCoach(dbContext, "09876543210");

             var payment = new Payment {
                Athlete = athlete, Coach = coach, Amount = 100,
                CoachService = new CoachService { Coach = coach, Title = "Svc", Description="D", Price=100, CommunicateType="Online"},
                AthleteQuestion = new AthleteQuestion { Athlete = athlete, DaysPerWeekToExercise = 3, Weight = 70 }
             };
            await dbContext.Payments.AddAsync(payment);
            await dbContext.SaveChangesAsync();


            var program = new WorkoutProgram { AthleteId = athlete.Id, Athlete = athlete, CoachId = coach.Id, Coach = coach, PaymentId = payment.Id, Payment = payment, Status = WorkoutProgramStatus.WRITING, Title="P1" };
            await dbContext.WorkoutPrograms.AddAsync(program);
            await dbContext.SaveChangesAsync();

            var response = await repository.ActiveProgram(athlete.PhoneNumber, program.Id);
            Assert.False(response.Action);
            Assert.Equal("Workout program status is not acceptable for activation.", response.Message);

            program.Status = WorkoutProgramStatus.NOTSTARTED;
            await dbContext.SaveChangesAsync();
            response = await repository.ActiveProgram(athlete.PhoneNumber, program.Id);
            Assert.False(response.Action);
            Assert.Equal("Workout program status is not acceptable for activation.", response.Message);
        }


        [Fact]
        public async Task ActiveProgram_ProgramStatusNotActive_ActivatesAndAddsTrainingSessions()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            var coach = await CreateAndAddCoach(dbContext, "09876543210");

            var athleteQuestion = new AthleteQuestion { Athlete = athlete, DaysPerWeekToExercise = 3, Weight = 70 };
            await dbContext.AthleteQuestions.AddAsync(athleteQuestion);

            var coachService = new CoachService { Coach = coach, Title = "Test Service", Description = "Desc", Price = 50, CommunicateType = "Email" };
            await dbContext.CoachServices.AddAsync(coachService);

            await dbContext.SaveChangesAsync(); // Save AthleteQuestion and CoachService first

            var payment = new Payment {
                AthleteId = athlete.Id,
                Athlete = athlete,
                CoachId = coach.Id,
                Coach = coach,
                Amount = 50,
                CoachServiceId = coachService.Id,
                CoachService = coachService,
                AthleteQuestionId = athleteQuestion.Id, // Link to existing AthleteQuestion
                AthleteQuestion = athleteQuestion
            };
            await dbContext.Payments.AddAsync(payment);
            await dbContext.SaveChangesAsync(); // Save Payment

            var program = new WorkoutProgram
            {
                AthleteId = athlete.Id, Athlete = athlete,
                CoachId = coach.Id, Coach = coach,
                PaymentId = payment.Id, Payment = payment,
                Status = WorkoutProgramStatus.NOTACTIVE, Title = "P Active", ProgramDuration = 4 // weeks
            };
             program.ProgramInDays.Add(new ProgramInDay { ForWhichDay = 1, AllExerciseInDays = new List<SingleExercise>() }); // Add at least one day
            await dbContext.WorkoutPrograms.AddAsync(program);
            await dbContext.SaveChangesAsync(); // Save WorkoutProgram

            var response = await repository.ActiveProgram(athlete.PhoneNumber, program.Id);

            Assert.True(response.Action);
            Assert.Equal("Program activated successfully.", response.Message);

            var updatedProgram = await dbContext.WorkoutPrograms.FindAsync(program.Id);
            Assert.Equal(WorkoutProgramStatus.ACTIVE, updatedProgram.Status);
            Assert.Equal(program.Id, athlete.ActiveWorkoutProgramId);

            var trainingSessions = await dbContext.TrainingSessions.Where(ts => ts.WorkoutProgramId == program.Id).ToListAsync();
            // DaysPerWeekToExercise = 3, ProgramDuration = 4 weeks => 3 * 4 = 12 sessions
            Assert.Equal(12, trainingSessions.Count);
            Assert.All(trainingSessions, ts => Assert.Equal(TrainingSessionStatus.NOTSTARTED, ts.TrainingSessionStatus));
        }
          [Fact]
        public async Task UpdateHightWeight_AthleteNotFound_ReturnsFalse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var phoneNumber = "09000000000";

            // Act
            var response = await repository.UpdateHightWeight(phoneNumber, 70.0, 170);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("User is not an athlete", response.Message);
        }

        [Fact]
        public async Task UpdateHightWeight_ValidInput_UpdatesAthleteAndAddsWeightEntry()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var phoneNumber = "09123456789";
            var athlete = await CreateAndAddAthlete(dbContext, phoneNumber);
            var newWeight = 75.5;
            var newHeight = 175;
            var today = DateTime.Now.Date;

            // Act
            var response = await repository.UpdateHightWeight(athlete.PhoneNumber, newWeight, newHeight);

            // Assert
            Assert.True(response.Action);
            Assert.Equal("Weight and hight updated successfully", response.Message);

            var updatedAthlete = await dbContext.Athletes.FindAsync(athlete.Id);
            Assert.NotNull(updatedAthlete);
            Assert.Equal(newWeight, updatedAthlete.CurrentWeight);
            Assert.Equal(newHeight, updatedAthlete.Height);

            var weightEntry = await dbContext.WeightEntries.FirstOrDefaultAsync(we => we.AthleteId == athlete.Id && we.CurrentDate.Date == today);
            Assert.NotNull(weightEntry);
            Assert.Equal(newWeight, weightEntry.Weight);
        }

        [Fact]
        public async Task GetLastQuestion_AthleteNotFound_ReturnsFalse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var phoneNumber = "09000000000";

            // Act
            var response = await repository.GetLastQuestion(phoneNumber);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("User is not an athlete", response.Message);
        }

        [Fact]
        public async Task GetLastQuestion_NoQuestions_ReturnsFalse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var phoneNumber = "09123456789";
            await CreateAndAddAthlete(dbContext, phoneNumber);

            // Act
            var response = await repository.GetLastQuestion(phoneNumber);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("Question not found", response.Message);
        }

        [Fact]
        public async Task GetLastQuestion_WithQuestions_ReturnsLastQuestion()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var phoneNumber = "09123456789";
            var athlete = await CreateAndAddAthlete(dbContext, phoneNumber);

            var question1 = new AthleteQuestion
            {
                AthleteId = athlete.Id,
                Athlete = athlete,
                CreatedAt = DateTime.Now.AddDays(-2),
                CurrentBodyForm = 1,
                DaysPerWeekToExercise = 3,
                FitnessLevel = FitnessLevel.BEGINNER,
                ExerciseGoal = ExerciseGoal.STAYFIT,
                Weight = 70,
                InjuryArea = new InjuryArea { None = true }
            };
            var question2 = new AthleteQuestion // This is the last question
            {
                AthleteId = athlete.Id,
                Athlete = athlete,
                CreatedAt = DateTime.Now.AddDays(-1),
                CurrentBodyForm = 2,
                DaysPerWeekToExercise = 4,
                FitnessLevel = FitnessLevel.INTERMEDIATE,
                ExerciseGoal = ExerciseGoal.GAINWEIGHT,
                Weight = 72,
                InjuryArea = new InjuryArea { Skeletal = new List<SkeletalDiseases> { SkeletalDiseases.KneePain } }
            };
            await dbContext.AthleteQuestions.AddRangeAsync(question1, question2);
            await dbContext.SaveChangesAsync();

            // Act
            var response = await repository.GetLastQuestion(phoneNumber);

            // Assert
            Assert.True(response.Action);
            Assert.Equal("Question found", response.Message);
            var resultDto = Assert.IsType<AthleteQuestionDto>(response.Result);
            Assert.Equal(question2.CurrentBodyForm, resultDto.CurrentBodyForm);
            Assert.Equal(question2.DaysPerWeekToExercise, resultDto.DaysPerWeekToExercise);
            Assert.Equal(FitnessLevel.INTERMEDIATE.ToString(), resultDto.FitnessLevel);
            Assert.NotNull(resultDto.InjuryArea);
            Assert.Contains(SkeletalDiseases.KneePain.ToString(), resultDto.InjuryArea.Skeletal);
        }


        [Fact]
        public async Task CompleteNewChallenge_AthleteNotFound_ReturnsFalse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var phoneNumber = "09000000000";
            var challengeType = ChallengeType.Swim_5.ToString();

            // Act
            var response = await repository.CompleteNewChallenge(phoneNumber, challengeType);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("User is not an athlete", response.Message);
        }

        [Fact]
        public async Task CompleteNewChallenge_ChallengeAlreadyCompleted_ReturnsFalse()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var phoneNumber = "09123456789";
            var athlete = await CreateAndAddAthlete(dbContext, phoneNumber);
            var challengeType = ChallengeType.PullUp_3;

            await dbContext.Challenges.AddAsync(new Challenge { AthleteId = athlete.Id, Athlete = athlete, ChallengeType = challengeType, CompletedAt = DateTime.Now });
            await dbContext.SaveChangesAsync();

            // Act
            var response = await repository.CompleteNewChallenge(phoneNumber, challengeType.ToString());

            // Assert
            Assert.False(response.Action);
            Assert.Equal("Challenge already completed", response.Message);
        }

        [Fact]
        public async Task CompleteNewChallenge_ValidNewChallenge_AddsChallengeAndReturnsTrue()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var phoneNumber = "09123456789";
            var athlete = await CreateAndAddAthlete(dbContext, phoneNumber);
            var challengeType = ChallengeType.Servicek_30s;

            // Act
            var response = await repository.CompleteNewChallenge(phoneNumber, challengeType.ToString());

            // Assert
            Assert.True(response.Action);
            Assert.Equal("Challenge completed successfully", response.Message);

            var challengeInDb = await dbContext.Challenges.FirstOrDefaultAsync(c => c.AthleteId == athlete.Id && c.ChallengeType == challengeType);
            Assert.NotNull(challengeInDb);
            Assert.Equal(DateTime.Now.Date, challengeInDb.CompletedAt.Date);
        }

        [Fact]
        public async Task CompletedChallenge_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var response = await repository.CompletedChallenge("09000000000");
            Assert.False(response.Action);
            Assert.Equal("User is not an athlete", response.Message);
        }

        [Fact]
        public async Task CompletedChallenge_NoChallenges_ReturnsEmptyList()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");

            var response = await repository.CompletedChallenge(athlete.PhoneNumber);

            Assert.True(response.Action);
            Assert.Equal("Challenges found", response.Message);
            var resultList = Assert.IsAssignableFrom<List<string>>(response.Result);
            Assert.Empty(resultList);
        }

        [Fact]
        public async Task CompletedChallenge_WithChallenges_ReturnsListOfChallengeTypes()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            var challenge1 = ChallengeType.Swim_5;
            var challenge2 = ChallengeType.PullUp_3;

            await dbContext.Challenges.AddRangeAsync(
                new Challenge { AthleteId = athlete.Id, Athlete = athlete, ChallengeType = challenge1, CompletedAt = DateTime.Now },
                new Challenge { AthleteId = athlete.Id, Athlete = athlete, ChallengeType = challenge2, CompletedAt = DateTime.Now }
            );
            await dbContext.SaveChangesAsync();

            var response = await repository.CompletedChallenge(athlete.PhoneNumber);

            Assert.True(response.Action);
            var resultList = Assert.IsAssignableFrom<List<string>>(response.Result);
            Assert.Equal(2, resultList.Count);
            Assert.Contains(challenge1.ToString(), resultList);
            Assert.Contains(challenge2.ToString(), resultList);
        }

        [Fact]
        public async Task GetAchievements_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var response = await repository.GetAchievements("09000000000");
            Assert.False(response.Action);
            Assert.Equal("User is not an athlete", response.Message);
        }

        [Fact]
        public async Task GetAchievements_NoActivitiesOrChallenges_ReturnsEmptyAchievements()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");

            var response = await repository.GetAchievements(athlete.PhoneNumber);

            Assert.True(response.Action);
            Assert.Equal("Achievements found", response.Message);
            var resultList = Assert.IsAssignableFrom<List<string>>(response.Result);
            Assert.Empty(resultList);
        }

        [Fact]
        public async Task GetAchievements_FirstWorkoutAchievement()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            await dbContext.Activities.AddAsync(new Activity { AthleteId = athlete.Id, Athlete = athlete, Date = DateTime.Now, ActivityCategory = ActivityCategory.RUNNING, Duration = 30, CaloriesLost = 100 });
            await dbContext.SaveChangesAsync();

            var response = await repository.GetAchievements(athlete.PhoneNumber);

            Assert.True(response.Action);
            var resultList = Assert.IsAssignableFrom<List<string>>(response.Result);
            Assert.Contains(AchievementType.FirstWorkout.ToString(), resultList);
        }

        [Fact]
        public async Task GetAchievements_FirstChallengeAchievement()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            await dbContext.Challenges.AddAsync(new Challenge { AthleteId = athlete.Id, Athlete = athlete, ChallengeType = ChallengeType.Swim_5, CompletedAt = DateTime.Now });
            await dbContext.SaveChangesAsync();

            var response = await repository.GetAchievements(athlete.PhoneNumber);

            Assert.True(response.Action);
            var resultList = Assert.IsAssignableFrom<List<string>>(response.Result);
            Assert.Contains(AchievementType.FirstChallenge.ToString(), resultList);
        }
         [Fact]
        public async Task GetAchievements_ConsistentAthlete_7ConsecutiveDays()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");

            for (int i = 0; i < 7; i++)
            {
                await dbContext.Activities.AddAsync(new Activity
                {
                    AthleteId = athlete.Id,
                    Athlete = athlete,
                    Date = DateTime.Now.Date.AddDays(-i), // 7 consecutive days ending today
                    ActivityCategory = ActivityCategory.WALKING,
                    Duration = 30,
                    CaloriesLost = 100
                });
            }
            await dbContext.SaveChangesAsync();

            // Act
            var response = await repository.GetAchievements(athlete.PhoneNumber);

            // Assert
            Assert.True(response.Action);
            var resultList = Assert.IsAssignableFrom<List<string>>(response.Result);
            Assert.Contains(AchievementType.ConsistentAthlete.ToString(), resultList);
            Assert.Contains(AchievementType.FirstWorkout.ToString(), resultList); // Should also have FirstWorkout
        }

        [Fact]
        public async Task GetAchievements_NotConsistentAthlete_LessThan7ConsecutiveDays()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");

            // Add 6 consecutive days
            for (int i = 0; i < 6; i++)
            {
                await dbContext.Activities.AddAsync(new Activity
                {
                    AthleteId = athlete.Id,
                    Athlete = athlete,
                    Date = DateTime.Now.Date.AddDays(-i),
                    ActivityCategory = ActivityCategory.WALKING,
                    Duration = 30,
                    CaloriesLost = 100
                });
            }
             // Add a non-consecutive day
            await dbContext.Activities.AddAsync(new Activity
            {
                AthleteId = athlete.Id,
                Athlete = athlete,
                Date = DateTime.Now.Date.AddDays(-8), // Gap
                ActivityCategory = ActivityCategory.WALKING,
                Duration = 30,
                CaloriesLost = 100
            });
            await dbContext.SaveChangesAsync();

            // Act
            var response = await repository.GetAchievements(athlete.PhoneNumber);

            // Assert
            Assert.True(response.Action);
            var resultList = Assert.IsAssignableFrom<List<string>>(response.Result);
            Assert.DoesNotContain(AchievementType.ConsistentAthlete.ToString(), resultList);
        }


        [Fact]
        public async Task GetAllPayments_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var response = await repository.GetAllPayments("09000000000");
            Assert.False(response.Action);
            Assert.Equal("Athlete not found", response.Message);
        }

        [Fact]
        public async Task GetAllPayments_NoWorkoutPrograms_ReturnsEmptyList()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");

            var response = await repository.GetAllPayments(athlete.PhoneNumber);

            Assert.True(response.Action);
            Assert.Equal("Payments found", response.Message);
            var resultList = Assert.IsAssignableFrom<List<AllPaymentResponseDto>>(response.Result);
            Assert.Empty(resultList);
        }

        [Fact]
        public async Task GetPayment_AthleteNotFound_ActuallyLooksForAthleteOnPayment_ReturnsPaymentNotFound()
        {
            // This test actually tests if payment exists, as athlete is checked by payment.Athlete.PhoneNumber
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            // No athlete created with "09000000000" that has this payment
            var response = await repository.GetPayment("09000000000", 1);
            Assert.False(response.Action);
            Assert.Equal("Payment not found", response.Message); // Because query is p.Athlete.PhoneNumber == phoneNumber && p.Id == paymentId
        }

        [Fact]
        public async Task GetPayment_PaymentNotFoundForAthlete_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");

            var response = await repository.GetPayment(athlete.PhoneNumber, 999); // Non-existent payment ID
            Assert.False(response.Action);
            Assert.Equal("Payment not found", response.Message);
        }

        [Fact]
        public async Task GetAllTrainingSession_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var response = await repository.GetAllTrainingSession("09000000000");
            Assert.False(response.Action);
            Assert.Equal("Athlete not found", response.Message);
        }

        [Fact]
        public async Task GetAllTrainingSession_NoActiveWorkoutProgram_ReturnsWorkoutProgramNotFound()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            athlete.ActiveWorkoutProgramId = 0; // Ensure no active program
            await dbContext.SaveChangesAsync();

            var response = await repository.GetAllTrainingSession(athlete.PhoneNumber);

            Assert.True(response.Action); // The method returns Action = true even if program not found
            Assert.Equal("workoutProgram not found", response.Message);
        }


        [Fact]
        public async Task GetTrainingSession_SessionNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            await CreateAndAddAthlete(dbContext, "09123456789"); // Athlete exists

            var response = await repository.GetTrainingSession("09123456789", 999); // Non-existent session ID
            Assert.False(response.Action);
            Assert.Equal("trainingSession not found", response.Message);
        }

        [Fact]
        public async Task DoTrainingSession_SessionNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            await CreateAndAddAthlete(dbContext, "09123456789");

            var response = await repository.DoTrainingSession("09123456789", 999, 0);
            Assert.False(response.Action);
            Assert.Equal("trainingSession not found", response.Message);
        }


        [Fact]
        public async Task FinishTrainingSession_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var dto = new FinishTrainingSessionDto { TrainingSessionId = 1, TrainingSessionName = "Test Session", Duration = 60, CaloriesLost = 300 };
            var response = await repository.FinishTrainingSession("09000000000", dto);
            Assert.False(response.Action);
            Assert.Equal("Athlete not found", response.Message);
        }

        [Fact]
        public async Task FinishTrainingSession_SessionNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            var dto = new FinishTrainingSessionDto { TrainingSessionId = 999, TrainingSessionName = "Test Session", Duration = 60, CaloriesLost = 300 };

            var response = await repository.FinishTrainingSession(athlete.PhoneNumber, dto);
            Assert.False(response.Action);
            Assert.Equal("trainingSession not found", response.Message);
        }


        [Fact]
        public async Task FeedbackTrainingSession_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var dto = new FeedbackTrainingSessionDto { TrainingSessionId = 1, ExerciseFeeling = ExerciseFeeling.Good.ToString() };
            var response = await repository.FeedbackTrainingSession("09000000000", dto);
            Assert.False(response.Action);
            Assert.Equal("Athlete not found", response.Message);
        }

        [Fact]
        public async Task FeedbackTrainingSession_SessionNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            var dto = new FeedbackTrainingSessionDto { TrainingSessionId = 999, ExerciseFeeling = ExerciseFeeling.Good.ToString() };

            var response = await repository.FeedbackTrainingSession(athlete.PhoneNumber, dto);
            Assert.False(response.Action);
            Assert.Equal("trainingSession not found", response.Message);
        }

        [Fact]
        public async Task FeedbackTrainingSession_SessionNotCompleted_ReturnsFalse()
        {
             // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var athlete = await CreateAndAddAthlete(dbContext, "09123456789");
            var coach = await CreateAndAddCoach(dbContext, "09876543210");
             var payment = new Payment { Athlete = athlete, Coach = coach, Amount = 100, CoachService = new CoachService{Coach = coach, Title="S", Description="D", Price=100, CommunicateType="C"}, AthleteQuestion = new AthleteQuestion{Athlete=athlete, DaysPerWeekToExercise=1, Weight=70} };
            await dbContext.Payments.AddAsync(payment);
            var program = new WorkoutProgram { Athlete = athlete,AthleteId = athlete.Id, Coach = coach,CoachId = coach.Id, Payment = payment,PaymentId = payment.Id, Title = "P1" };
            await dbContext.WorkoutPrograms.AddAsync(program);
            var programInDay = new ProgramInDay { WorkoutProgram = program, ForWhichDay = 1 };
            await dbContext.ProgramInDays.AddAsync(programInDay);
            var trainingSession = new TrainingSession
            {
                WorkoutProgram = program,
                WorkoutProgramId = program.Id,
                ProgramInDay = programInDay,
                ProgramInDayId = programInDay.Id,
                TrainingSessionStatus = TrainingSessionStatus.INPROGRESS, // Not COMPLETED
                ExerciseCompletionBitmap = new byte[1] { 0x00 }
            };
            await dbContext.TrainingSessions.AddAsync(trainingSession);
            await dbContext.SaveChangesAsync();

            var dto = new FeedbackTrainingSessionDto { TrainingSessionId = trainingSession.Id, ExerciseFeeling = ExerciseFeeling.Good.ToString() };

            // Act
            var response = await repository.FeedbackTrainingSession(athlete.PhoneNumber, dto);

            // Assert
            Assert.False(response.Action);
            Assert.Equal("trainingSession not completed", response.Message);
        }


        [Fact]
        public async Task ResetTrainingSession_SessionNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            // No need to create athlete as session not found is checked first
            var response = await repository.ResetTrainingSession("09123456789", 999);
            Assert.False(response.Action);
            Assert.Equal("trainingSession not found", response.Message);
        }


        [Fact]
        public async Task GetActivityPage_AthleteNotFound_ReturnsFalse()
        {
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var response = await repository.GetActivityPage("09000000000");
            Assert.False(response.Action);
            Assert.Equal("Athlete not found", response.Message);
        }

       [Fact]
        public async Task GetActivityPage_AthleteExists_ReturnsCorrectDataStructure()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new AthleteRepository(dbContext);
            var phoneNumber = "09123456789";
            var athlete = await CreateAndAddAthlete(dbContext, phoneNumber);
            athlete.WeightGoal = 65.0;
            athlete.CurrentWeight = 68.5;

            var today = DateTime.Today;
            var lastSaturday = repository.GetLastSaturday(today);
            await dbContext.Activities.AddAsync(new Activity { Athlete = athlete, AthleteId = athlete.Id, ActivityCategory = ActivityCategory.RUNNING, Date = today, Duration = 30, CaloriesLost = 250, Name = "Morning Run" });
            await dbContext.Activities.AddAsync(new Activity { Athlete = athlete, AthleteId = athlete.Id, ActivityCategory = ActivityCategory.WALKING, Date = lastSaturday, Duration = 60, CaloriesLost = 300, Name = "Saturday Walk" });

            var waterInTake = new WaterInTake { AthleteId = athlete.Id, Athlete = athlete, DailyCupOfWater = 8, Reminder = 60 };
            dbContext.WaterInTakes.Add(waterInTake);

            await dbContext.WaterInDays.AddAsync(new WaterInDay { Athlete = athlete, AthleteId = athlete.Id, Date = today, NumberOfCupsDrinked = 3 });

            var firstDayOfPersianMonth = repository.GetFirstDayOfPersianMonth(today);
            await dbContext.WeightEntries.AddAsync(new WeightEntry { Athlete = athlete, AthleteId = athlete.Id, Weight = 69.0, CurrentDate = firstDayOfPersianMonth.AddDays(1) });

            await dbContext.SaveChangesAsync();

            // Act
            var response = await repository.GetActivityPage(phoneNumber);

            // Assert
            Assert.True(response.Action);
            Assert.Equal("Activities found", response.Message);
            var result = Assert.IsType<ActivityPageDto>(response.Result); 

            Assert.NotNull(result);
            Assert.Equal(2, result.TotalActivities);
            Assert.Equal(90, result.TotalTime);
            Assert.Equal(550, result.TotalCalories);

            Assert.Equal(7, result.LastWeekActivities.Count);
      

            Assert.Equal(3, result.NumberOfCupsDrinked);
            Assert.Equal(8, result.DailyCupOfWater);
            Assert.Equal(60, result.Reminder);

            Assert.Single(result.TodayActivities);
            var todayActivity = result.TodayActivities.First();
            Assert.Equal(today.ToString("yyyy-MM-dd"), todayActivity.Date);
            Assert.Equal("Morning Run", todayActivity.Name);
            Assert.Equal(250, todayActivity.CaloriesLost);
            Assert.Equal(30, todayActivity.Duration);
            Assert.Equal(ActivityCategory.RUNNING.ToString(), todayActivity.ActivityCategory);


            Assert.Equal(68.5, result.CurrentWeight);
            Assert.Equal(65.0, result.GoalWeight);

            Assert.Single(result.LastMonthWeights);
            var lastMonthWeightEntry = result.LastMonthWeights.First();
            Assert.Equal(firstDayOfPersianMonth.AddDays(1).ToString("yyyy-MM-dd"), lastMonthWeightEntry.Date);
            Assert.Equal(69.0, lastMonthWeightEntry.Weight);
        }
    


        [Theory]
        [InlineData(DayOfWeek.Monday, 2)] // If today is Monday, last Saturday was 2 days ago
        [InlineData(DayOfWeek.Tuesday, 3)]
        [InlineData(DayOfWeek.Wednesday, 4)]
        [InlineData(DayOfWeek.Thursday, 5)]
        [InlineData(DayOfWeek.Friday, 6)]
        [InlineData(DayOfWeek.Saturday, 0)] // If today is Saturday, last Saturday is today
        [InlineData(DayOfWeek.Sunday, 1)]   // If today is Sunday, last Saturday was 1 day ago
        public void GetLastSaturday_ReturnsCorrectDate(DayOfWeek todayDayOfWeek, int expectedDaysAgo)
        {
            var dbContext = GetInMemoryDbContext(); // Not strictly needed for this static-like method test, but consistent
            var service = new AthleteRepository(dbContext);

            // Find a date that matches the target DayOfWeek
            DateTime today = DateTime.Now.Date;
            while (today.DayOfWeek != todayDayOfWeek)
            {
                today = today.AddDays(1); // Go forward until we hit the desired day of week
            }

            DateTime expectedSaturday = today.AddDays(-expectedDaysAgo);
            DateTime actualSaturday = service.GetLastSaturday(today);

            Assert.Equal(expectedSaturday, actualSaturday);
            Assert.Equal(DayOfWeek.Saturday, actualSaturday.DayOfWeek);
        }


        [Fact]
        public void GetFirstDayOfPersianMonth_ReturnsCorrectDate()
        {
            var dbContext = GetInMemoryDbContext();
            var service = new AthleteRepository(dbContext);
            var pc = new PersianCalendar();

            // Example 1: A date in the middle of a Persian month
            DateTime gregorianDate1 = new DateTime(2024, 5, 15); // Corresponds to Ordibehesht 1403
            DateTime expectedPersianFirst1 = pc.ToDateTime(1403, 2, 1, 0, 0, 0, 0); // 1st Ordibehesht 1403
            DateTime actualPersianFirst1 = service.GetFirstDayOfPersianMonth(gregorianDate1);
            Assert.Equal(expectedPersianFirst1, actualPersianFirst1);

            // Example 2: A date that is the first day of a Persian month
            DateTime gregorianDate2 = pc.ToDateTime(1403, 1, 1, 0,0,0,0); // 1st Farvardin 1403
            DateTime expectedPersianFirst2 = pc.ToDateTime(1403, 1, 1, 0, 0, 0, 0); // 1st Farvardin 1403
            DateTime actualPersianFirst2 = service.GetFirstDayOfPersianMonth(gregorianDate2);
            Assert.Equal(expectedPersianFirst2, actualPersianFirst2);

             // Example 3: A date near the end of a Persian month
            DateTime gregorianDate3 = new DateTime(2024, 3, 19); // Corresponds to 29th Esfand 1402
            DateTime expectedPersianFirst3 = pc.ToDateTime(1402, 12, 1, 0, 0, 0, 0); // 1st Esfand 1402
            DateTime actualPersianFirst3 = service.GetFirstDayOfPersianMonth(gregorianDate3);
            Assert.Equal(expectedPersianFirst3, actualPersianFirst3);
        }

    }
}