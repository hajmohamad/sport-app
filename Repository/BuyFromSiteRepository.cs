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
using sport_app_backend.Models.Account.Athlete;

using sport_app_backend.Models.Account;
using sport_app_backend.Models.Login_Sinup;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Question.A_Question;
using sport_app_backend.Models.TrainingPlan;

namespace sport_app_backend.Repository;

public class BuyFromSiteRepository(
    ApplicationDbContext dbContext,
    ITokenService tokenService,
    ISmsService sms,
    IStorage Storage,
    IZarinPal zarinPal,
    IConfiguration config,
    IChatRepository chatRepository)
    : IBuyFromSiteRepository
{
    public async Task<ApiResponse> GenerateAccessToken(string refreshToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.SiteRefreshToken == refreshToken);
        if (user is null) return new ApiResponse() { Message = "Invalid refresh token", Action = false };
        return user.LastLoginSite.AddDays(90) < DateTime.Now ? new ApiResponse() { Message = "Refresh token expired", Action = false } : new ApiResponse() { Message = "Success", Action = true, Result = new { AccessToken = tokenService.CreateTokenForSite(user) } };
    }
    public async Task<ApiResponse> CreateWorkoutPdfAsync(string wpId)
{
    var id = tokenService.DecodeHash(wpId);
    
    var workoutData = await dbContext.WorkoutPrograms
        .AsNoTracking()
        .Where(wp => wp.Id == id)
        .Select(wp => new 
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
            wp.ProgramPriority,
            AthleteCurrentWeight = (double?)wp.Payment.AthleteQuestion.Weight ?? 0.0,
            AthleteHeight = (double?)wp.Athlete.Height ?? 0.0,
            AhtleteGender = wp.Athlete.User.Gender,
            coachSlug = wp.Coach.WebSiteUrl ?? "",
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

    var heightInMeters = workoutData.AthleteHeight > 0 ? workoutData.AthleteHeight / 100.0 : 1.75;
    var bmi = heightInMeters > 0 ? (workoutData.AthleteCurrentWeight / (heightInMeters * heightInMeters)) : 0;

    var pdfModel = new WorkoutPdfModel
    {
        ProgramTitle = workoutData.Title ?? "برنامه ورزشی",
        StartDate = workoutData.StartDate?.ToShamsiDateString() ?? "ثبت نشده",
        CoachName = $"{workoutData.CoachFirstName} {workoutData.CoachLastName}".Trim(),
        ProgramLevel = workoutData.ProgramLevel.ToPersianString(),
        ProgramDuration = workoutData.ProgramDuration.ToString(),
        ProgramPriorities = workoutData.ProgramPriority != null 
            ? string.Join(" - ", workoutData.ProgramPriority.ToPersianString()) 
            : "",
        AthleteWeight = workoutData.AthleteCurrentWeight > 0 ? workoutData.AthleteCurrentWeight.ToString() : "ثبت نشده",
        AthleteHeight = workoutData.AthleteHeight > 0 ? workoutData.AthleteHeight.ToString() : "ثبت نشده",
        AthleteBmi = bmi > 0 ? Math.Round(bmi, 2).ToString() : "نامشخص",
        CoachSlug = workoutData.coachSlug,
        AthleteName = $"{workoutData.AthleteFirstName} {workoutData.AthleteLastName}".Trim(),
        AthleteFatPercent = workoutData.AthleteCurrentBodyForm != null 
            ? workoutData.AhtleteGender.GetFatPercentRange(workoutData.AthleteCurrentBodyForm) 
            : "نامشخص",
        WorkoutDays = workoutData.ProgramInDays.Select(pd => new WorkoutDayModel
        {
            DayNumber = pd.ForWhichDay,
            Exercises = pd.Exercises.Select(se => new ExerciseModel
            {
                Name = se.PersianName,
                Reps = se.RepsJson.ToReps(),
                Description = se.Description ?? "",
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
                        await Storage.RemovePhoto(frontLink);
                    }

                    var response = await Storage.UploadImage(file, "","bodyImage");
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
                        await Storage.RemovePhoto(frontLink);
                    }

                    var response = await Storage.UploadImage(file, "","bodyImage");
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
                        await Storage.RemovePhoto(frontLink);
                    }

                    var response = await Storage.UploadImage(file, "","bodyImage");
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
            var response = await Storage.UploadImage(file, "","bodyImage");
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
                        var removeResponse = await Storage.RemovePhoto(frontLink);
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
                        var removeResponse = await Storage.RemovePhoto(backLink);
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
                        var removeResponse = await Storage.RemovePhoto(sideLink);
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
                Code = await sms.SiteLogin(userPhoneNumber),
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
            Code = await sms.SiteLogin(userPhoneNumber),
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
                AccessToken = tokenService.CreateTokenForSite(user),
                
            }
        };
    }

    public async Task<ApiResponse> PreviewCheckout(string phoneNumber, int serviceId, CheckoutDiscountRequestDto? checkoutDiscountRequestDto)
    {
        var athleteId = await dbContext.Athletes.Where(x => x.PhoneNumber == phoneNumber).Select(a => a.Id)
            .FirstOrDefaultAsync();
        if (athleteId == 0)
        {
            return new ApiResponse { Action = false, Message = "User is not an athlete" };
        }

        var coachService = await dbContext.CoachServices.Include(coachService => coachService.Coach)
            .ThenInclude(coach => coach.User)
            .FirstOrDefaultAsync(x => x.Id == serviceId && x.IsActive && !x.IsDeleted);
        if (coachService is null)
        {
            return new ApiResponse { Message = "CoachingService not found", Action = false };
        }

        var pricingResult = await CalculateCheckoutPricing(coachService, checkoutDiscountRequestDto?.DiscountCode);
        if (!pricingResult.Action)
        {
            return pricingResult;
        }

        var now = DateTime.Now;

        var havLastAttempt = await dbContext.PaymentAttempts.Where(pa => pa.AthleteId == athleteId && pa.DateTime.Date==now.Date)
            .OrderByDescending(p => p.DateTime).FirstOrDefaultAsync();
        if (havLastAttempt is null)
        {
            var newPaymentAttempt = new PaymentAttempt()
            {
                AthleteId = athleteId,
                CoachId = coachService.CoachId,
                CoachServiceId = coachService.Id,
                DateTime = DateTime.Now

            };
            await dbContext.PaymentAttempts.AddAsync(newPaymentAttempt);
            
        }
        else
        {
            havLastAttempt.DateTime = DateTime.Now;
            havLastAttempt.CoachId = coachService.CoachId;
            havLastAttempt.CoachServiceId = coachService.Id;
        }

        await dbContext.SaveChangesAsync();

       

        return new ApiResponse
        {
            Action = true,
            Message = "preview generated",
            Result = new {
                pricingResult.Result,
                CoachServieName = coachService.Title,
                CoachName = coachService.Coach.User.FirstName + " " + coachService.Coach.User.LastName,
                coachSlug= coachService.Coach.WebSiteUrl
            }
        };
    }

    public async Task<ApiResponse> BuyCoachingService(string phoneNumber, int coachingServiceId, CheckoutDiscountRequestDto? checkoutDiscountRequestDto)
    {
        var athleteId = await dbContext.Athletes.Where(x=>x.PhoneNumber==phoneNumber).Select(a=>a.Id).FirstOrDefaultAsync();
        var coachService = await dbContext.CoachServices
            .FirstOrDefaultAsync(x => x.Id == coachingServiceId && x.IsActive && !x.IsDeleted);

        if (coachService == null)
            return new ApiResponse { Message = "CoachingService not found", Action = false };

        var pricingResult = await CalculateCheckoutPricing(coachService, checkoutDiscountRequestDto?.DiscountCode);
        if (!pricingResult.Action)
        {
            return pricingResult;
        }

        if (pricingResult.Result is not DiscountPreviewDto pricing || pricing.FinalPrice <= 0)
        {
            return new ApiResponse { Action = false, Message = "مبلغ نهایی پرداخت باید بیشتر از صفر باشد." };
        }

        var zarinPalResponse = await zarinPal.RequestPaymentAsync(new ZarinPalPaymentRequestDto
        {
            amount = (long)pricing.FinalPrice,
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
            Amount = pricing.FinalPrice,
            OriginalAmount = pricing.OriginalPrice,
            // PublicDiscountAmount = pricing.PublicDiscountAmount,
            CodeDiscountAmount = pricing.CodeDiscountAmount,
            DiscountCodeId = await GetDiscountCodeId(checkoutDiscountRequestDto?.DiscountCode),
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
                Price = pricing.OriginalPrice,
                // pricing.PublicDiscountAmount,
                pricing.CodeDiscountAmount,
                pricing.FinalPrice,

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
        await dbContext.Users.AddAsync(newUser);
        await dbContext.SaveChangesAsync();
        
        var athlete = new Athlete()
        {
            User = newUser,
            UserId = newUser.Id,
            PhoneNumber = phoneNumber
        };
        newUser.Athlete = athlete;
        
        await dbContext.Athletes.AddAsync(athlete);
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
        .Include(p => p.Athlete).ThenInclude(a => a.User)
        .Include(p => p.WorkoutProgram)
        .Include(p => p.Coach).ThenInclude(c => c.User)
        .FirstOrDefaultAsync(x => x.Authority == request.Authority);

    if (payment == null)
    {
        return new ApiResponse
        {
            Action = false,
            Message = "تراکنشی یافت نشد",
            Result = new { coachSlug = "", coachName = "" }
        };
    }

    var coachPhone = payment.Coach?.PhoneNumber;
    var coachSlug = payment.Coach?.WebSiteUrl;
    var coachName = $"{payment.Coach?.User?.FirstName} {payment.Coach?.User?.LastName}";

    if (!string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase))
    {
        payment.PaymentStatus = PaymentStatus.FAILED;
        await dbContext.SaveChangesAsync();

        return new ApiResponse
        {
            Action = false,
            Message = "پرداخت توسط کاربر لغو شد.",
            Result = new
            {
                CoachPhoneNumber = coachPhone,
                coachSlug,
                coachName
            }
        };
    }

    if (payment.PaymentStatus == PaymentStatus.FAILED)
    {
        return new ApiResponse
        {
            Action = false,
            Message = "پرداخت ناموفق",
            Result = new
            {
                CoachPhoneNumber = coachPhone,
                coachSlug,
                coachName
            }
        };
    }

    if (payment.PaymentStatus == PaymentStatus.SUCCESS)
    {
        return new ApiResponse
        {
            Action = true,
            Message = "پرداخت قبلاً تایید شده است",
            Result = new
            {
                WpKey = tokenService.HashEncode(payment.WorkoutProgram!.Id),
                payment.RefId,
                CoachPhoneNumber = coachPhone,
                coachSlug,
                coachName
            }
        };
    }

    request.Amount = payment.Amount;

    try
    {
        var result = await zarinPal.VerifyPaymentAsync(request);

        if (result?.Data?.Code == 100)
        {
            var confirmResult = await ConfirmTransactionId(payment, result.Data.Ref_id);
            if (!confirmResult.Action)
                return confirmResult;
            var conversationResult =
                await chatRepository.CreateCoachAthleteConversation(
                    payment.Coach.UserId,
                    payment.Athlete.UserId);
            if (!conversationResult.Action)
            {
                return conversationResult;
            }

            var wpKey = tokenService.HashEncode(payment.WorkoutProgram!.Id);

            await sms.AthleteSuccessfullySmsNotificationForBuyFromSite(
                payment.Athlete.PhoneNumber,
                payment.CoachService.Title,
                wpKey
            );

            return new ApiResponse
            {
                Action = true,
                Message = "پرداخت با موفقیت انجام شد",
                Result = new
                {
                    WpKey = wpKey,
                    RefId = result.Data.Ref_id,
                    CoachPhoneNumber = coachPhone,
                    coachSlug,
                    coachName
                }
            };
        }

        if (result?.Data?.Code == 101)
        {
            return new ApiResponse
            {
                Action = true,
                Message = "پرداخت قبلاً تایید شده است",
                Result = new
                {
                    WpKey = tokenService.HashEncode(payment.WorkoutProgram!.Id),
                    payment.RefId,
                    CoachPhoneNumber = coachPhone,
                    coachSlug,
                    coachName
                }
            };
        }

        payment.PaymentStatus = PaymentStatus.FAILED;
        await dbContext.SaveChangesAsync();

        return new ApiResponse
        {
            Action = false,
            Message = "پرداخت ناموفق",
            Result = new
            {
                CoachPhoneNumber = coachPhone,
                coachSlug,
                coachName
            }
        };
    }
    catch (Exception)
    {
        return new ApiResponse
        {
            Action = false,
            Message = "خطا در تایید پرداخت",
            Result = new
            {
                CoachPhoneNumber = coachPhone,
                coachSlug,
                coachName
            }
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
                DiscountPercent = wr.Payment.DiscountCode == null 
                    ? 0 
                    : wr.Payment.DiscountCode.DiscountPercent,
                CodeDiscountAmount = wr.Payment.CodeDiscountAmount ,
                AhtleteFirstName = wr.Athlete.User.FirstName,
                AthleteLastName = wr.Athlete.User.LastName,
                CoachPhoneNumber = wr.Coach.PhoneNumber,
                wr.Payment.PaymentDate,
                wr.Payment.Authority,
                wr.ProgramDuration,
                wr.ProgramLevel,
                wr.ProgramPriority,
                wr.Title,
                CoachFirstname = wr.Coach.User.FirstName,
                CoachLastname = wr.Coach.User.LastName,
                wr.Coach.WebSiteUrl,
                CoachSocialMedia = new
                {
                    wr.Coach.InstagramLink,
                    wr.Coach.TelegramLink,
                    wr.Coach.WhatsApp,
                    wr.Coach.EitaaUserName,
                    wr.Coach.BaleUserName
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
            CodeDiscountAmount =  programData.CodeDiscountAmount,
            CodeDiscountPercent = programData.DiscountPercent,
            AthleteName = programData.AhtleteFirstName + " " + programData.AthleteLastName,
            PaymentDate = persianDate,
            ProgramDuration = programData.ProgramDuration,
            ProgramLevel = programData.ProgramLevel.ToString(),
            ProgramPriorities = [programData.ProgramPriority.ToString()],
            Title = programData.Title,
            CoachName = programData.CoachFirstname + " " + programData.CoachLastname,
            CoachSocialMedia = new CoachSocialMediaDto
            {
                InstagramLink = programData.CoachSocialMedia.InstagramLink ?? " ",
                TelegramLink = programData.CoachSocialMedia.TelegramLink ?? " ",
                WhatsAppLink = programData.CoachSocialMedia.WhatsApp ?? " ",
                BaleUserName = programData.CoachSocialMedia.BaleUserName??" ",
                EitaaUserName = programData.CoachSocialMedia.EitaaUserName
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
                    programData.CoachPhoneNumber,
                    coachSlug= programData.WebSiteUrl,
                    workoutProgramInfo.CoachName
                }
            },
            WorkoutProgramStatus.NOTSTARTED => new ApiResponse()
            {
                Action = true, Message = "your program submitted", Result = new
                {
                    code = 202,
                    wPkey,
                    workoutProgramInfo,
                    programData.CoachPhoneNumber,
                    coachSlug= programData.WebSiteUrl,
                    workoutProgramInfo.CoachName
                }
            },
            WorkoutProgramStatus.WRITING => new ApiResponse()
            {
                Action = true, Message = "Your program is being designed", Result = new
                {
                    code = 203,
                    wPkey,
                    workoutProgramInfo,
                    programData.CoachPhoneNumber,
                    coachSlug= programData.WebSiteUrl,
                    workoutProgramInfo.CoachName
                }
            },
            _ => new ApiResponse()
            {
                Action = true, Message = "Your program is ready.", Result = new
                {
                    code = 204,
                    wPkey,
                    workoutProgramInfo,
                    programData.CoachPhoneNumber,
                    coachSlug= programData.WebSiteUrl,
                    workoutProgramInfo.CoachName
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
        var weightEntry = new WeightEntry()
        {
            Athlete = athlete,
            AthleteId = athlete.Id,
            CurrentDate = DateTime.Now,
            Weight = athleteQuestionBuyFromSiteDto.CurrentWeight
        };
        var weightEntryBefore = new WeightEntry()
        {
            Athlete = athlete,
            AthleteId = athlete.Id,
            CurrentDate = DateTime.Now.AddDays(-1),
            Weight = athleteQuestionBuyFromSiteDto.CurrentWeight
        };
        await dbContext.WeightEntries.AddAsync(weightEntry);
        await dbContext.WeightEntries.AddAsync(weightEntryBefore);
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
            if (payment.DiscountCodeId.HasValue)
            {
                var discountCode = await dbContext.DiscountCodes.FirstOrDefaultAsync(x => x.Id == payment.DiscountCodeId.Value);
                if (discountCode is not null)
                {
                    discountCode.UsedCount += 1;
                    discountCode.UpdatedAt = DateTime.Now;
                }
            }

            var workoutProgram = new WorkoutProgram
            {
                Title = payment.CoachService.Title,
                CoachId = payment.Coach.Id,
                AthleteId = payment.Athlete.Id,
                PaymentId = payment.Id,
                Status = WorkoutProgramStatus.UNCOMPLETEDQUESTION
            };
            var appFee = (payment.OriginalAmount * payment.Coach.ServiceFee) < 50000 ? 50000 : (payment.OriginalAmount * payment.Coach.ServiceFee);
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

    private async Task<ApiResponse> CalculateCheckoutPricing(CoachService coachService, string? discountCodeValue)
    {
        var originalPrice = coachService.Price;
        // var publicDiscountAmount = 0.0 ;
        var codeDiscountPercent = 0.0;
        // var publicDiscountPercent = 0.0;
        var codeDiscountAmount =0.0;
        double finalPrice;
        
        if (discountCodeValue is null)
        {
            // publicDiscountAmount= coachService.CalculatePublicDiscountAmount();
            // publicDiscountPercent = coachService.PublicDiscountPercent?? 0 ;
            // finalPrice = originalPrice - publicDiscountAmount;
            finalPrice = originalPrice;
        }else{
            var discountCodeValidation =
                await ValidateDiscountCode(coachService.CoachId,coachService.Id, NormalizeDiscountCode(discountCodeValue));
            if (!discountCodeValidation.Action)
            {
                return discountCodeValidation;
            }

            if (discountCodeValidation.Result is not DiscountCode discountCode)
            {
                return new ApiResponse { Action = false, Message = "کد اشتباه است" };
            }

            codeDiscountAmount = CoachMappers.CalculateDiscountAmount(coachService.Price, discountCode.DiscountPercent);
            codeDiscountPercent = discountCode.DiscountPercent;
            finalPrice = originalPrice - codeDiscountAmount;





        }

        return new ApiResponse
        {
            Action = true,
            Message = "bla",
            Result = new DiscountPreviewDto
            {
                OriginalPrice = originalPrice,
                // PublicDiscountAmount = publicDiscountAmount,
                // PublicDiscountPercent =  publicDiscountPercent,
                CodeDiscountAmount =  codeDiscountAmount,
                CodeDiscountPercent =  codeDiscountPercent,
                FinalPrice = finalPrice
            }
        };
    }

    private async Task<ApiResponse> ValidateDiscountCode(int coachId, int coachServiceId, string normalizedCode)
{
    var discountCode = await dbContext.DiscountCodes
        .Include(dc => dc.DiscountCodeCoachServices)
        .FirstOrDefaultAsync(x => x.CoachId == coachId && x.Code == normalizedCode && !x.IsDeleted);

    if (discountCode is null)
    {
        return new ApiResponse { Action = false, Message = "کد تخفیف وارد شده معتبر نیست." };
    }

    if (discountCode.Status == DiscountCodeStatus.INACTIVE)
    {
        return new ApiResponse { Action = false, Message = "این کد تخفیف غیرفعال است." };
    }
    
    bool isExpiredByDate = discountCode.ExpiresAt.HasValue && discountCode.ExpiresAt.Value <= DateTime.Now;
    bool isExpiredByUsage = discountCode.UsageLimit.HasValue && discountCode.UsedCount >= discountCode.UsageLimit.Value;

    if (discountCode.Status == DiscountCodeStatus.EXPIRED || isExpiredByDate || isExpiredByUsage)
    {
        if (discountCode.Status != DiscountCodeStatus.EXPIRED)
        {
            discountCode.Status = DiscountCodeStatus.EXPIRED;
            await dbContext.SaveChangesAsync();
        }
        return new ApiResponse { Action = false, Message = "این کد تخفیف منقضی شده است." };
    }


    if (!discountCode.AppliesToAllServices&& discountCode.DiscountCodeCoachServices.All(dcs => dcs.CoachServiceId != coachServiceId))
    {
        return new ApiResponse { Action = false, Message = "کد تخفیف برای این سرویس قابل استفاده نیست." };
    }
   

    return new ApiResponse 
    { 
        Action = true,
        Message = "کد تخفیف معتبر است",
        Result = discountCode 
    };
}

    private async Task<int?> GetDiscountCodeId(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return await dbContext.DiscountCodes
            .Where(x =>  x.Code == code && !x.IsDeleted)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();
    }

    private static string NormalizeDiscountCode(string code)
    {
        return code.Trim().Replace(" ", string.Empty).ToUpperInvariant();
    }
}
