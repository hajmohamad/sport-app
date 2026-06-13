using System.Globalization;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Login_Sinup;
using Amazon.S3;
using Amazon.S3.Model;
using sport_app_backend.Mappers;
using sport_app_backend.Models.Account.Athlete;
using sport_app_backend.Models.Account.Coach;
using sport_app_backend.Models.Actions;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Question.A_Question;
using sport_app_backend.Models.SupportApp;
using sport_app_backend.Models.TrainingPlan;

namespace sport_app_backend.Repository;

public class UserRepository(
    ApplicationDbContext dbContext,
    ITokenService tokenService,
    ISmsService sms,
    IStorage Storage,
    IConfiguration config)
    : IUserRepository
{
    public async Task<ApiResponse> AddRoleGender(string phoneNumber, RoleGenderDto roleGenderDto)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
        if(roleGenderDto.Role is null) return new ApiResponse() { Message = "Role is null", Action = false };
        if(roleGenderDto.Gender is null) return new ApiResponse() { Message = "Gender is null", Action = false };

        var gender = Enum.Parse<Gender>(roleGenderDto.Gender.ToUpper());
        user.Gender = gender;
        user.FirstName = roleGenderDto.FirstName;
        user.LastName = roleGenderDto.LastName;
        

        switch (roleGenderDto.Role.ToUpper())
        {
            case "COACH":
            {
                user.TypeOfUser = TypeOfUser.COACH;
                var coach = new Coach()
                {
                    User = user,
                    UserId = user.Id,
                    PhoneNumber = user.PhoneNumber
                };
                user.Coach = coach;
                user.TypeOfUser = TypeOfUser.COACH;
                await dbContext.Coaches.AddAsync(user.Coach);
                await dbContext.SaveChangesAsync();
                await  AddTemplateProgramForNewCouch(user.Coach,user.Gender);
                return new ApiResponse()
                {
                    Message = "Coach added successfully",
                    Action = true,
                    Result = new AddRoleResponse()
                    {
                        RefreshToken = user.RefreshToken,
                        AccessToken = tokenService.CreateTokenForApp(new TokenUserDto()
                        {
                            CoachId = coach.Id,
                            Id =  user.Id,
                            PhoneNumber = user.PhoneNumber,
                            TypeOfUser = TypeOfUser.COACH
                        }),
                        TypeOfUser = user.TypeOfUser.ToString(),
                        Gender = user.Gender.ToString(),
                        Questions= true 
                    }
                };
            }
            case "ATHLETE":
                user.TypeOfUser = TypeOfUser.ATHLETE;
                user.Athlete = new Athlete
                {
                    User = user,
                    UserId = user.Id,
                    PhoneNumber = user.PhoneNumber
                };
                user.TypeOfUser = TypeOfUser.ATHLETE;
                await dbContext.Athletes.AddAsync(user.Athlete);
                await dbContext.SaveChangesAsync();
                return new ApiResponse()
                {
                    Message = "Athlete added successfully",
                    Action = true,
                    Result = new AddRoleResponse()
                    {
                        RefreshToken = user.RefreshToken,
                        AccessToken = tokenService.CreateTokenForApp(new TokenUserDto()
                        {
                            AthleteId = user.AthleteId,
                            Id =  user.Id,
                            PhoneNumber = user.PhoneNumber,
                            TypeOfUser = TypeOfUser.ATHLETE
                        }),                        TypeOfUser = user.TypeOfUser.ToString(),
                        Gender = user.Gender.ToString(),
                        Questions= true 
                        }
                };
            default:
                return new ApiResponse() { Message = "Invalid role", Action = false };
        }
    }

    private async Task AddTemplateProgramForNewCouch(Coach coach, Gender gender)
    {
        
        AthleteQuestion? question;
        if (gender == Gender.MALE)
        {
            question= await dbContext.AthleteQuestions.Include(aq=>aq.Athlete).FirstOrDefaultAsync(a=>a.Id==166);
        }
        else
        {
            question= await dbContext.AthleteQuestions.Include(aq=>aq.Athlete).FirstOrDefaultAsync(a=>a.Id==167);
        }
        
        if (question is null) return;
        
        
        var coachService = new CoachService
        {
            Coach = coach,
            Title = "برنامه ورزشی تستی",
            Description = "این یک برنامه ورزشی تستی هست",
            Price = 0,
            IsActive = false,
            IsDeleted = true
        };
        
        dbContext.CoachServices.Add(coachService);
        await dbContext.SaveChangesAsync();
        
        
        var payment = new Payment
        {
            Coach = coach,
            AthleteId = question.AthleteId,
            CoachId = coach.Id,
            CoachServiceId = coachService.Id,
            PaymentStatus = PaymentStatus.SUCCESS,
            Amount = 0,
            Authority = "nothing",
            AppFee = 0,
            AthleteQuestionId = question.Id,
                
        };
        
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(); 
        
        
        var workoutProgram = new WorkoutProgram
        {
            Title = coachService.Title,
            Coach = coach,
            AthleteId = question.AthleteId,
            CoachId = coach.Id,
            PaymentId = payment.Id,
            Status = WorkoutProgramStatus.NOTSTARTED,
        };
        
        dbContext.WorkoutPrograms.Add(workoutProgram);
        await dbContext.SaveChangesAsync();
    }


    public async Task<ApiResponse> CheckCode(CheckCodeRequestDto checkCodeRequestDto)
{
    var code = await dbContext.CodeVerifies.FirstOrDefaultAsync(x => x.PhoneNumber == checkCodeRequestDto.PhoneNumber);
    if (code == null)
    {
        return new ApiResponse { Action = false, Message = "CodeIsNotCorrect" };
    }
    

    
    if (code.TimeCodeSend.AddMinutes(15) < DateTime.Now)
    {
        dbContext.CodeVerifies.Remove(code);
        await dbContext.SaveChangesAsync();
        return new ApiResponse { Action = false, Message = "Code Expired" };
        
    }
    
    if (code.Code != checkCodeRequestDto.Code)
    {
        return new ApiResponse { Action = false, Message = "CodeIsNotCorrect" };
    }
    dbContext.CodeVerifies.Remove(code);
    await dbContext.SaveChangesAsync();
    
    var userEntity = await dbContext.Users.Include(u=>u.Athlete).Include(u=>u.Coach).FirstOrDefaultAsync(x => x.PhoneNumber == checkCodeRequestDto.PhoneNumber);
    if (userEntity != null)
    {
        var questions = userEntity.FirstName  is not "";
        
        userEntity.LastLogin = DateTime.Now;
        return await GenerateSuccessResponse(userEntity,questions);
    }
    
    var newUser = await CreateNewUser(checkCodeRequestDto.PhoneNumber);
    return await GenerateSuccessResponse(newUser,false);
}

