using System.Globalization;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;
using sport_app_backend.Dtos;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;

namespace sport_app_backend.Repository;

public class InAppMessageRepository(ApplicationDbContext db) : IInAppMessageRepository
{

    public async Task<ApiResponse> CreateMessage(CreateInAppMessageDto dto)
    {
        if (!Enum.TryParse<TypeOfUser>(dto.TargetRole.ToUpper(), out var targetRole))
        {
            return new ApiResponse { Action = false, Message = "نقش هدف نامعتبر است." };
        }

        var message = new InAppMessage
        {
            Title = dto.Title,
            Message = dto.Message,
            ActionLink = dto.ActionLink,
            ActionText = dto.ActionText,
            TargetRole = targetRole,
            CreatedAt = DateTime.Now
        };

        db.InAppMessages.Add(message);
        await db.SaveChangesAsync();

        return new ApiResponse { Action = true, Message = "پیام با موفقیت ایجاد شد.", Result = message.Id };
    }


    public async Task<ApiResponse> GetMessages(string phoneNumber)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user == null)
            return new ApiResponse { Action = false, Message = "کاربر یافت نشد." };

        var userRole = user.TypeOfUser;

        var messages = await db.InAppMessages
            .Where(m => m.TargetRole == userRole)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new
            {
                Message = m,
                Status = db.UserMessageStatuses
                    .FirstOrDefault(s => s.UserId == user.Id && s.InAppMessageId == m.Id)
            })
            .ToListAsync();

        var result = messages.Select(x => new InAppMessageDto
        {
            Id = x.Message.Id,
            Title = x.Message.Title,
            Message = x.Message.Message,
            ActionLink = x.Message.ActionLink,
            ActionText = x.Message.ActionText,
            IsRead = x.Status?.IsRead ?? false,
            CreatedAt = x.Message.CreatedAt.ToString(),
        }).ToList();

        return new ApiResponse
        {
            Action = true,
            Message = "پیام‌ها دریافت شد.",
            Result = result
        };
    }

    public async Task<ApiResponse> GetMessageDetail(string phoneNumber, int messageId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user == null)
            return new ApiResponse { Action = false, Message = "کاربر یافت نشد." };

        var message = await db.InAppMessages.FirstOrDefaultAsync(m => m.Id == messageId);
        if (message == null)
            return new ApiResponse { Action = false, Message = "پیام یافت نشد." };

        var status = await db.UserMessageStatuses
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.InAppMessageId == messageId);

        if (status == null)
        {
            status = new UserMessageStatus
            {
                UserId = user.Id,
                InAppMessageId = messageId,
                IsRead = true,
                ReadAt = DateTime.Now
            };
            db.UserMessageStatuses.Add(status);
        }
        else
        {
            status.IsRead = true;
            status.ReadAt = DateTime.Now;
        }

        await db.SaveChangesAsync();

        return new ApiResponse
        {
            Action = true,
            Message = "جزئیات پیام",
            Result = new InAppMessageDto
            {
                Id = message.Id,
                Title = message.Title,
                Message = message.Message,
                ActionLink = message.ActionLink,
                ActionText = message.ActionText,
                IsRead = true,
                CreatedAt = message.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            }
        };
    }

    public async Task<ApiResponse> MarkAsRead(string phoneNumber, int messageId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user == null)
            return new ApiResponse { Action = false, Message = "کاربر یافت نشد." };

        var status = await db.UserMessageStatuses
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.InAppMessageId == messageId);

        if (status == null)
        {
            db.UserMessageStatuses.Add(new UserMessageStatus
            {
                UserId = user.Id,
                InAppMessageId = messageId,
                IsRead = true,
                ReadAt = DateTime.Now
            });
        }
        else
        {
            status.IsRead = true;
            status.ReadAt = DateTime.Now;
        }

        await db.SaveChangesAsync();
        return new ApiResponse { Action = true, Message = "پیام خوانده شد." };
    }


    public async Task<ApiResponse> MarkAllAsRead(string phoneNumber)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user == null)
            return new ApiResponse { Action = false, Message = "کاربر یافت نشد." };

        var userRole = user.TypeOfUser;

        var unreadMessageIds = await db.InAppMessages
            .Where(m => m.TargetRole == userRole)
            .Where(m => !db.UserMessageStatuses
                .Any(s => s.UserId == user.Id && s.InAppMessageId == m.Id && s.IsRead))
            .Select(m => m.Id)
            .ToListAsync();

        foreach (var msgId in unreadMessageIds)
        {
            var existing = await db.UserMessageStatuses
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.InAppMessageId == msgId);

            if (existing == null)
            {
                db.UserMessageStatuses.Add(new UserMessageStatus
                {
                    UserId = user.Id,
                    InAppMessageId = msgId,
                    IsRead = true,
                    ReadAt = DateTime.Now
                });
            }
            else
            {
                existing.IsRead = true;
                existing.ReadAt = DateTime.Now;
            }
        }

        await db.SaveChangesAsync();
        return new ApiResponse { Action = true, Message = "همه پیام‌ها خوانده شدند." };
    }


    public async Task<int> GetUnreadCount(string phoneNumber)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user == null) return 0;

        var userRole = user.TypeOfUser;

        return await db.InAppMessages
            .Where(m => m.TargetRole == userRole)
            .CountAsync(m => !db.UserMessageStatuses
                .Any(s => s.UserId == user.Id && s.InAppMessageId == m.Id && s.IsRead));
    }


}