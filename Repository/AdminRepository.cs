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
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using sport_app_backend.Models.Support;


namespace sport_app_backend.Repository
{
    public class AdminRepository(ApplicationDbContext context, ISmsService sms,    IStorage storage) : IAdminRepository
    {
          // دریافت لیست تیکت‌ها همراه با فیلتر اختیاری بر اساس وضعیت
        public async Task<ApiResponse> GetAllSupportTicketsAsync(TicketStatus? status = null)
        {
            var query = context.SupportTickets
                .Include(t => t.User)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            var tickets = await query
                .OrderByDescending(t => t.UpdatedAt)
                .Select(t => new AdminTicketListDto
                {
                    Id = t.Id,
                    Title = t.Subject,
                    Status = t.Status,
                    Category = t.Category,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    UserId = t.UserId,
                    UserFullName = t.User != null ? (t.User.FirstName + " " + t.User.LastName).Trim() : "کاربر بدون نام",
                    UserPhoneNumber = t.User != null ? t.User.PhoneNumber : string.Empty
                })
                .ToListAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "لیست تیکت‌ها با موفقیت دریافت شد.",
                Result = tickets
            };
        }

        // دریافت جزئیات تیکت به همراه تمامی پیام‌های ثبت شده
        public async Task<ApiResponse> GetSupportTicketDetailsAsync(int ticketId)
        {
            var ticket = await context.SupportTickets
                .Include(t => t.User)
                .Include(t => t.Messages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = "تیکت مورد نظر یافت نشد."
                };
            }

            var messagesDto = ticket.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new TicketMessageDto
                {
                    Id = m.Id,
                    MessageText = m.MessageText,
                    CreatedAt = m.CreatedAt,
                    IsFromSupport = m.IsFromSupport,
                    SenderName = m.IsFromSupport ? "پشتیبان نرم‌افزار" : (m.Sender != null ? (m.Sender.FirstName + " " + m.Sender.LastName).Trim() : "کاربر"),
                    SenderImage = m.IsFromSupport ? string.Empty : (m.Sender != null ? m.Sender.ImageProfile : string.Empty)
                })
                .ToList();

            var ticketDetails = new AdminTicketDetailsDto
            {
                Id = ticket.Id,
                Title = ticket.Subject,
                Status = ticket.Status,
                Category = ticket.Category,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                UserId = ticket.UserId,
                UserFullName = ticket.User != null ? (ticket.User.FirstName + " " + ticket.User.LastName).Trim() : "کاربر بدون نام",
                UserPhoneNumber = ticket.User != null ? ticket.User.PhoneNumber : string.Empty,
                Messages = messagesDto
            };

            return new ApiResponse
            {
                Action = true,
                Message = "جزئیات تیکت با موفقیت دریافت شد.",
                Result = ticketDetails
            };
        }

        public async Task<ApiResponse> ReplyToSupportTicketAsync(int ticketId, ReplyTicketDto dto)
        {
            var ticket = await context.SupportTickets.Include(st=>st.User).FirstOrDefaultAsync(t => t.Id == ticketId);
            
            if (ticket == null)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = "تیکت یافت نشد."
                };
            }

            if (ticket.Status == TicketStatus.Closed)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = "این تیکت قبلا بسته شده است و امکان ثبت پاسخ وجود ندارد."
                };
            }

            var newMessage = new TicketMessage
            {
                TicketId = ticketId,
                MessageText = dto.MessageText,
                CreatedAt = DateTime.UtcNow,
                IsFromSupport = true,
                SenderId = null // فرستنده ادمین است و ایدی در دیتابیس null ثبت می‌شود
            };

            ticket.Status = TicketStatus.Answered;
            ticket.UpdatedAt = DateTime.UtcNow;

            await context.TicketMessages.AddAsync(newMessage);
            await context.SaveChangesAsync();
            await sms.SupportTicketAnsweredSms(ticket.User.PhoneNumber, ticket.Subject);

            return new ApiResponse
            {
                Action = true,
                Message = "پاسخ پشتیبانی با موفقیت ثبت شد.",
                Result = new TicketMessageDto
                {
                    Id = newMessage.Id,
                    MessageText = newMessage.MessageText,
                    CreatedAt = newMessage.CreatedAt,
                    IsFromSupport = true,
                    SenderName = "پشتیبان نرم‌افزار",
                    SenderImage = string.Empty
                }
            };
        }

        // بستن تیکت پشتیبانی
        public async Task<ApiResponse> CloseSupportTicketAsync(int ticketId)
        {
            var ticket = await context.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                return new ApiResponse
                {
                    Action = false,
                    Message = "تیکت یافت نشد."
                };
            }

            ticket.Status = TicketStatus.Closed;
            ticket.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return new ApiResponse
            {
                Action = true,
                Message = "تیکت با موفقیت بسته شد."
            };
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

        public async Task<ApiResponse> VerifiedCoach(string coachPhoneNumber)
        {
            var coach = await context.Coaches.FirstOrDefaultAsync(c => c.PhoneNumber == coachPhoneNumber);
            if (coach is null)
                return new ApiResponse() { Message = "coach not found", Action = false };

            coach.Verified = true;
         
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
                .Where(fb => fb.IsChosen && fb.CoachId == coach.Id)
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