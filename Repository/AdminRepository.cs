using System.Net;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Actions;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;
using sport_app_backend.Models.Question.A_Question;
using sport_app_backend.Models.TrainingPlan;
using sport_app_backend.Services;

namespace sport_app_backend.Repository
{
    public class AdminRepository(ApplicationDbContext context, ISmsService sms,    IStorage storage) : IAdminRepository
    {

    

        public async Task<ApiResponse> AddExercises(AddExercisesRequestDto exercises)
        {
            try
            {
                var exe = await context.Exercises.AddAsync(exercises.ToExercise());
                
                await context.SaveChangesAsync();
                return new ApiResponse()
                {
                    Action = true,
                    Message = "Exercises added",
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new ApiResponse()
                {
                    Action = false,
                    Message = e.Message + exercises.PersianName,
                };

            }


        }

        public async Task<ApiResponse> ConfirmTransactionId(string transactionId)
        {
            var payment = await context.Payments.Include(p => p.Coach).Include(z=>z
                    .CoachService)
                .Include(p => p.Athlete).Include(payment => payment.WorkoutProgram).FirstOrDefaultAsync(x => x.Authority == transactionId);
            if (payment is null) return new ApiResponse() { Message = "Payment not found", Action = false };
            payment.CoachService.NumberOfSell += 1;
            var workoutProgram = new WorkoutProgram()
            {
                Title = payment.CoachService.Title,
                Coach = payment.Coach,
                Athlete = payment.Athlete,
                AthleteId = payment.Athlete.Id,
                CoachId = payment.Coach.Id,
                Payment = payment,
                PaymentId = payment.Id

            };
            payment.WorkoutProgram = workoutProgram;
            await context.WorkoutPrograms.AddAsync(workoutProgram);
            payment.PaymentStatus = PaymentStatus.SUCCESS;
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "Payment confirmed successfully",
                Action = true
            };

          
        }
        public async Task<ApiResponse> EditTotalSessionCount()
        {
            var allPrograms = await context.WorkoutPrograms
                .Include(p => p.TrainingSessions)
                .Where(wp => wp.TotalSessionCount==0&&wp.Status==WorkoutProgramStatus.ACTIVE)
                .ToListAsync();

            int updatedProgramsCount = 0;

            foreach (var program in allPrograms)
            {
                program.TotalSessionCount = program.TrainingSessions.Count;

                program.CompletedSessionCount = program.TrainingSessions
                    .Count(ts => ts.TrainingSessionStatus == TrainingSessionStatus.COMPLETED);

              
                updatedProgramsCount++;
            }

            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = $"{updatedProgramsCount} برنامه تمرینی با موفقیت به‌روزرسانی و پر شد."
            };
        }