private async Task<ApiResponse> GenerateSuccessResponse(User user,bool question)
{
    return new ApiResponse
    {
        Action = true,
        Message = "CodeIsCorrect",
        Result = new CheckCodeResponseDto
        {
            RefreshToken = await tokenService.CreateRefreshToken(user),
            AccessToken = tokenService.CreateTokenForApp(new TokenUserDto()
            {
                Id = user.Id,
                TypeOfUser = user.TypeOfUser,
                AthleteId = user.Athlete?.Id,
                CoachId = user.Coach?.Id,
                PhoneNumber = user.PhoneNumber,
                
                
            }),
            TypeOfUser = user.TypeOfUser.ToString(),
            Gender = user.Gender.ToString() ,
            Questions=question
        }
    };
}

private async Task<User> CreateNewUser(string phoneNumber)
{
    var newUser = new User
    {
        UserName =await GenerateUniqueUsername(),
        PhoneNumber = phoneNumber,
        TypeOfUser = TypeOfUser.NONE,
        LastLogin = DateTime.Now
    };
    
    await tokenService.CreateRefreshToken(newUser);
    await dbContext.Users.AddAsync(newUser);
    await dbContext.SaveChangesAsync();
    
    return newUser;
}

private async Task<string> GenerateUniqueUsername()
{
    string username;
    do
    {
        username = Guid.NewGuid().ToString("N").Substring(0, 8);
    } while (await dbContext.Users.AnyAsync(x => x.UserName == username));
    
    return username;
}



    public async Task<ApiResponse> Login(string userPhoneNumber)
    {
        var user = await dbContext.CodeVerifies.FirstOrDefaultAsync(x => x.PhoneNumber == userPhoneNumber);
        if (user is null)
        {
            await dbContext.CodeVerifies.AddAsync(new CodeVerify()
            {
                PhoneNumber = userPhoneNumber,
                Code = await sms.SendCode(userPhoneNumber),
                TimeCodeSend = DateTime.Now
            });
            await dbContext.SaveChangesAsync();


            return new ApiResponse()
            {
                Action = true,
                Message = "CodeIsSuccessFullySend"
            };
        }

        if (user.TimeCodeSend.AddMinutes(2) >= DateTime.Now)
            return new ApiResponse()
            {
                Action = false,
                Message = "you should wait 2 minutes"
            };
        dbContext.CodeVerifies.Remove(user);
        await dbContext.SaveChangesAsync();
        await dbContext.CodeVerifies.AddAsync(new CodeVerify()
        {
            PhoneNumber = userPhoneNumber,
            Code = await sms.SendCode(userPhoneNumber),
            TimeCodeSend = DateTime.Now
        });
        await dbContext.SaveChangesAsync();
        return new ApiResponse()
        {
            Action = true,
            Message = "CodeIsSuccessFullySend"
        };

    }

    public async Task<ApiResponse> GenerateAccessToken(string refreshToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.RefreshToken == refreshToken)
            .Select(u => new TokenUserDto
            {
                Id = u.Id,
                TypeOfUser = u.TypeOfUser,
                AthleteId = u.AthleteId, 
                CoachId = u.CoachId,
                PhoneNumber = u.PhoneNumber,
                LastLogin = u.LastLogin,
                
            })
            .FirstOrDefaultAsync();

        if (user is null) return new ApiResponse() { Message = "Invalid refresh token", Action = false };
        return user.LastLogin.AddDays(180) < DateTime.Now ? new ApiResponse() { Message = "Refresh token expired", Action = false } : new ApiResponse() { Message = "Success", Action = true, Result = new { AccessToken = tokenService.CreateTokenForApp(user) } };
    }

    public async Task<ApiResponse> AddUsername(string phoneNumber, string username)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
        var checkUserNameIsUniq = await dbContext.Users.FirstOrDefaultAsync(x => x.UserName == username);
        if (checkUserNameIsUniq is not null&& checkUserNameIsUniq!=user)
        {
            return new ApiResponse()
            {
                Action = false,
                Message = "Username is already taken"
            };
        }
        user.UserName = username;
        await dbContext.SaveChangesAsync();
        return new ApiResponse() { Message = "Success", Action = true };
    }
    public async Task<ApiResponse> EditUserProfile(string phoneNumber, EditUserProfileDto editUserProfileDto)
    {
        var user= await dbContext.Users.Include(q=>q.Coach).FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
        var findUserName= await dbContext.Users.FirstOrDefaultAsync(x => x.UserName == editUserProfileDto.UserName);
        if(findUserName is not null&& findUserName!=user) return new ApiResponse() { Message = "Username already exists", Action = false };// Ensure the user is an athlete
        user.UserName = editUserProfileDto.UserName; user.FirstName = editUserProfileDto.FirstName;
        user.LastName = editUserProfileDto.LastName;
        user.BirthDate = Convert.ToDateTime(editUserProfileDto.BirthDate);
       
        await dbContext.SaveChangesAsync();
        return new ApiResponse()
        {
            Message = "user profile edited successfully",
            Action = true
        };
    }

    public async  Task<ApiResponse> GetUserProfileForEdit(string phoneNumber)
    {
        var user= await dbContext.Users.Include(q=>q.Coach).FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
        
       

        return new ApiResponse()
        {
            Message = "user profile fetched successfully",
            Action = true,
            Result = new
            {
                user.UserName,
                user.FirstName,
                user.LastName,
                user.BirthDate,
                user.ImageProfile,
                user.PhoneNumber,
            }
        };
    }

    public async Task<ApiResponse> Logout(string phoneNumber)
    { var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
        user.RefreshToken = null;
        await dbContext.SaveChangesAsync();
        return new ApiResponse() { Message = "Success", Action = true };
    }

    public async Task<ApiResponse> AppSupport(string phoneNumber, ReportAppDto reportAppDto)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
        await dbContext.SupportApp.AddAsync(new SupportApp()
        {   User = user,
            UserId = user.Id,
            Category = Enum.Parse<SupportAppCategory>(reportAppDto.Category),
            Description = reportAppDto.Description,
           // CreateDate =  DateTime.Now,
        });
        await dbContext.SaveChangesAsync();
        return new ApiResponse() { Message = "Success", Action = true };
        
    }
    public async Task<ApiResponse> GetAllExercise()
    {
            var exercisesDto = await dbContext.Exercises
                .AsNoTracking() 
                .OrderByDescending(x => x.Views)
                .Select(x => x.ToAllExerciseResponseDto()) 
                .ToListAsync();
        return new ApiResponse()
        {
            Message = "Exercises found",
            Action = true,
            Result = exercisesDto
        };
    }


    public Task<ApiResponse> GetExercise(int exerciseId)
    {
        var exercise = dbContext.Exercises.FirstOrDefault(x => x.Id == exerciseId);
        if (exercise is null) return Task.FromResult(new ApiResponse() { Message = "Exercise not found", Action = false });
        return Task.FromResult(new ApiResponse()
            { Message = "Success", Action = true, Result = exercise.ToExerciseDto() });
    }

    public async Task<ApiResponse> RemoveProfilePhoto(string phoneNumber)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
        var img = user.ImageProfile;
        if (img=="") return new ApiResponse() { Message = "now img found", Action = false };
        var response = await Storage.RemovePhoto(img);
        if (!response.Action) return response;
        user.ImageProfile = "";
        await dbContext.SaveChangesAsync();
        return response;
    }

    public async Task<ApiResponse> SaveImageAsync(string phoneNumber, IFormFile image)
    {
        
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
        if (image.Length <= 0) return new ApiResponse() { Message = "image not receive", Action = false }; ;

        var response = await Storage.UploadImage(image, user.ImageProfile,"profileImage");
        if (!response.Action) return response;
        if (response.Result is not null)
        {
            user.ImageProfile = response.Result.ToString() ?? string.Empty;
        }

        await dbContext.SaveChangesAsync();

        return new ApiResponse()
        {
            Action = true,
            Message = "image upload successfully",
            Result = new
            {
                ImageUrl = user.ImageProfile
            }
        };
    }
    public async Task<ApiResponse> CreateWorkoutPdfAsync(string wpId)
    {
        var id = tokenService.DecodeHash(wpId);
    // --- ۱. واکشی داده‌های خام از دیتابیس ---
    var workoutData = await dbContext.WorkoutPrograms
        .AsNoTracking()
        .Where(wp => wp.Id == id)
        .Select(wp => new // واکشی به یک شیء بی‌نام
        {
            wp.Title,
            wp.StartDate,
            CoachFirstName = wp.Coach.User.FirstName,
            CoachLastName = wp.Coach.User.LastName,
            AthleteCurrentBodyForm = wp.Payment.AthleteQuestion.CurrentBodyForm,
            wp.ProgramLevel,
            wp.ProgramDuration,
            wp.ProgramPriorities, // <-- واکشی لیست خام Enum ها
            AthleteCurrentWeight = wp.Athlete.CurrentWeight,
            AthleteHeight = wp.Athlete.Height,
            AhtleteGender= wp.Athlete.User.Gender,
            ProgramInDays = wp.ProgramInDays.Select(pd => new 
            {
                pd.ForWhichDay,
                Exercises = pd.AllExerciseInDays.Select(se => new 
                {   se.Exercise.Id,
                    se.Exercise.PersianName,
                    se.RepsJson,
                    se.Description,
                    se.RepType
                }).ToList()
            }).ToList()
        })
        .FirstOrDefaultAsync();
        
    if (workoutData == null) return null;


    var heightInMeters = workoutData.AthleteHeight / 100.0;

    var bmi = workoutData.AthleteCurrentWeight / (heightInMeters * heightInMeters);
    var pc = new PersianCalendar();
    


    var pdfModel = new WorkoutPdfModel
    {
        ProgramTitle = workoutData.Title,
        StartDate = workoutData.StartDate?.ToShamsiDateString()!,
        CoachName = $"{workoutData.CoachFirstName} {workoutData.CoachLastName}",
        ProgramLevel = workoutData.ProgramLevel.ToPersianString(),
        ProgramDuration = workoutData.ProgramDuration.ToString() ,
        ProgramPriorities = string.Join(" - ", workoutData.ProgramPriorities.Select(p => p.ToPersianString())),
        AthleteWeight = workoutData.AthleteCurrentWeight.ToString(),
        AthleteHeight = workoutData.AthleteHeight.ToString(),
        AthleteBmi = Math.Round(bmi, 2).ToString(), 
        AthleteFatPercent = workoutData.AhtleteGender.GetFatPercentRange(workoutData.AthleteCurrentBodyForm),
        WorkoutDays = workoutData.ProgramInDays.Select(pd => new WorkoutDayModel
        {
            DayNumber = pd.ForWhichDay,
            Exercises = pd.Exercises.Select(se => new ExerciseModel
            {
                Name = se.PersianName,
                Reps = se.RepsJson.ToReps(),
                Description = se.Description,
                RepType = se.RepType.ToString(),
            }).ToList()
        }).ToList()
    };
    return new ApiResponse()
    {
        Action = true,
        Message = "get program",
        Result = pdfModel
    };


}

    public async Task<ApiResponse> CheckQuestionSubmitted(string phoneNumber)
    {
        var user = await dbContext.Users.AsNoTracking()
            .Where(u => u.PhoneNumber == phoneNumber)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Gender,
                u.ImageProfile,
                u.TypeOfUser,
                u.PhoneNumber
            })
            .FirstOrDefaultAsync();
      
        if (user == null)
            return new ApiResponse()
            {
                Action = false,
                Message = "Phone number doesn't exist"
            };
        var hasUnreadMessage = await dbContext.InAppMessages
            .Where(m => m.TargetRole == user.TypeOfUser)
            .AnyAsync(m => !dbContext.UserMessageStatuses
                .Any(s => s.UserId == user.Id && s.InAppMessageId == m.Id && s.IsRead));
        if (user.TypeOfUser != TypeOfUser.COACH)
            return new ApiResponse()
            {
                Action = true,
                Message = "find user",
                Result = new
                {
                    user.FirstName,
                    user.LastName,
                    user.ImageProfile,
                    TypeOfUser = user.TypeOfUser.ToString(),
                    user.PhoneNumber,
                    Gender = user.Gender.ToString(),
                    Question = user.FirstName != "",
                    hasUnreadMessage
                }
            };
        var couchId =await  dbContext.Coaches.AsNoTracking().Where(c => c.PhoneNumber == phoneNumber).Select(c=>c.Id).FirstOrDefaultAsync();
        var numberOfFeedBack =  dbContext.WorkoutProgramFeedback.Count(e => e.CouchId == couchId);


        return new ApiResponse()
        {
            Action = true,
            Message = "find user",
            Result = new
            {
                user.FirstName,
                user.LastName,
                user.ImageProfile,
                TypeOfUser = user.TypeOfUser.ToString(),
                user.PhoneNumber,
                Gender = user.Gender.ToString(),
                Question = user.FirstName != "",
                hasUnreadMessage,
                numberOfFeedBack


            }
        };


    }

  

