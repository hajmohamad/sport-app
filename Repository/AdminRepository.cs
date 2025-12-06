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
using sport_app_backend.Services;

namespace sport_app_backend.Repository
{
    public class AdminRepository(ApplicationDbContext context, ISmsService sms,    ILiaraStorage liaraStorage) : IAdminRepository
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
        public async Task<ApiResponse> BackfillWorkoutProgramStats()
        {
            var allPrograms = await context.WorkoutPrograms
                .Include(p => p.TrainingSessions)
                .ToListAsync();

            int updatedProgramsCount = 0;

            foreach (var program in allPrograms)
            {
                program.TotalSessionCount = program.TrainingSessions.Count;

                program.CompletedSessionCount = program.TrainingSessions
                    .Count(ts => ts.TrainingSessionStatus == TrainingSessionStatus.COMPLETED);

              
                var lastExerciseActivity = await context.Activities
                    .Where(a => a.AthleteId == program.AthleteId && a.ActivityCategory == ActivityCategory.EXERCISE)
                    .OrderByDescending(a => a.Date)
                    .FirstOrDefaultAsync();
                
                if (lastExerciseActivity != null)
                {
                    program.LastExerciseDate = lastExerciseActivity.Date;
                }
                
                updatedProgramsCount++;
            }

            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = $"{updatedProgramsCount} برنامه تمرینی با موفقیت به‌روزرسانی و پر شد."
            };
        }

        public async Task<ApiResponse> VerifiedCoach(string coachPhoneNumber)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == coachPhoneNumber);
            if (coach is null)
                return new ApiResponse() { Message = "coach not found", Action = false };

            coach.Verified = true;

            var athlete = await context.Athletes.FirstOrDefaultAsync(a => a.PhoneNumber == "09395327229");
            if (athlete is null)
                return new ApiResponse() { Message = "athlete not found", Action = false };
            var athleteQuestion = await context.AthleteQuestions.FirstOrDefaultAsync(a=>a.AthleteId == athlete.Id);
            if (athleteQuestion is null)
                return new ApiResponse() { Message = "athlete not found", Action = false };

            var coachService = new CoachService
            {
                Coach = coach,
                Title = "برنامه ورزشی تستی",
                Description = "این یک برنامه ورزشی تستی هست",
                Price = 0,
                IsActive = false,
                IsDeleted = false
            };

            context.CoachServices.Add(coachService);
            await context.SaveChangesAsync(); // حالا Id ساخته میشه ✅


            var payment = new Payment
            {
                Coach = coach,
                Athlete = athlete,
                AthleteId = athlete.Id,
                CoachId = coach.Id,
                CoachServiceId = coachService.Id,
                PaymentStatus = PaymentStatus.SUCCESS,
                Amount = 0,
                Authority = "nothing",
                AppFee = 0,
                AthleteQuestionId = athleteQuestion.Id,
                
            };

            context.Payments.Add(payment);
            await context.SaveChangesAsync(); // حالا Id ساخته میشه ✅


            var workoutProgram = new WorkoutProgram
            {
                Title = coachService.Title,
                Coach = coach,
                Athlete = athlete,
                AthleteId = athlete.Id,
                CoachId = coach.Id,
                PaymentId = payment.Id,
                Status = WorkoutProgramStatus.NOTSTARTED,
            };

            context.WorkoutPrograms.Add(workoutProgram);
            await context.SaveChangesAsync();

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
                var urlLink = await liaraStorage.UploadImage(file, "","coachPayout");
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

        public async Task<ApiResponse> GetCoachService(string phoneNumber)
        {
            var coachingServiceDto = await context.CoachServices
                .Where(u =>u.IsDeleted!=true && u.Coach.PhoneNumber == phoneNumber)
                .ToListAsync();
          
            return new ApiResponse
            {
                Action = true, Message = "Coach found",
                Result = coachingServiceDto.Select(cs=>cs.ToCoachingServiceResponse()).ToList()
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
    }
    
}