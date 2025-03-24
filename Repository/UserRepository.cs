using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Login_Sinup;

namespace sport_app_backend.Repository;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ISendVerifyCodeService _sendVerifyCode;
    private readonly ITokenService _tokenService;

    public UserRepository(ApplicationDbContext dbContext, ITokenService tokenService, ISendVerifyCodeService sendVerifyCode)
    {
        _context = dbContext;
        _sendVerifyCode = sendVerifyCode;
        _tokenService = tokenService;

    }


    public async Task<ApiResponse> AddRoleGender(string phoneNumber, RoleGenderDto roleGenderDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (user is null) return new ApiResponse() { Message = "User not found", Action = false };
        if(roleGenderDto.Role is null) return new ApiResponse() { Message = "Role is null", Action = false };
        if(roleGenderDto.Gender is null) return new ApiResponse() { Message = "Gender is null", Action = false };

        var sportEnum = Enum.Parse<Gender>(roleGenderDto.Gender);
        user.Gender = sportEnum;
        

        if (roleGenderDto.Role.Equals("Coach"))
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
            await _context.Coaches.AddAsync(user.Coach);
            await _context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Coach added successfully",
                Action = true,
                Result = new AddRoleResponse()
                {
                    RefreshToken = user.RefreshToken,
                    AccessToken = _tokenService.CreateToken(user),
                    TypeOfUser = user.TypeOfUser.ToString()
                }
            };

        }
        else if (roleGenderDto.Gender.Equals("Athlete"))
        {
            user.TypeOfUser = TypeOfUser.ATHLETE;
            user.Athlete = new Athlete
            {
                User = user,
                UserId = user.Id,
                PhoneNumber = user.PhoneNumber
            };
            user.TypeOfUser = TypeOfUser.ATHLETE;
            await _context.Athletes.AddAsync(user.Athlete);
            await _context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Athlete added successfully",
                Action = true,
                Result = new AddRoleResponse()
                {
                    RefreshToken = user.RefreshToken,
                    AccessToken = _tokenService.CreateToken(user),
                    TypeOfUser = user.TypeOfUser.ToString()
                }
            };
        }
        else
        {
            return new ApiResponse() { Message = "Invalid role", Action = false };
        }
    }

    public async Task<ApiResponse> CheckCode(CheckCodeRequestDto checkCodeRequestDto)
    {
        var user = await _context.CodeVerifies.FirstOrDefaultAsync(x => x.PhoneNumber == checkCodeRequestDto.PhoneNumber);

        if (user != null)
        {
            _context.CodeVerifies.Remove(user);
            await _context.SaveChangesAsync();
            if (user.TimeCodeSend.AddMinutes(15) < DateTime.Now)
            {
                return new ApiResponse()
                {
                    Action = false,
                    Message = "Code Expired"
                };
            }
            else
            {
                if (user.Code == checkCodeRequestDto.Code)
                {
                    var userEntity = await _context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == checkCodeRequestDto.PhoneNumber);
                    if (userEntity != null)
                    {
                        userEntity.LastLogin = DateTime.Now;
                        return new ApiResponse()
                        {
                            Action = true,
                            Message = "CodeIsCorrect",
                            Result = new CheckCodeResponseDto()
                            {

                                RefreshToken = userEntity.RefreshToken,
                                AccessToken = _tokenService.CreateToken(userEntity),
                                TypeOfUser = userEntity.TypeOfUser.ToString()
                            }
                        };
                    }
                    else
                    {   // create new user
                        var newUser = new User()
                        {
                            PhoneNumber = checkCodeRequestDto.PhoneNumber,
                            TypeOfUser = TypeOfUser.NONE,
                        };
                        newUser.LastLogin = DateTime.Now;
                        _tokenService.CreateRefreshToken(newUser);
                        var result = await _context.Users.AddAsync(newUser);
                        var result2 = await _context.SaveChangesAsync();


                        return new ApiResponse()
                        {
                            Action = true,
                            Message = "CodeIsCorrect",
                            Result = new CheckCodeResponseDto()
                            {
                                RefreshToken = newUser.RefreshToken,
                                AccessToken = _tokenService.CreateToken(newUser),
                                TypeOfUser = newUser.TypeOfUser.ToString()
                            }
                        };
                    }


                }
            }

            return new ApiResponse()
            {
                Action = false,
                Message = "CodeIsNotCorrect"
            };
        }

        return new ApiResponse()
        {
            Action = false,
            Message = "CodeIsNotCorrect"
        };
    }


    public async Task<ApiResponse> Login(string UserPhoneNumber)
    {
        var user = await _context.CodeVerifies.FirstOrDefaultAsync(x => x.PhoneNumber == UserPhoneNumber);
        if (user is null)
        {
            await _context.CodeVerifies.AddAsync(new CodeVerify()
            {
                PhoneNumber = UserPhoneNumber,
                Code = await _sendVerifyCode.SendCode(UserPhoneNumber),
                TimeCodeSend = DateTime.Now
            });
            await _context.SaveChangesAsync();


            return new ApiResponse()
            {
                Action = true,
                Message = "CodeIsSuccessFullySend"
            };
        }
        else
        {
            if (user.TimeCodeSend.AddMinutes(2) < DateTime.Now)
            {
                _context.CodeVerifies.Remove(user);
                await _context.SaveChangesAsync();
                await _context.CodeVerifies.AddAsync(new CodeVerify()
                {
                    PhoneNumber = UserPhoneNumber,
                    Code = await _sendVerifyCode.SendCode(UserPhoneNumber),
                    TimeCodeSend = DateTime.Now
                });
                await _context.SaveChangesAsync();
                return new ApiResponse()
                {
                    Action = true,
                    Message = "CodeIsSuccessFullySend"
                };
            }
            else
            {
                return new ApiResponse()
                {
                    Action = false,
                    Message = "you should wait 2 minutes"
                };

            }
        }
    }

    public async Task<ApiResponse> GenerateAccessToken(string refreshToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);
        if (user is null) return new ApiResponse() { Message = "Invalid refresh token", Action = false };
        if (user.RefreshTokeNExpire < DateTime.Now) return new ApiResponse() { Message = "Refresh token expired", Action = false };
        return new ApiResponse() { Message = "Success", Action = true, Result = new { AccessToken = _tokenService.CreateToken(user) } };
    }

}
