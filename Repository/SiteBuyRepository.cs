using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Login_Sinup;

namespace sport_app_backend.Repository;

public class SiteBuyRepository(
    ApplicationDbContext dbContext,
    ITokenService tokenService,
    ISmsService sms,
    ILiaraStorage liaraStorage,
    IConfiguration config)
    : ISiteBuyRepository
{
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

    public async Task<ApiResponse> CheckCode(CheckCodeRequestDto checkCodeRequestDto)
    {
        var user = await dbContext.CodeVerifies.FirstOrDefaultAsync(x =>
            x.PhoneNumber == checkCodeRequestDto.PhoneNumber);
        if (user == null)
        {
            return new ApiResponse { Action = false, Message = "CodeIsNotCorrect" };
        }



        if (user.TimeCodeSend.AddMinutes(15) < DateTime.Now)
        {
            dbContext.CodeVerifies.Remove(user);
            await dbContext.SaveChangesAsync();
            return new ApiResponse { Action = false, Message = "Code Expired" };

        }

        if (user.Code != checkCodeRequestDto.Code)
        {
            return new ApiResponse { Action = false, Message = "CodeIsNotCorrect" };
        }

        dbContext.CodeVerifies.Remove(user);
        await dbContext.SaveChangesAsync();

        var userEntity =
            await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == checkCodeRequestDto.PhoneNumber);
        if (userEntity != null)
        {
            var questions = userEntity.FirstName is not null;

            userEntity.LastLogin = DateTime.Now;
            return await GenerateSuccessResponse(userEntity, questions);
        }

        var newUser = await CreateNewAthlete(checkCodeRequestDto.PhoneNumber);
        return await GenerateSuccessResponse(newUser, false);
    }

    private async Task<ApiResponse> GenerateSuccessResponse(User user, bool question)
    {
        return new ApiResponse
        {
            Action = true,
            Message = "CodeIsCorrect",
            Result = new CheckCodeResponseDto
            {
                RefreshToken = await tokenService.CreateRefreshToken(user),
                AccessToken = tokenService.CreateToken(user),
                TypeOfUser = user.TypeOfUser.ToString(),
                Gender = user.Gender.ToString(),
                Questions = question
            }
        };
    }

    private async Task<User> CreateNewAthlete(string phoneNumber)
    {
        // ۱. ساخت شیء User
        var newUser = new User
        {
            UserName = await GenerateUniqueUsername(),
            PhoneNumber = phoneNumber,
            TypeOfUser = TypeOfUser.ATHLETE,
            LastLogin = DateTime.Now
        };

        
        newUser.Athlete = new Athlete()
        {
            User = newUser,
            PhoneNumber = phoneNumber
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

}