public async Task<(IEnumerable<AllExerciseResponseDto> Exercises, int TotalCount)> GetExercisesAsync(
    string? level,
    string? type,
    string? mechanic,
    string?[]? equipment,
    string? muscle,
    string? place,
    int page,
    int pageSize,
    string? searchTerm)
{
    page = page <= 0 ? 1 : page;
    pageSize = pageSize <= 0 ? 10 : pageSize;

    IQueryable<Exercise> query = dbContext.Exercises.AsNoTracking();

    // Search
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        searchTerm = searchTerm.Trim();
        query = query.Where(e =>
            EF.Functions.Like(e.PersianName!, $"%{searchTerm}%") ||
            EF.Functions.Like(e.EnglishName!, $"%{searchTerm}%"));
    }

    // Level
    if (!string.IsNullOrWhiteSpace(level) &&
        Enum.TryParse<ExerciseLevel>(level, true, out var levelEnum))
    {
        query = query.Where(e => e.ExerciseLevel == levelEnum);
    }

    if (!string.IsNullOrWhiteSpace(type) &&
        Enum.TryParse<ExerciseType>(type, true, out var typeEnum))
    {
        query = query.Where(e => e.ExerciseType == typeEnum);
    }

    if (!string.IsNullOrWhiteSpace(mechanic) &&
        Enum.TryParse<MechanicType>(mechanic, true, out var mechanicEnum))
    {
        query = query.Where(e => e.Mechanics == mechanicEnum);
    }

    if (equipment != null && equipment.Length > 0)
    {
        var validEquipments = equipment
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e =>
            {
                bool parsed = Enum.TryParse<EquipmentType>(e, true, out var result);
                return new { parsed, result };
            })
            .Where(x => x.parsed)
            .Select(x => x.result)
            .ToList();

        if (validEquipments.Count > 0)
            query = query.Where(e => validEquipments.Contains(e.Equipment));
    }

    if (!string.IsNullOrWhiteSpace(muscle) &&
        Enum.TryParse<BaseCategory>(muscle, true, out var muscleEnum))
    {
        query = query.Where(e => e.BaseCategory == muscleEnum);
    }

    if (!string.IsNullOrWhiteSpace(place))
    {
        place = place.Trim();
        query = query.Where(e => EF.Functions.Like(e.Description!, $"%{place}%"));
    }

    var totalCount = await query.CountAsync();

    var exercises = await query
        .OrderByDescending(e => e.Views)
        .ThenBy(e => e.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(e => new AllExerciseResponseDto
        {
            Id = e.Id,
            Name = e.PersianName,
            ImageLink = e.ImageLink,
            BaseCategory = e.BaseCategory.ToString(),
            Equipment = e.Equipment.ToString(),
            ExerciseType = e.ExerciseType.ToString(),
            Level = e.ExerciseLevel.ToString(),
            Mechanics = e.Mechanics.ToString(),
            View = e.Views,
            Met = e.Met
        })
        .ToListAsync();

    return (exercises, totalCount);
}

}