        public async Task<ApiResponse> VerifiedCoach(string coachPhoneNumber, string? siteUrl)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == coachPhoneNumber);
            if (coach is null)
                return new ApiResponse() { Message = "coach not found", Action = false };

            coach.Verified = true;
            if (siteUrl is not null)
            {
                coach.WebSiteUrl = siteUrl;
            }
            await  context.SaveChangesAsync();

            
            
            return new ApiResponse()
            {
                Message = "coach verified successfully",
                Action = true
            };
        }
        public async Task<ApiResponse> GetAllCoachPayouts()
        {
            var payouts = await context.CoachPayouts
                .Include(p => p.Coach)
                .ThenInclude(c => c.User)
                .OrderByDescending(p => p.RequestDate)
                .ToListAsync();

            var payoutDtos = payouts.Select(p => new
            {
                p.Id,
                CoachName = $"{p.Coach.User.FirstName} {p.Coach.User.LastName}",
                p.Amount,
                p.RequestDate,
                p.Status,
                p.PaidDate,
                p.TransactionReference,
                p.Imagelink
                
            });

            return new ApiResponse { Action = true,
                Message = "دریافت موفق لیست",Result = payoutDtos };
            
        }

        public async Task<ApiResponse> UpdateCoachPayoutStatus(int payoutId, PayoutStatus newStatus,
            string? transactionReference, IFormFile? file)
        {
            var payout = await context.CoachPayouts
                .Include(p => p.Coach)
                .FirstOrDefaultAsync(p => p.Id == payoutId);

            if (payout == null)
            {
                return new ApiResponse { Action = false, Message = "درخواست تسویه یافت نشد." };
            }
            

            if (payout.Status is PayoutStatus.Paid or PayoutStatus.Rejected)
            {
                return new ApiResponse { Action = false, Message = $"وضعیت تسویه حساب قبلاً به {payout.Status} تغییر کرده است و قابل تغییر نیست." };
            }

            payout.Status = newStatus;
            var imageLink = "";
            if (file != null)
            {
                var urlLink = await storage.UploadImage(file, "","coachPayout");
                if (urlLink.Action)
                {
                    imageLink = (string)urlLink.Result!;
                }
            }


            switch (newStatus)
            {
                case PayoutStatus.Paid:
                    payout.PaidDate = DateTime.Now;
                    payout.TransactionReference = transactionReference;
                    payout.Coach.Amount -= payout.Amount;
                    payout.Imagelink = imageLink;
                    break;
                case PayoutStatus.Rejected:
                    break;
            }

            await context.SaveChangesAsync();

            return new ApiResponse { Action = true, Message = "وضعیت تسویه با موفقیت به روز رسانی شد" };
        }

        public async Task<ApiResponse> GetCoachService(string coachWebSiteUrl)
        {
         
            var coach = await context.Coaches
                .Include(c => c.CoachingServices)
                .Include(c => c.User)
                .Include(c=>c.AthleteChangePhotos)
                .FirstOrDefaultAsync(c => c.WebSiteUrl == coachWebSiteUrl);

            if (coach == null)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = "Coach not found with the provided phone number.",
                    Result = null
                };
            }

            if (!coach.ShowWebsite)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = "وبسایت شما تایید نشده است.",
                    Result = null
                };
            }

          
            var coachingServiceDtos = coach.CoachingServices.Where(c=>c is { IsDeleted: false, IsActive: true })
                .Select(cs => cs.ToCoachingServiceResponse())
                .ToList();
            var socialMediaLink = new
            {
                coach.InstagramLink,
                coach.BaleUserName,
                coach.TelegramLink,
                coach.EitaaUserName,
                coach.WhatsApp
            };
            var athleteChange = coach.AthleteChangePhotos.Select(x => new
            {
                x.Title,
                x.Description,
                x.PhotoUrl,

            });

      
            var workoutProgramFeedBack = await context.WorkoutProgramFeedback
                .Where(fb => fb.IsChosen && fb.CouchId == coach.Id)
                .ToListAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "Coach found successfully.",
                Result = new
                {
                    coachingServiceDto = coachingServiceDtos,
                    workoutProgramFeedBack,
                    socialMediaLink,
                    coach.User.ImageProfile,
                    athleteChange,
                    coach.Slogan,
                    coach.SiteDescription,
                    coach.User.FirstName,
                    coach.User.LastName,
                }
            };
        }


        public async Task<SmsResponse> SendMassageToCoach( string phoneNumber, string message)
        {
         
      
            var result = sms.SendSms(phoneNumber, message);
            return await result;
        }

        public async Task<ApiResponse> GetSupportApp()
        {
            var result = await context.SupportApp.AsNoTracking().Where(ap=>ap.IsActive).Select(rp=>new {
                rp.Id,
                rp.User.FirstName,
                rp.User.LastName,
                rp.User.TypeOfUser,
                rp.User.PhoneNumber,
                rp.Category,
                rp.Description
                
            }).ToListAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "نمدونم برات چی بنویسم ولی بدون کار میکنه",
                Result = result.Select(rp=>new
                {
                    rp.Id,
                    Name = rp.FirstName+" "+rp.LastName,
                    rp.PhoneNumber,
                    Category = rp.Category.ToString(),
                    rp.Description,
                    TypeOfUser = rp.TypeOfUser.ToString()
                }).ToList()
            };

        }

        public async Task<ApiResponse> AddSlug(string engName, string slug)
        {
            var result =  await context.Exercises.FirstOrDefaultAsync(c => c.ImageLink == engName);
            if (result is null)
                return new ApiResponse()
                {
                    Action = false,
                    Message = engName
                };
            result.Slug =  slug;
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "true"
            };
        }

        public async Task<ApiResponse> AnswerSupportApp(int id)
        {
            var supportApp = await context.SupportApp.FirstOrDefaultAsync(ap=>ap.Id==id);
            if (supportApp != null) supportApp.IsActive = false;
            await context.SaveChangesAsync();
            return new ApiResponse
            {
                Action = true,
                Message = "پاسخ داده شد"
            };
        }

        public async Task<ApiResponse> SetCoachWebsiteUrl(string phoneNumber, string webSiteUrl)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
            if (coach is null)
            {
                return new ApiResponse()
                {
                    Action = false,
                    Message = "coach Not Found"
                };
            }
            coach.WebSiteUrl = webSiteUrl;
            await context.SaveChangesAsync();
            return new ApiResponse()
            {
                Action = true,
                Message = "success"
            };
        }

        public async Task<ApiResponse> GetVerifiedCoaches()
        {
            var coaches = await context.Coaches.Include(c=>c.User)
                .Select(c=>new
                {
                    c.User.FirstName,
                    c.User.LastName,
                    c.User.ImageProfile,
                    c.Verified,
                    coachSlug= c.WebSiteUrl
                }).Where(c=>c.Verified).ToListAsync();
            if (coaches.Count==0)
            {
                return new ApiResponse()
                {
                    Action = false,
                    Message = "coaches Not Found"
                };
            }

            return new ApiResponse()
            {
                Action = true,
                Message = "success",
                Result = coaches
            };

        }

        public async Task<ApiResponse> ActiveShowWebsiteCoach(string coachPhoneNumber)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == coachPhoneNumber);
            if (coach is null)
                return new ApiResponse() { Message = "coach not found", Action = false };
            coach.ShowWebsite = true;
            await  context.SaveChangesAsync();
            return new ApiResponse()
            {
                Message = "coach verified successfully",
                Action = true
            };        }
    }
    
}