using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ZarinPal;
using sport_app_backend.Dtos.ZarinPal.Verify;
using sport_app_backend.Interface;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Login_Sinup;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Question.A_Question;

namespace sport_app_backend.Repository;

public class BuyFromSiteRepository(
    ApplicationDbContext dbContext,
    ITokenService tokenService,
    ISmsService sms,
    ILiaraStorage liaraStorage,
    IZarinPal zarinPal,
    IConfiguration config)
    : IBuyFromSiteRepository
{
    public async Task<ApiResponse> GenerateAccessToken(string refreshToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.SiteRefreshToken == refreshToken);
        if (user is null) return new ApiResponse() { Message = "Invalid refresh token", Action = false };
        return user.LastLoginSite.AddDays(90) < DateTime.Now ? new ApiResponse() { Message = "Refresh token expired", Action = false } : new ApiResponse() { Message = "Success", Action = true, Result = new { AccessToken = tokenService.CreateToken(user) } };
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
                AthleteFirstName = wp.Athlete.User.FirstName,
                AthleteLastName = wp.Athlete.User.LastName,
                AthleteCurrentBodyForm = wp.Payment.AthleteQuestion.CurrentBodyForm,
                wp.ProgramLevel,
                wp.ProgramDuration,
                wp.ProgramPriorities, // <-- واکشی لیست خام Enum ها
                AthleteCurrentWeight = wp.Payment.AthleteQuestion.Weight,
                AthleteHeight = wp.Athlete.Height,
                AhtleteGender = wp.Athlete.User.Gender,
                ProgramInDays = wp.ProgramInDays.Select(pd => new
                {
                    pd.ForWhichDay,
                    Exercises = pd.AllExerciseInDays.Select(se => new
                    {
                        se.Exercise.Id,
                        se.Exercise.PersianName,
                        se.RepType,
                        se.Description,
                        se.RepsJson,
                        se.Exercise.Slug
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
            StartDate = workoutData.StartDate.ToShamsiDateString(),
            CoachName = $"{workoutData.CoachFirstName} {workoutData.CoachLastName}",
            ProgramLevel = workoutData.ProgramLevel.ToPersianString(),
            ProgramDuration = workoutData.ProgramDuration.ToString(),
            ProgramPriorities = string.Join(" - ", workoutData.ProgramPriorities.Select(p => p.ToPersianString())),
            AthleteWeight = workoutData.AthleteCurrentWeight.ToString(),
            AthleteHeight = workoutData.AthleteHeight.ToString(),
            AthleteBmi = Math.Round(bmi, 2).ToString(),
            AthleteName=$"{workoutData.AthleteFirstName} {workoutData.AthleteLastName}",
            AthleteFatPercentage = workoutData.AhtleteGender.GetFatPercentageRange(workoutData.AthleteCurrentBodyForm),
            WorkoutDays = workoutData.ProgramInDays.Select(pd => new WorkoutDayModel
            {
                DayNumber = pd.ForWhichDay,
                Exercises = pd.Exercises.Select(se => new ExerciseModel
                {
                    Name = se.PersianName,
                    Reps = se.RepsJson.ToReps(),
                    Description = se.Description,
                    RepType = se.RepType.ToString(),
                    slug = se.Slug
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
    public async Task<ApiResponse> UploadImageForAthleteQuestion(string wpKey, int id, string sideName, IFormFile file)
    {
        var workoutProgramId = tokenService.DecodeHash(wpKey);
        if (workoutProgramId == 0)
        {
            return new ApiResponse { Action = false, Message = "برنامه شما پیدا نشد" };
        }

        var athleteId = await dbContext.WorkoutPrograms.AsNoTracking().Where(wp => wp.Id == workoutProgramId)
            .Select(wp => wp.AthleteId).FirstOrDefaultAsync();
        if (athleteId == 0)
        {
            return new ApiResponse
            {
                Message = "athlete not found",
                Action = false
            };
        }

        if (id != 0)
        {
            var athleteImage = await dbContext.AthleteImage.Where(ai => ai.Id == id && ai.AthleteId == athleteId)
                .FirstOrDefaultAsync();
            if (athleteImage is null)
            {
                return new ApiResponse
                {
                    Message = "AthleteBodyImage not found",
                    Action = false
                };
            }

            switch (sideName)
            {
                case "front":
                {
                    var frontLink = athleteImage.FrontLink;
                    if (frontLink is { Length: > 1 })
                    {
                        await liaraStorage.RemovePhoto(frontLink);
                    }

                    var response = await liaraStorage.UploadImage(file, "");
                    if (response.Action)
                    {
                        athleteImage.FrontLink = response.Result as string;
                    }
                    else
                    {
                        return response;
                    }

                    break;
                }
                case "back":
                {
                    var frontLink = athleteImage.BackLink;
                    if (frontLink is { Length: > 1 })
                    {
                        await liaraStorage.RemovePhoto(frontLink);
                    }

                    var response = await liaraStorage.UploadImage(file, "");
                    if (response.Action)
                    {
                        athleteImage.BackLink = response.Result as string;
                    }
                    else
                    {
                        return response;
                    }

                    break;
                }
                case "side":
                {
                    var frontLink = athleteImage.SideLink;
                    if (frontLink is { Length: > 1 })
                    {
                        await liaraStorage.RemovePhoto(frontLink);
                    }

                    var response = await liaraStorage.UploadImage(file, "");
                    if (response.Action)
                    {
                        athleteImage.SideLink = response.Result as string;
                    }
                    else
                    {
                        return response;
                    }

                    break;
                }
            }

            await dbContext.SaveChangesAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "img upload successfully",
                Result = athleteImage
            };
        }
        else
        {
            var response = await liaraStorage.UploadImage(file, "");
            if (!response.Action)
            {
                return response;
            }

            var imageUrl = response.Result as string;

            var athleteImage = new AthleteBodyImage();
            switch (sideName)

            {
                case "back":
                {
                    athleteImage.BackLink = imageUrl;
                    break;
                }
                case "front":
                {
                    athleteImage.FrontLink = imageUrl;
                    break;
                }
                case "side":
                {
                    athleteImage.SideLink = imageUrl;
                    break;
                }
            }

            athleteImage.AthleteId = athleteId;

            await dbContext.AthleteImage.AddAsync(athleteImage);
            await dbContext.SaveChangesAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "img upload successfully",
                Result = athleteImage.ToAthleteBodyImageDto()
            };
        }
    }
    public async Task<ApiResponse> RemoveImageForAthleteQuestion(string wpKey,int id, string sideName)
        {
            var workoutProgramId = tokenService.DecodeHash(wpKey);
            if (workoutProgramId == 0)
            {
                return new ApiResponse { Action = false, Message = "برنامه شما پیدا نشد" };
            }

            var athleteId = await dbContext.WorkoutPrograms.AsNoTracking().Where(wp => wp.Id == workoutProgramId)
                .Select(wp => wp.AthleteId).FirstOrDefaultAsync();
            var athleteImage = await dbContext.AthleteImage.Where(ai => ai.Id == id&&ai.AthleteId==athleteId).FirstOrDefaultAsync();
            if (athleteImage is null)
            {
                return new ApiResponse
                {
                    Message = "AthleteBodyImage not found",
                    Action = false
                };
            }

         
          
            switch (sideName)
            {
                case "front":
                {
                    var frontLink = athleteImage.FrontLink;
                    if (frontLink is { Length: > 1 })
                    {
                        var removeResponse = await liaraStorage.RemovePhoto(frontLink);
                        if (!removeResponse.Action)
                        {
                            return removeResponse;
                        }

                        athleteImage.FrontLink = "";
                    }
                    else
                    {
                        return new ApiResponse()
                        {
                            Action = false,
                            Message = "no image Found"
                        };
                    }
                    break;
                }
                case "back":
                {
                    var backLink = athleteImage.BackLink;
                    if (backLink is { Length: > 1 })
                    {
                        var removeResponse = await liaraStorage.RemovePhoto(backLink);
                        if (!removeResponse.Action)
                        {
                            return removeResponse;
                        }

                        athleteImage.BackLink = "";
                    }
                    else
                    {
                        return new ApiResponse()
                        {
                            Action = false,
                            Message = "no image Found"
                        };
                    }
                    break;
                }
                case "side":
                {
                    var sideLink = athleteImage.SideLink;
                    if (sideLink is { Length: > 1 })
                    {
                        var removeResponse = await liaraStorage.RemovePhoto(sideLink);
                        if (!removeResponse.Action)
                        {
                            return removeResponse;
                        }

                        athleteImage.SideLink = "";
                    }
                    else
                    {
                        return new ApiResponse()
                        {
                            Action = false,
                            Message = "no image Found"
                        };
                    }
                    break;
                }
            }
            await dbContext.SaveChangesAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "img remove successfully",
                Result = athleteImage.ToAthleteBodyImageDto()
            }; 

            
            
        }

    public async Task<ApiResponse> GetImageForAthleteQuestion(string wPkey,int id)
        {
            var workoutProgramId = tokenService.DecodeHash(wPkey);
            if (workoutProgramId == 0)
            {
                return new ApiResponse { Action = false, Message = "برنامه شما پیدا نشد" };
            }

            var athleteId = await dbContext.WorkoutPrograms.AsNoTracking().Where(wp => wp.Id == workoutProgramId)
                .Select(wp => wp.AthleteId).FirstOrDefaultAsync();

            var athleteImage = await dbContext.AthleteImage.Where(ai => ai.Id == id&&ai.AthleteId==athleteId).FirstOrDefaultAsync();
            if (athleteImage is null)
            {
                return new ApiResponse
                {
                    Message = "AthleteBodyImage not found",
                    Action = false
                };
            }
            return new ApiResponse()
            {
                Action = true,
                Message = "img remove successfully",
                Result = athleteImage.ToAthleteBodyImageDto()
            }; 
        }
    public Task<ApiResponse> GetExercise(int exerciseId)
    {
        var exercise = dbContext.Exercises.FirstOrDefault(x => x.Id == exerciseId);
        if (exercise is null)
            return Task.FromResult(new ApiResponse() { Message = "Exercise not found", Action = false });
        return Task.FromResult(new ApiResponse()
            { Message = "Success", Action = true, Result = exercise.ToExerciseDto() });
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

    public async Task<ApiResponse> CheckCode(CheckCodeRequestFromBuyFromSiteDto checkCodeRequestDto)
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
        if (userEntity is null)
        {
            var newAthleteUser = await CreateNewAthleteUser(checkCodeRequestDto.PhoneNumber);
            return await GenerateSuccessResponse(newAthleteUser);
        }

        if (userEntity.TypeOfUser == TypeOfUser.COACH)
        {
            return new ApiResponse()
            {
                Action = false,
                Message = "با شماره دیگری تلاش کنید این شماره با نام مربی ثبت نام کرده است"
            };
        }
        return await GenerateSuccessResponse(userEntity);
        
    }
    private async Task<ApiResponse> GenerateSuccessResponse(User user)
    {
        return new ApiResponse
        {
            Action = true,
            Message = "CodeIsCorrect",
            Result = new 
            {
                RefreshToken = await tokenService.CreateSiteRefreshToken(user),
                AccessToken = tokenService.CreateToken(user),
                
            }
        };
    }

    public async Task<ApiResponse> BuyCoachingService(string phoneNumber, int coachingServiceId)
    {
        var athleteId = await dbContext.Athletes.Where(x=>x.PhoneNumber==phoneNumber).Select(a=>a.Id).FirstOrDefaultAsync();
        var coachService = await dbContext.CoachServices
            .AsNoTracking()
            .Where(x => x.Id == coachingServiceId && x.IsActive && !x.IsDeleted)
            .Select(wr => new
            {
                wr.Price,
                wr.CoachId,
                wr.Title,
                CoachFirstname = wr.Coach.User.FirstName,
                CoachLastname = wr.Coach.User.LastName,
            }).FirstOrDefaultAsync();

        if (coachService == null)
            return new ApiResponse { Message = "CoachingService not found", Action = false };


        var zarinPalResponse = await zarinPal.RequestPaymentAsync(new ZarinPalPaymentRequestDto
        {
            amount = (long)coachService.Price,
            callback_url = "https://chaarset.ir/verify-payment/",
            description = "خرید",
            Mobile = phoneNumber
        });

        if (!zarinPalResponse.IsSuccessful)
        {
            return new ApiResponse
            {
                Action = false,
                Message = zarinPalResponse.ErrorMessage
            };
        }

        var payment = new Payment
        {
            AthleteId = athleteId,
            CoachServiceId = coachingServiceId,
            CoachId = coachService.CoachId,
            Authority = zarinPalResponse.Authority,
            Amount = coachService.Price,
        };

        await dbContext.Payments.AddAsync(payment);
        await dbContext.SaveChangesAsync();

        return new ApiResponse
        {
            Action = true,
            Message = "get url successfully",
            Result = new
            {
                zarinPalResponse.PaymentUrl,
                CoachServieName = coachService.Title,
                CoachName = coachService.CoachFirstname + " " + coachService.CoachLastname,
                coachService.Price
            }
        };
    }


    private async Task<User> CreateNewAthleteUser(string phoneNumber)
    {
        var newUser = new User
        {
            UserName = await GenerateUniqueUsername(),
            PhoneNumber = phoneNumber,
            TypeOfUser = TypeOfUser.ATHLETE,
            LastLoginSite = DateTime.Now,
        };
        


        newUser.Athlete = new Athlete()
        {
            User = newUser,
            PhoneNumber = phoneNumber
        };

        await tokenService.CreateSiteRefreshToken(newUser);

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

    public async Task<ApiResponse> VerifyPaymentAsync(ZarinPalVerifyRequestDto request, string status)
    {
        var payment = await dbContext.Payments
            .Include(p => p.CoachService)
            .Include(p => p.Athlete).ThenInclude(athlete => athlete.User)
            .Include(p => p.WorkoutProgram).Include(payment => payment.Coach).ThenInclude(coach => coach.User)
            .FirstOrDefaultAsync(x => x.Authority == request.Authority);

        if (payment == null)
        {
            return new ApiResponse
            {
                Action = false,
                Message = "تراکنشی یافت نشد",
            };
        }

        if (!status.Equals("ok", StringComparison.CurrentCultureIgnoreCase))
        {
            payment.PaymentStatus = PaymentStatus.FAILED;
            await dbContext.SaveChangesAsync();
            return new ApiResponse()
            {
                Action = false,
                Message = "پرداخت توسط کاربر لغو شد.",
                Result = new
                {
                    CoachPhoneNumber = payment.Coach.PhoneNumber
                }
            };
        }

        switch (payment.PaymentStatus)
        {
            case PaymentStatus.FAILED:
                return new ApiResponse()
                {
                    Action = false,
                    Message = "پرداخت ناموفق",
                    Result = new
                    {
                        CoachPhoneNumber = payment.Coach.PhoneNumber
                    }
                };
            case PaymentStatus.SUCCESS:
                return new ApiResponse()
                {
                    Action = true,
                    Message = "پرداخت با موفقیت انجام شد و قبلا تایید شده است  ",
                    Result = new
                    {
                        WpKey = tokenService.HashEncode(payment.WorkoutProgram!.Id),
                        payment.RefId,
                        CoachPhoneNumber = payment.Coach.PhoneNumber
                    }
                };

            case PaymentStatus.INPROGRESS:
                break;
        }

        request.Amount = payment.Amount;

        try
        {
            var result = await zarinPal.VerifyPaymentAsync(request);
            switch (result?.Data)
            {
                case { Code: 100 }:
                {
                    var confirmResult = await ConfirmTransactionId(payment, result.Data.Ref_id);
                    if (!confirmResult.Action)
                    {
                        return confirmResult;
                    }

                    var wpKey = tokenService.HashEncode(payment.WorkoutProgram!.Id);

                    await sms.AthleteSuccessfullySmsNotificationForBuyFromSite(
                        payment.Athlete.PhoneNumber,
                        wpKey, payment.CoachService.Title);


                    return new ApiResponse
                    {
                        Action = true,
                        Message = "پرداخت با موفقیت انجام شد ",
                        Result = new
                        {
                            WpKey = wpKey,
                            RefId = result.Data.Ref_id,
                            CoachPhoneNumber = payment.Coach.PhoneNumber
                        }
                    };
                }
                case { Code: 101 }:

                    return new ApiResponse
                    {
                        Action = true,
                        Message = "پرداخت با موفقیت انجام شد و قبلا تایید شده است  ",
                        Result = new
                        {
                            WpKey = tokenService.HashEncode(payment.WorkoutProgram!.Id),
                            payment.RefId,
                            CoachPhoneNumber = payment.Coach.PhoneNumber
                        }
                    };
            }

            if (result?.Errors is null)
                return new ApiResponse
                {
                    Action = false,
                    Message = "خطا ناشناخته",
                };
            payment.PaymentStatus = PaymentStatus.FAILED;
            await dbContext.SaveChangesAsync();


            return new ApiResponse
            {
                Action = false,
                Message = "پرداخت ناموفق",
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error verifying payment: {ex.Message}");
            return new ApiResponse
            {
                Action = false,
                Message = $"Error verifying payment: {ex.Message}"
            };
        }
    }


    public async Task<ApiResponse> GetWorkoutProgram(string wPkey)
    {
        var workoutProgramId = tokenService.DecodeHash(wPkey);
        if (workoutProgramId == 0)
        {
            return new ApiResponse()
            {
                Action = false,
                Message = "برنامه شما پیدا نشد"
            };
        }

        var programData = await dbContext.WorkoutPrograms
            .AsNoTracking()
            .Where(wp => wp.Id == workoutProgramId)
            .Select(wr => new
            {
                wr.Status,
                workoutProgramPrice = wr.Payment.Amount,
                AhtleteFirstName = wr.Athlete.User.FirstName,
                AthleteLastName = wr.Athlete.User.LastName,
                CoachPhoneNumber = wr.Coach.PhoneNumber,
                wr.Payment.PaymentDate,
                wr.Payment.Authority,
                wr.ProgramDuration,
                wr.ProgramLevel,
                wr.ProgramPriorities,
                wr.Title,
                CoachFirstname = wr.Coach.User.FirstName,
                CoachLastname = wr.Coach.User.LastName,
                CoachSocialMedia = new
                {
                    wr.Coach.InstagramLink,
                    wr.Coach.TelegramLink,
                    wr.Coach.WhatsApp
                }
            })
            .FirstOrDefaultAsync();

        if (programData is null)
        {
            return new ApiResponse()
            {
                Action = false,
                Message = "برنامه شما پیدا نشد"
            };
        }

        var pc = new System.Globalization.PersianCalendar();
        var year = pc.GetYear(programData.PaymentDate);
        var month = pc.GetMonth(programData.PaymentDate);
        var day = pc.GetDayOfMonth(programData.PaymentDate);


        var persianDate = $"{year}/{month:D2}/{day:D2}";

        var workoutProgramInfo = new WorkoutProgramInfoForSiteDto
        {
            Status = programData.Status.ToString(),
            WorkoutProgramPrice = programData.workoutProgramPrice.ToString(CultureInfo.InvariantCulture),
            AthleteName = programData.AhtleteFirstName + " " + programData.AthleteLastName,
            PaymentDate = persianDate,
            ProgramDuration = programData.ProgramDuration,
            ProgramLevel = programData.ProgramLevel.ToString(),
            ProgramPriorities = programData.ProgramPriorities.Select(x => x.ToString()).ToList(),
            Title = programData.Title,
            CoachName = programData.CoachFirstname + " " + programData.CoachLastname,
            CoachSocialMedia = new CoachSocialMediaDto
            {
                InstagramLink = programData.CoachSocialMedia.InstagramLink ?? " ",
                TelegramLink = programData.CoachSocialMedia.TelegramLink ?? " ",
                WhatsAppLink = programData.CoachSocialMedia.WhatsApp ?? " "
            }
        };

        return programData.Status switch
        {
            WorkoutProgramStatus.UNCOMPLETEDQUESTION => new ApiResponse()
            {
                Action = true, Message = "no athlete Question submitted", Result = new
                {
                    code = 201,
                    wPkey,
                    athority = programData.Authority,
                    programData.CoachPhoneNumber
                }
            },
            WorkoutProgramStatus.NOTSTARTED => new ApiResponse()
            {
                Action = true, Message = "your program submitted", Result = new
                {
                    code = 202,
                    wPkey,
                    workoutProgramInfo,
                    programData.CoachPhoneNumber
                }
            },
            WorkoutProgramStatus.WRITING => new ApiResponse()
            {
                Action = true, Message = "Your program is being designed", Result = new
                {
                    code = 203,
                    wPkey,
                    workoutProgramInfo,
                    programData.CoachPhoneNumber
                }
            },
            _ => new ApiResponse()
            {
                Action = true, Message = "Your program is ready.", Result = new
                {
                    code = 204,
                    wPkey,
                    workoutProgramInfo,
                    programData.CoachPhoneNumber
                }
            }
        };
    }

    public async Task<ApiResponse> AthleteQuestion(AthleteQuestionBuyFromSiteDto athleteQuestionBuyFromSiteDto)
    {
        var workoutProgramId = tokenService.DecodeHash(athleteQuestionBuyFromSiteDto.WpKey);
        if (workoutProgramId == 0)
        {
            return new ApiResponse { Action = false, Message = "برنامه شما پیدا نشد" };
        }

        var workProgram = await dbContext.WorkoutPrograms
            .Include(wp => wp.Payment).ThenInclude(p => p.CoachService)
            .Include(wp => wp.Payment).ThenInclude(p => p.Coach).ThenInclude(c => c.User)
            .Include(wp => wp.Athlete).ThenInclude(a => a.User).Include(workoutProgram => workoutProgram.Payment)
            .ThenInclude(payment => payment.Athlete).ThenInclude(athlete => athlete.User)
            .FirstOrDefaultAsync(w => w.Id == workoutProgramId);
        if (workProgram is null)
        {
            return new ApiResponse() { Message = "User is not an athlete", Action = false };
        }

        var athlete = workProgram.Athlete;


        athlete.User.BirthDate = Convert.ToDateTime(athleteQuestionBuyFromSiteDto.BirthDay);
        athlete.User.Gender = Enum.Parse<Gender>(athleteQuestionBuyFromSiteDto.Gender.ToUpper() ?? string.Empty);
        athlete.User.FirstName = athleteQuestionBuyFromSiteDto.FirstName;
        athlete.User.LastName = athleteQuestionBuyFromSiteDto.LastName;
        athlete.Height = athleteQuestionBuyFromSiteDto.Height;
        athlete.CurrentWeight = athleteQuestionBuyFromSiteDto.CurrentWeight;
        var athleteQuestion = athleteQuestionBuyFromSiteDto.ToAthleteQuestionBuyFromSite(athlete);

        if (athleteQuestionBuyFromSiteDto.AthleteBodyImageId > 0)
        {
            var athleteImage = await dbContext.AthleteImage
                .FirstOrDefaultAsync(ai => ai.Id == athleteQuestionBuyFromSiteDto.AthleteBodyImageId);
            if (athleteImage is not null)
            {
                athleteImage.AthleteQuestion = athleteQuestion;
            }
        }


        athlete.AthleteQuestions.Add(athleteQuestion);
        workProgram.Status = WorkoutProgramStatus.NOTSTARTED;

        workProgram.Payment.AthleteQuestion = athleteQuestion;
        await dbContext.SaveChangesAsync();
        await sms.CoachServiceBuySmsNotification(workProgram.Payment.Coach.PhoneNumber, workProgram.Payment.Coach.User.FirstName,
            workProgram.Title, workProgram.Payment.Amount.ToString());

        return new ApiResponse()
        {
            Message = "Athlete questions submitted successfully",
            Action = true
        };
    }


    private async Task<ApiResponse> ConfirmTransactionId(Payment payment, long refId)
    {
        try
        {
            payment.CoachService.NumberOfSell += 1;
            payment.RefId = refId;

            var workoutProgram = new WorkoutProgram
            {
                Title = payment.CoachService.Title,
                CoachId = payment.Coach.Id,
                AthleteId = payment.Athlete.Id,
                PaymentId = payment.Id,
                Status = WorkoutProgramStatus.UNCOMPLETEDQUESTION
            };
            var appFee = (payment.Amount * payment.Coach.ServiceFee) < 50000 ? 50000 : (payment.Amount * payment.Coach.ServiceFee);
            payment.AppFee = appFee;
            payment.WorkoutProgram = workoutProgram;
            payment.PaymentStatus = PaymentStatus.SUCCESS;

            await dbContext.WorkoutPrograms.AddAsync(workoutProgram);
            await dbContext.SaveChangesAsync();

            return new ApiResponse
            {
                Message = "Payment confirmed successfully",
                Action = true,
                Result = workoutProgram.Id
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                Message = $"Error confirming transaction: {ex.Message}",
                Action = false
            };
        }
    }
}