using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Actions;
using sport_app_backend.Repository;

namespace TestProject;

public class GetActivityTest

{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new ApplicationDbContext(options);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    [Fact]
    public async Task GetLastWeekActivity_AthleteNotFound_ReturnsFalseApiResponse()
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
    public async Task GetLastWeekActivity_AthleteFound_NoActivitiesLastWeek_ReturnsTrueApiResponseWithEmptyList()
    {
        var dbContext = GetInMemoryDbContext();
        var service = new AthleteRepository(dbContext);
        var phoneNumber = "1112223333";
        var athlete = new Athlete
        {
            Id = 1, PhoneNumber = phoneNumber
            ,            User = null

        };
        dbContext.Athletes.Add(athlete);
        await dbContext.SaveChangesAsync();

        var response = await service.GetLastWeekActivity(phoneNumber);

        Assert.True(response.Action);
        Assert.Equal("Activities found", response.Message);
        var resultList = Assert.IsAssignableFrom<List<ActivityDto>>(response.Result);
        Assert.Empty(resultList);
    }

    [Fact]
    public async Task GetLastWeekActivity_AthleteFound_WithActivitiesLastWeek_ReturnsCorrectActivities()
    {
        var dbContext = GetInMemoryDbContext();
        var service = new AthleteRepository(dbContext);
        var phoneNumber = "4445556666";
        var athlete = new Athlete
        {
            Id = 1, PhoneNumber = phoneNumber,
            User = null
        };
        dbContext.Athletes.Add(athlete);

        var today = DateTime.Now.Date;
        var lastSaturday = service.GetLastSaturday(today);

        var activitiesToAdd = new List<Activity>
        {
            new Activity
            {
                Id = 1,
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
                Id = 2,
                AthleteId = athlete.Id,
                Name = "Evening Walk",
                Date = lastSaturday.AddDays(1),
                CaloriesLost = 150,
                Duration = 45,
                ActivityCategory = ActivityCategory.WALKING,
                Athlete = athlete
            },
            new Activity
            {
                Id = 3,
                AthleteId = athlete.Id,
                Name = "Old Swim",
                Date = lastSaturday.AddDays(-1),
                CaloriesLost = 400,
                Duration =60 ,
                ActivityCategory = ActivityCategory.SWIMMING,
                Athlete = athlete
            }
        };
        dbContext.Activities.AddRange(activitiesToAdd);
        await dbContext.SaveChangesAsync();

        var response = await service.GetLastWeekActivity(phoneNumber);

        Assert.True(response.Action);
        Assert.Equal("Activities found", response.Message);
        var resultList = Assert.IsAssignableFrom<List<ActivityDto>>(response.Result);
        Assert.Equal(2, resultList.Count);

        var firstActivity = resultList[0];
        Assert.Equal(1, firstActivity.Id);
        Assert.Equal(lastSaturday.ToString("yyyy-MM-dd"), firstActivity.Date);
        Assert.Equal(300, firstActivity.CaloriesLost);
        Assert.Equal(ActivityCategory.RUNNING.ToString(), firstActivity.ActivityCategory);

        var secondActivity = resultList[1];
        Assert.Equal(2, secondActivity.Id);
        Assert.Equal(lastSaturday.AddDays(1).ToString("yyyy-MM-dd"), secondActivity.Date);
    }

     [Fact]
    public async Task GetLastWeekActivity_AthleteFound_ActivitiesOnLastSaturday_ReturnsActivity()
    {
        var dbContext = GetInMemoryDbContext();
        var service = new AthleteRepository(dbContext);
        var phoneNumber = "7778889999";
        var athlete = new Athlete
        {
            Id = 1,
            PhoneNumber = phoneNumber,
            User = null
        };
        dbContext.Athletes.Add(athlete);

        var today = DateTime.Now.Date;
        var lastSaturday = service.GetLastSaturday(today);

        var activitiesToAdd = new List<Activity>
        {
            new Activity
            {
                Id = 1,
                AthleteId = athlete.Id,
                Name = "Saturday Workout",
                Date = lastSaturday,
                CaloriesLost = 500,
                Duration = 60,
                ActivityCategory = ActivityCategory.CYCLING,
                Athlete = athlete
            }
        };
        dbContext.Activities.AddRange(activitiesToAdd);
        await dbContext.SaveChangesAsync();

        var response = await service.GetLastWeekActivity(phoneNumber);

        Assert.True(response.Action);
        Assert.Equal("Activities found", response.Message);
        var resultList = Assert.IsAssignableFrom<List<ActivityDto>>(response.Result);
        Assert.Single(resultList);

        var activity = resultList[0];
        Assert.Equal(1, activity.Id);
        Assert.Equal(lastSaturday.ToString("yyyy-MM-dd"), activity.Date);
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, 2)]
    [InlineData(DayOfWeek.Tuesday, 3)]
    [InlineData(DayOfWeek.Wednesday, 4)]
    [InlineData(DayOfWeek.Thursday, 5)]
    [InlineData(DayOfWeek.Friday, 6)]
    [InlineData(DayOfWeek.Saturday, 0)]
    [InlineData(DayOfWeek.Sunday, 1)]
    public void GetLastSaturday_ReturnsCorrectDate(DayOfWeek todayDayOfWeek, int expectedDaysAgo)
    {
        var dbContext = GetInMemoryDbContext();
        var service = new AthleteRepository(dbContext);

        DateTime today = DateTime.Now.Date;
        while (today.DayOfWeek != todayDayOfWeek)
        {
            today = today.AddDays(-1);
        }

        DateTime expectedSaturday = today.AddDays(-expectedDaysAgo);

        DateTime actualSaturday = service.GetLastSaturday(today);

        Assert.Equal(expectedSaturday, actualSaturday);
        Assert.Equal(DayOfWeek.Saturday, actualSaturday.DayOfWeek);
    }
}