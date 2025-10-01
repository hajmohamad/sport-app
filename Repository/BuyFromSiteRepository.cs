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
                    se.Set,
                    se.Rep
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
        ProgramDuration = workoutData.ProgramDuration.ToString() ,
        ProgramPriorities = string.Join(" - ", workoutData.ProgramPriorities.Select(p => p.ToPersianString())),
        AthleteWeight = workoutData.AthleteCurrentWeight.ToString(),
        AthleteHeight = workoutData.AthleteHeight.ToString(),
        AthleteBmi = Math.Round(bmi, 2).ToString(), 
        AthleteFatPercentage = workoutData.AhtleteGender.GetFatPercentageRange(workoutData.AthleteCurrentBodyForm),
        WorkoutDays = workoutData.ProgramInDays.Select(pd => new WorkoutDayModel
        {
            DayNumber = pd.ForWhichDay,
            Exercises = pd.Exercises.Select(se => new ExerciseModel
            {
                Name = se.PersianName,
                Set = se.Set,
                Rep = se.Rep.ToString()
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

    public Task<ApiResponse> GetExercise(int exerciseId)
    {
        var exercise = dbContext.Exercises.FirstOrDefault(x => x.Id == exerciseId);
        if (exercise is null) return Task.FromResult(new ApiResponse() { Message = "Exercise not found", Action = false });
        return Task.FromResult(new ApiResponse()
            { Message = "Success", Action = true, Result = exercise.ToExerciseDto() });
    }
    public async Task<ApiResponse>  Login(string userPhoneNumber)
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
            var newAthlete = await CreateNewAthlete(checkCodeRequestDto.PhoneNumber);
            var paymentUrlForNewAthlete = await BuyCoachingService(newAthlete.Id,checkCodeRequestDto.PhoneNumber,checkCodeRequestDto.CoachServiceId);
            
            return paymentUrlForNewAthlete;
        }
        if (userEntity.TypeOfUser == TypeOfUser.COACH)
        {
            return new ApiResponse()
            {
                Action = false,
                Message = "با شماره دیگری تلاش کنید این شماره با نام مربی ثبت نام کرده است"
            };
        }
        var athleteId = await dbContext.Athletes.AsNoTracking()
            .Where(a => a.PhoneNumber == checkCodeRequestDto.PhoneNumber)
            .Select(a => a.Id).FirstOrDefaultAsync();        
        var payment = await BuyCoachingService(athleteId,checkCodeRequestDto.PhoneNumber,checkCodeRequestDto.CoachServiceId);
       
        return payment;
        
   
       
    }

    private async Task<ApiResponse> BuyCoachingService(int athleteId,string phoneNumber, int coachingServiceId)
        {
         
            var coachService = await dbContext.CoachServices
                .AsNoTracking()
                .Where(x => x.Id == coachingServiceId && x.IsActive&&!x.IsDeleted)
                .Select(wr=>new
                {
                    wr.Price,
                    wr.CoachId,
                    wr.Title,
                    CoachFirstname= wr.Coach.User.FirstName,
                    CoachLastname=wr.Coach.User.LastName,
                    
                }).FirstOrDefaultAsync();

            if (coachService == null)
                return new ApiResponse { Message = "CoachingService not found", Action = false };

         
            var zarinPalResponse = await zarinPal.RequestPaymentAsync(new ZarinPalPaymentRequestDto
            {
                amount = (long)coachService.Price,
                callback_url = "https://chaarset.ir/verify-payment",
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
                    CoachName= coachService.CoachFirstname + " "+coachService.CoachLastname,
                    coachService.Price
                }
            };
        }
    private async Task<Athlete> CreateNewAthlete(string phoneNumber)
    {
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

        return newUser.Athlete;
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
    public async Task<ApiResponse> VerifyPaymentAsync(ZarinPalVerifyRequestDto request,string status)
        {
            var payment = await dbContext.Payments
                .Include(p => p.CoachService)
                .Include(p => p.Athlete).ThenInclude(athlete => athlete.User)
                .Include(p => p.WorkoutProgram).
                Include(payment => payment.Coach).ThenInclude(coach => coach.User)
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
                        CoachPhoneNumber= payment.Coach.PhoneNumber
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
                        Result =new 
                        {                                    
                            CoachPhoneNumber= payment.Coach.PhoneNumber
                        }
                    };
                case PaymentStatus.SUCCESS:
                    return new ApiResponse()
                    {
                        Action = true,
                        Message = "پرداخت با موفقیت انجام شد و قبلا تایید شده است  ",
                        Result = new
                        {
                            WpKey= tokenService.HashEncode(payment.WorkoutProgram!.Id),
                            payment.RefId,
                            CoachPhoneNumber= payment.Coach.PhoneNumber
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

                                var wpKey =  tokenService.HashEncode((int)(confirmResult.Result ?? 0));

                                await sms.AthleteSuccessfullySmsNotificationForBuyFromSite(
                                    payment.Athlete.PhoneNumber,
                                    wpKey, payment.CoachService.Title);


                                return new ApiResponse
                                {
                                    Action = true,
                                    Message = "پرداخت با موفقیت انجام شد ",
                                    Result = new
                                    {
                                        WpKey= wpKey,
                                        RefId=result.Data.Ref_id,
                                        CoachPhoneNumber= payment.Coach.PhoneNumber
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
                                        WpKey= tokenService.HashEncode(payment.WorkoutProgram!.Id),
                                        payment.RefId,
                                        CoachPhoneNumber= payment.Coach.PhoneNumber
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
                AhtleteFirstName= wr.Athlete.User.FirstName,
                AthleteLastName = wr.Athlete.User.LastName,
                wr.Payment.PaymentDate,
                wr.Payment.Authority,
                wr.ProgramDuration,
                wr.ProgramLevel,
                wr.ProgramPriorities,
                wr.Title,
                CoachFirstname= wr.Coach.User.FirstName,
                CoachLastname=wr.Coach.User.LastName,
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
            WorkoutProgramPrice = programData.workoutProgramPrice.ToString(),
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
                    athority = programData.Authority
                }
            },
            WorkoutProgramStatus.NOTSTARTED => new ApiResponse()
            {
                Action = true, Message = "your program submitted", Result = new
                {
                    code = 202,
                    wPkey,
                    workoutProgramInfo
                }
            },
            WorkoutProgramStatus.WRITING => new ApiResponse()
            {
                Action = true, Message = "Your program is being designed", Result = new
                {
                    code = 203,
                    wPkey,
                    workoutProgramInfo
                }
            },
            _ => new ApiResponse() { Action = true, Message = "Your program is ready.", Result = new
            {
                code = 204,
                wPkey,
                workoutProgramInfo
            } }
        };
    }
    public async Task<ApiResponse> SubmitAthleteQuestionWithImages(
    [FromForm] AthleteQuestionBuyFromSiteDto dto, 
    IFormFile? frontImage, 
    IFormFile? backImage, 
    IFormFile? sideImage)
{
    var workoutProgramId = tokenService.DecodeHash(dto.WpKey);
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
   
    
    if (workProgram?.Athlete is null)
    {
        return new ApiResponse { Action = false, Message = "برنامه شما پیدا نشد" };
    }
    
    var athlete = workProgram.Athlete;
    AthleteBodyImage? newAthleteBodyImage = null;
    if (frontImage != null || backImage != null || sideImage != null)
    {
         newAthleteBodyImage = new AthleteBodyImage { AthleteId = athlete.Id };

        if (frontImage != null)
        {
            var response = await liaraStorage.UploadImage(frontImage,"");
            if (!response.Action) return response;
            newAthleteBodyImage.FrontLink = response.Result as string;
        }
        if (backImage != null)
        {
            var response = await liaraStorage.UploadImage(backImage,"");
            if (!response.Action) return response;
            newAthleteBodyImage.BackLink = response.Result as string;
        }
        if (sideImage != null)
        {
            var response = await liaraStorage.UploadImage(sideImage,"");
            if (!response.Action) return response;
            newAthleteBodyImage.SideLink = response.Result as string;
        }
    }

    athlete.Height = dto.Height;
    athlete.User.FirstName = dto.FirstName;
    athlete.User.LastName = dto.LastName;
    athlete.User.BirthDate = Convert.ToDateTime(dto.BirthDay);

    var athleteQuestion = dto.ToAthleteQuestionBuyFromSite(athlete); // فرض می‌کنیم یک مپر برای DTO جدید دارید
    workProgram.Payment.AthleteQuestion = athleteQuestion;
    
    if (newAthleteBodyImage != null)
    {
        athleteQuestion.AthleteBodyImage = newAthleteBodyImage;
    }
    
    workProgram.Status = WorkoutProgramStatus.NOTSTARTED;
    
    
    await dbContext.SaveChangesAsync();
    
    var payment = workProgram.Payment;
    if (!string.IsNullOrEmpty(payment.Athlete.User.FirstName))
    {
        await sms.NotifyAthleteOfProgramLinkSms(
            payment.Athlete.PhoneNumber,
            payment.Athlete.User.FirstName,
            dto.WpKey
        );
    }
    
    if (!string.IsNullOrEmpty(payment.Coach.User.FirstName))
    {
        await sms.CoachServiceBuySmsNotification(
            payment.Coach.PhoneNumber,
            payment.Coach.User.FirstName, 
            payment.CoachService.Title,
            payment.Amount.ToString(CultureInfo.CurrentCulture));
    }

    return new ApiResponse
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
            payment.AppFee = (payment.Amount * payment.Coach.ServiceFee);

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
