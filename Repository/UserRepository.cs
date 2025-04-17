using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Login_Sinup;
using Amazon.S3;
using Amazon.S3.Model;

namespace sport_app_backend.Repository;

public class UserRepository(
    ApplicationDbContext dbContext,
    ITokenService tokenService,
    ISendVerifyCodeService sendVerifyCode)
    : IUserRepository
{
    public async Task<ApiResponse> AddRoleGender(string phoneNumber, RoleGenderDto roleGenderDto)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
        if(roleGenderDto.Role is null) return new ApiResponse() { Message = "Role is null", Action = false };
        if(roleGenderDto.Gender is null) return new ApiResponse() { Message = "Gender is null", Action = false };

        var sportEnum = Enum.Parse<Gender>(roleGenderDto.Gender.ToUpper());
        user.Gender = sportEnum;
        

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
                return new ApiResponse()
                {
                    Message = "Coach added successfully",
                    Action = true,
                    Result = new AddRoleResponse()
                    {
                        RefreshToken = user.RefreshToken,
                        AccessToken = tokenService.CreateToken(user),
                        TypeOfUser = user.TypeOfUser.ToString(),
                        Gender = user.Gender.ToString(),
                        Questions= false 
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
                        AccessToken = tokenService.CreateToken(user),
                        TypeOfUser = user.TypeOfUser.ToString(),
                        Gender = user.Gender.ToString(),
                        Questions= false 
                        }
                };
            default:
                return new ApiResponse() { Message = "Invalid role", Action = false };
        }
    }


    public async Task<ApiResponse> CheckCode(CheckCodeRequestDto checkCodeRequestDto)
{
    var user = await dbContext.CodeVerifies.FirstOrDefaultAsync(x => x.PhoneNumber == checkCodeRequestDto.PhoneNumber);
    if (user == null)
    {
        return new ApiResponse { Action = false, Message = "CodeIsNotCorrect" };
    }
    
    dbContext.CodeVerifies.Remove(user);
    await dbContext.SaveChangesAsync();
    
    if (user.TimeCodeSend.AddMinutes(15) < DateTime.Now)
    {
        return new ApiResponse { Action = false, Message = "Code Expired" };
    }
    
    if (user.Code != checkCodeRequestDto.Code)
    {
        return new ApiResponse { Action = false, Message = "CodeIsNotCorrect" };
    }
    
    var userEntity = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == checkCodeRequestDto.PhoneNumber);
    if (userEntity != null)
    {
        var questions = userEntity.FirstName is not null;
        
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
            AccessToken = tokenService.CreateToken(user),
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
                Code = await sendVerifyCode.SendCode(userPhoneNumber),
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
            Code = await sendVerifyCode.SendCode(userPhoneNumber),
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
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);
        if (user is null) return new ApiResponse() { Message = "Invalid refresh token", Action = false };
        return user.RefreshTokeNExpire < DateTime.Now ? new ApiResponse() { Message = "Refresh token expired", Action = false } : new ApiResponse() { Message = "Success", Action = true, Result = new { AccessToken = tokenService.CreateToken(user) } };
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
        user.Bio = editUserProfileDto.Bio;
        if(user.Coach is not null) user.Coach.HeadLine = editUserProfileDto.HeadLine;
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
                user.Bio,
                user.Coach?.HeadLine
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

    public async Task<ApiResponse> ReportApp(string phoneNumber, ReportAppDto reportAppDto)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
        await dbContext.ReportApps.AddAsync(new ReportApp()
        {   User = user,
            UserId = user.Id,
            Category = reportAppDto.Category,
            Description = reportAppDto.Description
        });
        await dbContext.SaveChangesAsync();
        return new ApiResponse() { Message = "Success", Action = true };
        
    }

    public async Task<ApiResponse> SaveImageAsync(string phoneNumber, IFormFile image)
    {
        
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
        const string accessKey = "kn8vqc4tvn5d36ol";
        const string secretKey = "6a05ec26-f42a-4c26-84a1-ad5d93236b63";
        const string bucketName = "charset7";
        const string endpoint = "https://storage.c2.liara.space";
        if (image.Length <= 0) return new ApiResponse() { Message = "image not receive", Action = false }; ;
        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            SignatureVersion = "4"
        };

        var credentials = new Amazon.Runtime.BasicAWSCredentials(
            accessKey,
            secretKey
        );

        using var client = new AmazonS3Client(credentials, config);

        var extension = Path.GetExtension(image.FileName);
        var objectKey = $"{Guid.NewGuid()}{extension}";

        try
        {
            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream).ConfigureAwait(false);
            memoryStream.Position = 0;

            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                InputStream = memoryStream,
                ContentType = image.ContentType // اضافه برای بهتر بودن متادیتا
            };

            await client.PutObjectAsync(request);

            var fileUrl = $"{endpoint}/{bucketName}/{objectKey}";
            if (user.ImageProfile.Length > 10)
            {
                await DeleteObjectAsync(client, user.ImageProfile);
            }

            user.ImageProfile = fileUrl;
            await dbContext.SaveChangesAsync(); 

        }
        catch (AmazonS3Exception e)
        {

            return new ApiResponse()
            {
                Action = false,
                Message = $"Error uploading to S3: {e.Message}",
                
            };
        }

       
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

    private static async Task DeleteObjectAsync(IAmazonS3 client, string url)
    {
       var uri = new Uri(url);
        var segments = uri.AbsolutePath.TrimStart('/').Split('/', 2);

        if (segments.Length < 2)
            throw new ArgumentException("URL does not contain a valid bucket and object key");
        var bucketName = segments[0];
        var objectKey = segments[1];

        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            await client.DeleteObjectAsync(deleteRequest);
            Console.WriteLine($"File '{objectKey}' deleted successfully.");
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
    }

}
