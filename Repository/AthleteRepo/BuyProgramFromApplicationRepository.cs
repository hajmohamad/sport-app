using System.Globalization;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Dtos.ZarinPal;
using sport_app_backend.Dtos.ZarinPal.Verify;
using sport_app_backend.Interface;
using sport_app_backend.Interface.Athlete;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Question.A_Question;

namespace sport_app_backend.Repository.AthleteRepo;

public class BuyProgramFromApplicationRepository(
    ApplicationDbContext context,
    IZarinPal zarinPal,
    ISmsService smsService,
    ILiaraStorage liara) : IBuyProgramFromApplications
{
    private async Task<ApiResponse> ConfirmTransactionId(Payment payment, long refId)
    {
        try
        {
            payment.CoachService.NumberOfSell += 1;
            payment.RefId = refId;

            var workoutProgram = new WorkoutProgram
            {
                Title = payment.CoachService.Title,
                Coach = payment.Coach,
                CoachId = payment.Coach.Id,
                Athlete = payment.Athlete,
                AthleteId = payment.Athlete.Id,
                Payment = payment,
                PaymentId = payment.Id
            };
            payment.AppFee = (payment.Amount * payment.Coach.ServiceFee);

            payment.WorkoutProgram = workoutProgram;
            payment.PaymentStatus = PaymentStatus.SUCCESS;

            await context.WorkoutPrograms.AddAsync(workoutProgram);
            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Message = "Payment confirmed successfully",
                Action = true
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



        public async Task<ApiResponse> UploadImageForAthleteQuestion(string phoneNumber,int id, string sideName, IFormFile file)
        {
            var athleteId = await context.Athletes.AsNoTracking()
                .Where(a => a.PhoneNumber == phoneNumber)
                .Select(a => a.Id).FirstOrDefaultAsync();
            if (athleteId ==0)
            {
                return new ApiResponse
                {
                    Message = "athlete not found",
                    Action = false
                };
            }
            if (id != 0)
            {
               
                var athleteImage = await context.AthleteImage.Where(ai => ai.Id == id&&ai.AthleteId==athleteId).FirstOrDefaultAsync();
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
                            await liara.RemovePhoto(frontLink);
                        }

                        var response = await liara.UploadImage(file, "","bodyImage");
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
                            await liara.RemovePhoto(frontLink);
                        }

                        var response = await liara.UploadImage(file, "","bodyImage");
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
                            await liara.RemovePhoto(frontLink);
                        }

                        var response = await liara.UploadImage(file, "","bodyImage");
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
                await context.SaveChangesAsync();
                return new ApiResponse()
                {
                    Action = true,
                    Message = "img upload successfully",
                    Result = athleteImage
                };
            }
            else
            {
                var response = await liara.UploadImage(file, "","bodyImage");
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

                await context.AthleteImage.AddAsync(athleteImage);
                await context.SaveChangesAsync();
                return new ApiResponse()
                {
                    Action = true,
                    Message = "img upload successfully",
                    Result = athleteImage.ToAthleteBodyImageDto()
                };
            }
        }

        public async Task<ApiResponse> RemoveImageForAthleteQuestion(string phoneNumber,int id, string sideName)
        {
            var athleteId = await context.Athletes.AsNoTracking()
                .Where(a => a.PhoneNumber == phoneNumber)
                .Select(a => a.Id).FirstOrDefaultAsync();
            if (athleteId ==0)
            {
                return new ApiResponse
                {
                    Message = "athlete not found",
                    Action = false
                };
            }

            var athleteImage = await context.AthleteImage.Where(ai => ai.Id == id&&ai.AthleteId==athleteId).FirstOrDefaultAsync();
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
                        var removeResponse = await liara.RemovePhoto(frontLink);
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
                        var removeResponse = await liara.RemovePhoto(backLink);
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
                        var removeResponse = await liara.RemovePhoto(sideLink);
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
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "img remove successfully",
                Result = athleteImage.ToAthleteBodyImageDto()
            }; 

            
            
        }

        public async Task<ApiResponse> GetImageForAthleteQuestion(string phoneNumber,int id)
        {
            var athleteId = await context.Athletes.AsNoTracking()
                .Where(a => a.PhoneNumber == phoneNumber)
                .Select(a => a.Id).FirstOrDefaultAsync();
            if (athleteId ==0)
            {
                return new ApiResponse
                {
                    Message = "athlete not found",
                    Action = false
                };
            }

            var athleteImage = await context.AthleteImage.Where(ai => ai.Id == id&&ai.AthleteId==athleteId).FirstOrDefaultAsync();
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
        public async Task<ApiResponse> VerifyPaymentAsync(ZarinPalVerifyRequestDto request,string status)
        {
            var payment = await context.Payments
                .Include(p => p.Coach).
                ThenInclude(coach => coach.User)
                .Include(p => p.CoachService)
                .Include(p => p.Athlete).
                ThenInclude(athlete => athlete.User)
                .Include(p => p.WorkoutProgram)
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
                await context.SaveChangesAsync();
                return new ApiResponse()
                {
                    Action = false,
                    Message = "!!پرداخت توسط کاربر لغو شد."
                };
            }

            switch (payment.PaymentStatus)
            {
                case PaymentStatus.FAILED:
                    return new ApiResponse()
                    {
                        Action = false,
                        Message = "!!پرداخت ناموفق"
                    };
                case PaymentStatus.SUCCESS:
                    return new ApiResponse()
                    {
                        Action = true,
                        Message = "پرداخت با موفقیت انجام شد و قبلا تایید شده است  ",
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

                        if (payment.Athlete.User.FirstName != null)
                        {
                            await smsService.AthleteSuccessfullySmsNotification(
                                payment.Athlete.PhoneNumber,
                                payment.Athlete.User.FirstName, payment.CoachService.Title);
                        }

                        if (payment.Coach.User.FirstName != null)
                        {
                            await smsService.CoachServiceBuySmsNotification(payment.Coach.PhoneNumber,
                                payment.Coach.User.FirstName, payment.CoachService.Title,
                                payment.Amount.ToString(CultureInfo.CurrentCulture));
                        }

                        return new ApiResponse
                        {
                            Action = true,
                            Message = "پرداخت با موفقیت انجام شد ",
                            Result = result.Data.Ref_id
                        };
                    }
                    case { Code: 101 }:
                        return new ApiResponse
                        {
                            Action = true,
                            Message = "پرداخت با موفقیت انجام شد و قبلا تایید شده است  ",
                            Result = result.Data.Ref_id
                        };
                }

                if (result?.Errors is null)
                    return new ApiResponse
                    {
                        Action = false,
                        Message = "خطا ناشناخته",
                    };
                payment.PaymentStatus = PaymentStatus.FAILED;
                await context.SaveChangesAsync();

                // var error = result?.Errors?.ToString() ??
                //             "Unknown error from payment gateway.";
                return new ApiResponse
                {
                    Action = false,
                    Message = "پرداخت ناموفق",
                    // Result = error
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

        public async Task<ApiResponse> BuyCoachingService(string phoneNumber, int coachingServiceId)
        {
            var athlete = await context.Athletes
                .Include(x => x.AthleteQuestions)
                .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);

            if (athlete == null)
                return new ApiResponse { Message = "User is not an athlete", Action = false };

            var lastQuestion = athlete.AthleteQuestions.LastOrDefault();
            if (lastQuestion == null)
                return new ApiResponse { Message = "User has not completed the questions", Action = false };

            var coachService = await context.CoachServices
                .Include(x => x.Coach)
                .FirstOrDefaultAsync(x => x.Id == coachingServiceId && x.IsActive);

            if (coachService == null)
                return new ApiResponse { Message = "CoachingService not found", Action = false };

            if (coachService.IsDeleted || !coachService.IsActive)
                return new ApiResponse { Message = "CoachingService is deleted or not active", Action = false };

            var zarinPalResponse = await zarinPal.RequestPaymentAsync(new ZarinPalPaymentRequestDto
            {
                amount = (long)coachService.Price,
                callback_url = "https://chaarset.ir/payment/verify",
                description = "خرید",
                Mobile = athlete.PhoneNumber
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
                Athlete = athlete,
                AthleteId = athlete.Id,
                CoachService = coachService,
                CoachServiceId = coachService.Id,
                CoachId = coachService.CoachId,
                Coach = coachService.Coach,
                Authority = zarinPalResponse.Authority,
                Amount = coachService.Price,
                AthleteQuestion = lastQuestion
            };

            await context.Payments.AddAsync(payment);
            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "get url successfully",
                Result = zarinPalResponse
            };
        }

        public async Task<ApiResponse> SearchCoaches(CoachNameSearchDto coachNameSearchDto)
        {
            if (string.IsNullOrWhiteSpace(coachNameSearchDto.FullName))
            {
                return new ApiResponse()
                {
                    Message = "No search term provided",
                    Action = true,
                    Result = new List<CoachForSearch>() // Return an empty list
                };
            }

            // --- The Main Optimization ---
            var coaches = await context.Users
                .AsNoTracking()
                .Where(c => c.TypeOfUser == TypeOfUser.COACH &&
                            (c.FirstName.Contains(coachNameSearchDto.FullName) ||
                             c.LastName.Contains(coachNameSearchDto.FullName) ||
                             (c.FirstName + " " + c.LastName).Contains(coachNameSearchDto.FullName)))
                .Select(c => c.ToCoachForSearch())
                .ToListAsync();

            return new ApiResponse()
            {
                Message = "Coaches found",
                Action = true,
                Result = coaches
            };
        }

        public async Task<ApiResponse> GetLastQuestion(string phoneNumber)
        {
            var athlete = await context.Athletes.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (athlete is null)
            {
                return new ApiResponse() { Message = "User is not an athlete", Action = false };
            }

            var lastQuestion = await context.AthleteQuestions
                .Where(q => q.AthleteId == athlete.Id).Include(i => i.InjuryArea)
                .OrderByDescending(q => q.CreatedAt)
                .FirstOrDefaultAsync();
            if (lastQuestion is null) return new ApiResponse() { Message = "Question not found", Action = false };

            return new ApiResponse()
            {
                Message = "Question found",
                Action = true,
                Result = lastQuestion.ToAthleteQuestionDto()
            };
        }

        public async Task<ApiResponse> SubmitAthleteQuestions(string phoneNumber, AthleteQuestionDto athleteQuestionDto)
        {
           
            var athlete = await context.Athletes
                .Include(a => a.User)
                .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);

            if (athlete is null)
            {
                return new ApiResponse() { Message = "User is not an athlete", Action = false };
            }

        

           
            athlete.User.BirthDate = Convert.ToDateTime(athleteQuestionDto.BirthDay);

            var athleteQuestion = athleteQuestionDto.ToAthleteQuestion(athlete);

            if (athleteQuestionDto.AthleteBodyImageId > 0)
            {
                var athleteImage = await context.AthleteImage
                    .FirstOrDefaultAsync(ai => ai.Id == athleteQuestionDto.AthleteBodyImageId);
                if(athleteImage is not null){
                    athleteImage.AthleteQuestion = athleteQuestion;
                }

                
            }
    
        
            athlete.AthleteQuestions.Add(athleteQuestion);
    
       
            await context.SaveChangesAsync();

            return new ApiResponse()
            {
                Message = "Athlete questions submitted successfully",
                Action = true
            };
        }


}