using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using sport_app_backend.Data;
using sport_app_backend.Dtos.Chat;
using sport_app_backend.Hubs;
using sport_app_backend.Interface;
using sport_app_backend.Mappers;
using sport_app_backend.Models;
using sport_app_backend.Models.Account;
using sport_app_backend.Models.Chat;
using sport_app_backend.Models.Payments;
using sport_app_backend.Models.Program;
using sport_app_backend.Services.Cash;

namespace sport_app_backend.Repository;

public class ChatRepository(
    ApplicationDbContext context,
    IHubContext<ChatHub> hubContext,
    IStorage storage,
    IConfiguration configuration,
    WorkoutProgramCacheService workoutCache) : IChatRepository
{
    private const int DefaultMessageTake = 50;
    private const int MinimumMessageTake = 20;
    private const int MaximumMessageTake = 100;
    private const long MaximumAttachmentSize = 10 * 1024 * 1024;


    public async Task<ApiResponse> GetConversationMessages(
        int userId,
        long conversationId,
        long? beforeMessageId,
        int take)
    {
        if (userId <= 0 || conversationId <= 0)
        {
            return Failure("اطلاعات گفتگو نامعتبر است.");
        }

        var conversationParticipant = await context.ConversationParticipants
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .Include(conversationParticipant => conversationParticipant.User)
            .ToListAsync();
            
        var userParticipant = conversationParticipant.FirstOrDefault(x => x.UserId == userId);

        if (userParticipant is null)
        {
            return Failure("شما عضو این گفتگو نیستید.");
        }
        
        var otherParticipant = conversationParticipant
            .FirstOrDefault(x => x.UserId != userId);
        if (otherParticipant is null)
        {
            return Failure("کاربر مقابل پیدا نشد");
        }
        

        take = NormalizeMessageTake(take);
        var messagesQuery = context.ChatMessages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId);

        if (beforeMessageId.HasValue)
        {
            if (beforeMessageId.Value <= 0)
            {
                return Failure("شناسه پیام نامعتبر است.");
            }

            messagesQuery = messagesQuery.Where(x => x.Id < beforeMessageId.Value);
        }

        var athleteStatus = "";

        if (otherParticipant.Role == ConversationParticipantRole.Athlete)
        {
            var programs = await workoutCache.GetCoachWorkoutProgramAthleteUserIdByCoachUserId(userId);

            if (programs.TryGetValue(otherParticipant.UserId, out var program))
            {
                athleteStatus = program.GetStatus();
            }
        }

        var otherUserNameAndPhoto= new
            {
                FullName = GetFullName(otherParticipant!.User),
                Photo = otherParticipant.User.ImageProfile,
                phoneNumber=otherParticipant.User.PhoneNumber,
                athleteStatus
            };
        

        var messages = await messagesQuery
            .OrderByDescending(x => x.Id)
            .Take(take)
            .ToListAsync();

        var messageList = messages
            .OrderBy(x => x.Id)
            .Select(x => x.ChatMessageDto(userId, otherParticipant.LastReadMessageId))
            .ToList();
        
        await context.ConversationParticipants
            .Where(x =>
                x.ConversationId == conversationId &&
                x.UserId == userId)
            .ExecuteUpdateAsync(update =>
                update.SetProperty(x => x.UnreadCount, 0));
        
        return Success("پیام‌های گفتگو با موفقیت دریافت شدند.", new
        {
            messageList,
            otherUserNameAndPhoto
        });
    }

public async Task<ApiResponse> SendMessage(int senderUserId, SendMessageDto dto)
{
    if (senderUserId <= 0)
    {
        return Failure("شناسه ارسال‌کننده نامعتبر است.");
    }
    
    var normalizedText = NormalizeMessageText(dto.Text);

    if (string.IsNullOrWhiteSpace(normalizedText))
    {
        return Failure("متن پیام نمی‌تواند خالی باشد.");
    }

    var resolvedConversation = await ResolveConversationIdOrCreateSupport(
        senderUserId,
        dto.ConversationId);

    if (!resolvedConversation.Action)
    {
        return Failure(resolvedConversation.Message);
    }
    var conversationId = resolvedConversation.ConversationId;
    var conversation = await context.Conversations
        .Include(x => x.Participants)
            .ThenInclude(p => p.User) 
        .FirstOrDefaultAsync(x => x.Id == conversationId);

    if (conversation is null)
    {
        return Failure("گفتگو یافت نشد.");
    }

    if (conversation.IsClosed)
    {
        return Failure("این گفتگو بسته شده است.");
    }
    
    var senderParticipant = conversation.Participants.FirstOrDefault(p => p.UserId == senderUserId);
    
    if (senderParticipant is null)
    {
        return Failure("شما عضو این گفتگو نیستید یا اطلاعات کاربری شما یافت نشد.");
    }

    var now = DateTime.UtcNow;

    var message = new ChatMessage
    {
        ConversationId = conversation.Id,
        SenderUserId = senderUserId,
        Type = ChatMessageType.Text,
        Text = normalizedText,
        SentAt = now,
        SenderUser = senderParticipant.User 
    };

    context.ChatMessages.Add(message);

    conversation.LastMessageAt = now;
    conversation.LastMessageText = BuildConversationPreview(message);
    
    var otherParticipant = conversation.Participants.FirstOrDefault(x => x.UserId != senderUserId);
    if (otherParticipant != null)
    {
        otherParticipant.UnreadCount++;
    }

    await context.SaveChangesAsync();

    conversation.LastMessageId = message.Id;
    
    await context.SaveChangesAsync();


    ChatMessageDto? senderMessageDto = null;

    foreach (var participant in conversation.Participants)
    {
        var otherParticipantLastReadMessageId = conversation.Participants
            .FirstOrDefault(x => x.UserId != participant.UserId)
            ?.LastReadMessageId;

       
        var messageDto = message.ChatMessageDto(
            participant.UserId,
            otherParticipantLastReadMessageId);

        if (participant.UserId == senderUserId)
        {
            senderMessageDto = messageDto;
        }

        await hubContext.Clients
            .Group($"user_{participant.UserId}")
            .SendAsync("ReceiveMessage", messageDto);

        await hubContext.Clients
            .Group($"user_{participant.UserId}")
            .SendAsync("ChatListUpdated", new
            {
                conversation.Id,
                conversation.LastMessageId,
                conversation.LastMessageText,
                conversation.LastMessageAt,
                // ارسال شمارنده جدید تا UI به‌روز شود
                UnreadCount = participant.UnreadCount 
            });
    }

    return Success("پیام با موفقیت ارسال شد.", senderMessageDto);
}


    
public async Task<ApiResponse> MarkAsRead(
    int userId,
    long conversationId,
    long lastReadMessageId)
{
    if (userId <= 0 || conversationId <= 0 || lastReadMessageId <= 0)
    {
        return Failure("اطلاعات خواندن پیام نامعتبر است.");
    }

    var participant = await context.ConversationParticipants
        .Include(x => x.Conversation)
            .ThenInclude(x => x.Participants)
        .FirstOrDefaultAsync(x =>
            x.ConversationId == conversationId &&
            x.UserId == userId);

    if (participant is null)
    {
        return Failure("شما عضو این گفتگو نیستید.");
    }

    var messageExistsInConversation = await context.ChatMessages
        .AsNoTracking()
        .AnyAsync(x =>
            x.Id == lastReadMessageId &&
            x.ConversationId == conversationId);

    if (!messageExistsInConversation)
    {
        return Failure("پیام انتخاب‌شده متعلق به این گفتگو نیست.");
    }
    
    bool needsUpdate = false;
    
    if (participant.UnreadCount > 0)
    {
        participant.UnreadCount = 0;
        needsUpdate = true;
    }

    if (!participant.LastReadMessageId.HasValue || participant.LastReadMessageId.Value < lastReadMessageId)
    {
        participant.LastReadMessageId = lastReadMessageId;
        participant.LastReadAt = DateTime.UtcNow;
        needsUpdate = true;
    }

    if (!needsUpdate) return Success("پیام‌ها به‌عنوان خوانده‌شده ثبت شدند.");
    await context.SaveChangesAsync();

     
    await hubContext.Clients
        .Group($"chat_{conversationId}")
        .SendAsync("MessagesRead", new
        {
            ConversationId = conversationId,
            ReaderUserId = userId,
            LastReadMessageId = lastReadMessageId,
            LastReadAt = participant.LastReadAt
        });

    // ✅ اصلاح کلیدی: نوتیفیکیشن فقط به کاربر فعلی ارسال می‌شود
    await hubContext.Clients
        .Group($"user_{userId}") // <--- فقط به گروه کاربری که پیام را خوانده
        .SendAsync("ChatListUpdated", new
        {
            ConversationId = conversationId,
            UnreadCount = 0 // حالا این مقدار فقط برای UI کاربر صحیح ارسال می‌شود
        });

    return Success("پیام‌ها به‌عنوان خوانده‌شده ثبت شدند.");
}

    public async Task<ApiResponse> UploadAttachment(
    int userId,
    long? conversationId,
    IFormFile file)
{
    if (userId <= 0)
    {
        return Failure("شناسه کاربر نامعتبر است.");
    }

    if (file is null || file.Length <= 0)
    {
        return Failure("فایل انتخاب نشده است.");
    }

    if (file.Length > MaximumAttachmentSize)
    {
        return Failure("حجم فایل نباید بیشتر از ۱۰ مگابایت باشد.");
    }

    var contentType = file.ContentType?.Trim().ToLowerInvariant();

    if (string.IsNullOrWhiteSpace(contentType) ||
        !AllowedAttachmentContentTypes.Contains(
            contentType,
            StringComparer.OrdinalIgnoreCase))
    {
        return Failure("فقط تصویر و فایل PDF مجاز هستند.");
    }

    var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

    var isPdf = string.Equals(
        contentType,
        "application/pdf",
        StringComparison.OrdinalIgnoreCase);

    var isImage = contentType.StartsWith("image/");

    if (isPdf && extension != ".pdf")
    {
        return Failure("پسوند فایل PDF نامعتبر است.");
    }

    if (isImage)
    {
        var allowedImageExtensions = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".gif"
        };

        if (string.IsNullOrWhiteSpace(extension) ||
            !allowedImageExtensions.Contains(extension))
        {
            return Failure("پسوند تصویر نامعتبر است.");
        }
    }

    var resolvedConversation = await ResolveConversationIdOrCreateSupport(
        userId,
        conversationId);

    if (!resolvedConversation.Action)
    {
        return Failure(resolvedConversation.Message);
    }

    var resolvedConversationId = resolvedConversation.ConversationId;

    var conversation = await context.Conversations
        .Include(x => x.Participants)
            .ThenInclude(p => p.User) 
        .FirstOrDefaultAsync(x => x.Id == resolvedConversationId);

    if (conversation is null)
    {
        return Failure("گفتگو یافت نشد.");
    }

    if (conversation.IsClosed)
    {
        return Failure("این گفتگو بسته شده است.");
    }
    
    var senderParticipant = conversation.Participants.FirstOrDefault(p => p.UserId == userId);
    
    if (senderParticipant is null)
    {
        return Failure("شما عضو این گفتگو نیستید.");
    }
    
    var folderPath = $"chat/{resolvedConversationId}";
    var uploadResult = await storage.UploadFile(file, string.Empty, folderPath);

    if (!uploadResult.Action || uploadResult.Result is null)
    {
        return uploadResult;
    }

    var now = DateTime.UtcNow;

    var message = new ChatMessage
    {
        ConversationId = conversation.Id,
        SenderUserId = userId,
        Type = isPdf ? ChatMessageType.File : ChatMessageType.Image,
        SentAt = now,
        FileUrl = uploadResult.Result.ToString(),
        SenderUser = senderParticipant.User 
    };

    context.ChatMessages.Add(message);

    conversation.LastMessageAt = now;
    conversation.LastMessageText = BuildConversationPreview(message);
    
    var otherParticipant = conversation.Participants.FirstOrDefault(x => x.UserId != userId);
    if (otherParticipant != null)
    {
        otherParticipant.UnreadCount++;
    }

    await context.SaveChangesAsync(); 

    conversation.LastMessageId = message.Id;
    await context.SaveChangesAsync(); 


    ChatMessageDto? senderMessageDto = null;
    
    foreach (var participant in conversation.Participants)
    {
        var otherParticipantLastReadMessageId = conversation.Participants
            .FirstOrDefault(x => x.UserId != participant.UserId)
            ?.LastReadMessageId;

        var messageDto = message.ChatMessageDto(
            participant.UserId,
            otherParticipantLastReadMessageId);

        if (participant.UserId == userId)
        {
            senderMessageDto = messageDto;
        }

        await hubContext.Clients
            .Group($"user_{participant.UserId}")
            .SendAsync("ReceiveMessage", messageDto);

        await hubContext.Clients
            .Group($"user_{participant.UserId}")
            .SendAsync("ChatListUpdated", new
            {
                conversation.Id, 
                conversation.LastMessageId,
                conversation.LastMessageText,
                conversation.LastMessageAt,
                UnreadCount = participant.UnreadCount 
            });
    }

    return Success("فایل با موفقیت ارسال شد.", senderMessageDto);
}


    public async Task<ApiResponse> BackfillCoachAthleteConversationsFromSuccessfulPayments()
    {
        var pairs = await context.WorkoutPrograms
            .AsNoTracking()
            .Select(p => new
            {
                CoachUserId = p.Coach.UserId,
                AthleteUserId = p.Athlete.UserId
            })
            .Distinct()
            .ToListAsync();

        foreach (var pair in pairs)
        {
            await CreateCoachAthleteConversation(
                pair.CoachUserId,
                pair.AthleteUserId);
        }

        return Success("گفتگوهای مربی و ورزشکار بر اساس پرداخت‌های موفق بررسی و ایجاد شدند.");
    }

    public async Task<ApiResponse> CreateCoachAthleteConversation(int coachUserId, int athleteUserId)
    {
        if (coachUserId <= 0 || athleteUserId <= 0)
        {
            return Failure("شناسه کاربران نامعتبر است.");
        }

        if (coachUserId == athleteUserId)
        {
            return Failure("امکان ایجاد گفتگو با خود کاربر وجود ندارد.");
        }
        
        var existingConversation = await FindCoachAthleteConversation(coachUserId, athleteUserId);

        if (existingConversation is not null)
        {
            return Success("گفتگو از قبل وجود دارد.", existingConversation.Id);
        }

        var now = DateTime.UtcNow;

        var conversation = new Conversation
        {
            Type = ConversationType.CoachAthlete,
            CreatedAt = now,
            IsClosed = false,
            Participants =
            [
                new ConversationParticipant
                {
                    UserId = coachUserId,
                    Role = ConversationParticipantRole.Coach,
                    JoinedAt = now
                },
                new ConversationParticipant
                {
                    UserId = athleteUserId,
                    Role = ConversationParticipantRole.Athlete,
                    JoinedAt = now
                }
            ]
        };

        await context.Conversations.AddAsync(conversation);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            var existingAfterConflict = await FindCoachAthleteConversation(coachUserId, athleteUserId);

            if (existingAfterConflict is not null)
            {
                return Success("گفتگو از قبل وجود دارد.", existingAfterConflict.Id);
            }

            throw;
        }

        await NotifyConversationCreated(
            conversation.Id,
            ConversationType.CoachAthlete,
            coachUserId,
            athleteUserId);

        return Success("گفتگوی مربی و ورزشکار با موفقیت ایجاد شد.", conversation.Id);
    }

    private async Task<ApiResponse> CreateSupportConversation(int userId)
    {
        var supportUserId = GetSupportUserId();

        if (userId <= 0 || supportUserId <= 0)
        {
            return Failure("شناسه کاربر یا پشتیبان نامعتبر است.");
        }

        if (userId == supportUserId)
        {
            return Failure("کاربر پشتیبانی نمی‌تواند با خودش گفتگو داشته باشد.");
        }

        var userExists = await context.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == userId);

        var supportUserExists = await context.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == supportUserId);

        if (!userExists || !supportUserExists)
        {
            return Failure("کاربر یا حساب پشتیبانی یافت نشد.");
        }

        var existingConversation = await FindSupportConversation(userId, supportUserId);

        if (existingConversation is not null)
        {
            return Success("گفتگوی پشتیبانی از قبل وجود دارد.", existingConversation.Id);
        }

        var now = DateTime.UtcNow;

        var conversation = new Conversation
        {
            Type = ConversationType.UserSupport,
            CreatedAt = now,
            IsClosed = false,
            Participants =
            [
                new ConversationParticipant
                {
                    UserId = userId,
                    Role = ConversationParticipantRole.User,
                    JoinedAt = now
                },
                new ConversationParticipant
                {
                    UserId = supportUserId,
                    Role = ConversationParticipantRole.Support,
                    JoinedAt = now
                }
            ]
        };

        await context.Conversations.AddAsync(conversation);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            var existingAfterConflict = await FindSupportConversation(userId, supportUserId);

            if (existingAfterConflict is not null)
            {
                return Success("گفتگوی پشتیبانی از قبل وجود دارد.", existingAfterConflict.Id);
            }

            throw;
        }

        await NotifyConversationCreated(
            conversation.Id,
            ConversationType.UserSupport,
            userId,
            supportUserId);

        return Success("گفتگوی پشتیبانی با موفقیت ایجاد شد.", conversation.Id);
    }

    public async Task<ApiResponse> GetCoachChatList(int coachId, int coachUserId)
    {
        if (coachId <= 0)
        {
            return Failure("شناسه مربی نامعتبر است.");
        }
        
        var conversations = await GetCoachAthleteConversations(coachUserId);
        var coachPrograms= workoutCache.GetCoachWorkoutProgramAthleteUserIdByCoachUserId(coachUserId).Result;


        var result = new CoachChatListDto
        {
            Support = await GetSupportChatItem(coachUserId) 
        };

        foreach (var conversation in conversations)
        {
            var coachParticipants = conversation.Participants.FirstOrDefault(u => u.UserId==coachUserId);
            var athleteParticipant = conversation.Participants.FirstOrDefault(x => x.UserId != coachUserId);
            var program = coachPrograms[athleteParticipant!.UserId];

            var item = BuildChatListItem(conversation, athleteParticipant.User, program,coachParticipants!.UnreadCount,athleteParticipant.UnreadCount);

            switch (program.GetStatus())
            {
                case "Active":
                    result.Active.Add(item);
                    break;
                case "NeedsFollowUp":
                    result.NeedsFollowUp.Add(item);
                    break;
                case "NearingCompletion":
                    result.NearingCompletion.Add(item);
                    break;
                default:
                    result.Inactive.Add(item);
                    break;
            }
        }

        SortCoachResult(result);

        return Success("لیست چت‌های مربی با موفقیت دریافت شد.", result);
    }

    public async Task<ApiResponse> GetAthleteChatList(int athleteId)
    {
        if (athleteId <= 0)
        {
            return Failure("شناسه ورزشکار نامعتبر است.");
        }

        var athlete = await context.Athletes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == athleteId);

        if (athlete is null)
        {
            return Failure("ورزشکار یافت نشد.");
        }

        var athleteUserId = athlete.UserId;

        var conversations = await GetAthleteConversations(athleteUserId);
        
        var conversationIds = conversations.Select(c => c.Id).ToList();
        var unreadCountsDict = await GetUnreadCountsBatch(conversationIds, athleteUserId);

        var result = new AthleteChatListDto
        {
            Support = await GetSupportChatItem(athleteUserId)
        };

        foreach (var conversation in conversations)
        {
            var coach = conversation.Participants.FirstOrDefault(x => x.UserId != athleteUserId);
            if (coach == null) continue;
            
            unreadCountsDict.TryGetValue(conversation.Id, out int unreadCount);

            var item = BuildChatListItem(
                conversation,
                coach.User,
                null,
                unreadCount,
                coach.UnreadCount); 

            result.Coaches.Add(item);
        }

        result.Coaches = result.Coaches
            .OrderByDescending(x => x.LastMessageAt)
            .ThenBy(x => x.FullName)
            .ToList();

        return Success("لیست چت‌های ورزشکار با موفقیت دریافت شد.", result);
    }

    public async Task<ApiResponse> AddSystemMessage(long conversationId, string text)
    {
        if (conversationId <= 0)
        {
            return Failure("شناسه گفتگو نامعتبر است.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return Failure("متن پیام سیستمی نمی‌تواند خالی باشد.");
        }

        var conversation = await context.Conversations
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == conversationId);

        if (conversation is null)
        {
            return Failure("گفتگو یافت نشد.");
        }

        if (conversation.IsClosed)
        {
            return Failure("این گفتگو بسته شده است.");
        }

        var senderParticipant = conversation.Participants.FirstOrDefault();

        if (senderParticipant is null)
        {
            return Failure("هیچ عضوی برای گفتگو یافت نشد.");
        }

        var now = DateTime.UtcNow;

        var systemMessage = new ChatMessage
        {
            ConversationId = conversationId,
            SenderUserId = senderParticipant.UserId,
            Type = ChatMessageType.System,
            Text = text.Trim(),
            SentAt = now,
        };

        context.ChatMessages.Add(systemMessage);

        conversation.LastMessageAt = now;
        conversation.LastMessageText = BuildConversationPreview(systemMessage);

        await context.SaveChangesAsync();

        conversation.LastMessageId = systemMessage.Id;
        await context.SaveChangesAsync();

        var messageWithSender = await context.ChatMessages
            .AsNoTracking()
            .Include(x => x.SenderUser)
            .FirstAsync(x => x.Id == systemMessage.Id);

        var messageDto = messageWithSender.ChatMessageDto(senderParticipant.UserId, null);

        await hubContext.Clients
            .Group($"chat_{conversationId}")
            .SendAsync("ReceiveMessage", messageDto);

        foreach (var participant in conversation.Participants)
        {
            await hubContext.Clients
                .Group($"user_{participant.UserId}")
                .SendAsync("ChatListUpdated", new
                {
                    ConversationId = conversationId,
                    LastMessageId = systemMessage.Id,
                    LastMessageText = conversation.LastMessageText,
                    LastMessageAt = conversation.LastMessageAt
                });
        }

        return Success("پیام سیستمی با موفقیت ثبت شد.", messageDto);
    }


    private async Task<List<Conversation>> GetCoachAthleteConversations(int coachUserId)
    {

        return await context.Conversations
            .AsNoTracking()
            .Where(x => x.Type == ConversationType.CoachAthlete && 
                        x.Participants.Any(p => p.UserId == coachUserId) )
            .Include(x => x.Participants)
                .ThenInclude(x => x.User)
            .ToListAsync();
    }

    private async Task<Dictionary<long, int>> GetUnreadCountsBatch(List<long> conversationIds, int currentUserId)
    {
        if (conversationIds.Count == 0) return new Dictionary<long, int>();

        var unreadCounts = await context.ConversationParticipants
            .AsNoTracking()
            .Where(p => conversationIds.Contains(p.ConversationId) && p.UserId == currentUserId)
            .Select(p => new
            {
                p.ConversationId,
                p.UnreadCount
            })
            .ToDictionaryAsync(x => x.ConversationId, x => x.UnreadCount);

        return unreadCounts;
    }

 

    private static ChatListItemDto BuildChatListItem(
        Conversation conversation,
        User otherUser,
        WorkoutProgram? program,
        int unreadCount,
        int otherUserUnreadCount)
    {
        return new ChatListItemDto
        {
            ConversationId = conversation.Id,
            UserId = otherUser.Id,
            FullName = $"{otherUser.FirstName} {otherUser.LastName}",
            PhoneNumber = otherUser.PhoneNumber,
            ProfileImageUrl = otherUser.ImageProfile,
            Service = program?.Title,
            LastExerciseDate = program?.LastExerciseDate,
            Status = program?.GetStatus() ?? "active",
            LastMessageId = conversation.LastMessageId,
            LastMessageText = conversation.LastMessageText,
            LastMessageAt = conversation.LastMessageAt,
            UnreadCount = unreadCount,
            IsSupport = false,
            OtherUserRead = otherUserUnreadCount==0
            
        };
    }

    private async Task<Conversation?> FindCoachAthleteConversation(int coachUserId, int athleteUserId)
    {
        return await context.Conversations
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x =>
                x.Type == ConversationType.CoachAthlete &&
                x.Participants.Count == 2 &&
                x.Participants.Any(p => p.UserId == coachUserId) &&
                x.Participants.Any(p => p.UserId == athleteUserId));
    }

    private async Task<Conversation?> FindSupportConversation(int userId, int supportUserId)
    {
        return await context.Conversations
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x =>
                x.Type == ConversationType.UserSupport &&
                x.Participants.Count == 2 &&
                x.Participants.Any(p => p.UserId == userId) &&
                x.Participants.Any(p => p.UserId == supportUserId));
    }
    
    private async Task<List<Conversation>> GetAthleteConversations(int athleteUserId)
    {
        return await context.Conversations
            .AsNoTracking()
            .Where(x =>
                x.Type == ConversationType.CoachAthlete &&
                x.Participants.Count == 2 &&
                x.Participants.Any(p => p.UserId == athleteUserId) )
            .Include(x => x.Participants)
                .ThenInclude(x => x.User)
            .ToListAsync();
    }

    private async Task<ChatListItemDto?> GetSupportChatItem(int userId)
    {
        var supportUserId = GetSupportUserId();

        var conversation = await context.Conversations
            .AsNoTracking()
            .Include(x => x.Participants)
                .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.Type == ConversationType.UserSupport &&
                x.Participants.Count == 2 &&
                x.Participants.Any(p => p.UserId == userId) &&
                x.Participants.Any(p => p.UserId == supportUserId));

        if (conversation is null)
        {
            return null;
        }

        var supportParticipant = conversation.Participants
            .FirstOrDefault(x => x.UserId == supportUserId);
        var userParticipant = conversation.Participants.FirstOrDefault(x => x.UserId == userId);
        if (supportParticipant?.User is null || userParticipant is null) return null;


        if (supportParticipant?.User is null)
        {
            return null;
        }

        return conversation.ToSupportListItem(
            supportParticipant.User, 
            userParticipant.UnreadCount); 
    }


    private static string BuildConversationPreview(ChatMessage message)
    {
        return message.Type switch
        {
            ChatMessageType.Text => message.Text ?? string.Empty,
            ChatMessageType.Image => "تصویر",
            ChatMessageType.File => "فایل",
            ChatMessageType.System => message.Text ?? "پیام سیستمی",
            _ => "پیام جدید"
        };
    }

    private static string? NormalizeMessageText(string? text)
    {
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static string GetConversationService(ConversationType conversationType)
    {
        return conversationType switch
        {
            ConversationType.CoachAthlete => "برنامه تمرینی",
            ConversationType.UserSupport => "پشتیبانی",
            _ => "گفتگو"
        };
    }

    private static string GetFullName(User user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.PhoneNumber : fullName;
    }

    private static int NormalizeMessageTake(int take)
    {
        if (take <= 0) return DefaultMessageTake;
        if (take < MinimumMessageTake) return MinimumMessageTake;
        if (take > MaximumMessageTake) return MaximumMessageTake;
        return take;
    }

    private int GetSupportUserId()
    {
        return configuration.GetValue<int>("Chat:SupportUserId", 1);
    }

    private async Task NotifyConversationCreated(
        long conversationId,
        ConversationType conversationType,
        params int[] userIds)
    {
        foreach (var userId in userIds.Distinct())
        {
            await hubContext.Clients
                .Group($"user_{userId}")
                .SendAsync("ConversationCreated", new
                {
                    ConversationId = conversationId,
                    ConversationType = conversationType.ToString()
                });
        }
    }

    private static void SortCoachResult(CoachChatListDto result)
    {
        result.Active = SortChatItems(result.Active);
        result.NeedsFollowUp = SortChatItems(result.NeedsFollowUp);
        result.NearingCompletion = SortChatItems(result.NearingCompletion);
        result.Inactive = SortChatItems(result.Inactive);
    }

    private static List<ChatListItemDto> SortChatItems(IEnumerable<ChatListItemDto> items)
    {
        return items
            .OrderByDescending(x => x.LastMessageAt)
            .ThenBy(x => x.FullName)
            .ToList();
    }

    private async Task<(bool Action, string Message, long ConversationId)> ResolveConversationIdOrCreateSupport(
        int userId,
        long? conversationId)
    {
        if (userId <= 0)
        {
            return (false, "شناسه کاربر نامعتبر است.", 0);
        }

        if (conversationId.HasValue)
        {
            if (conversationId.Value <= 0)
            {
                return (false, "شناسه گفتگو نامعتبر است.", 0);
            }

            return (true, string.Empty, conversationId.Value);
        }

        var createSupportResult = await CreateSupportConversation(userId);

        if (!createSupportResult.Action)
        {
            return (false, createSupportResult.Message, 0);
        }

        var supportConversationId = ExtractLongId(createSupportResult.Result);

        if (supportConversationId <= 0)
        {
            return (false, "شناسه گفتگوی پشتیبانی نامعتبر است.", 0);
        }

        return (true, string.Empty, supportConversationId);
    }

    private static long ExtractLongId(object? value)
    {
        if (value is null) return 0;

        return value switch
        {
            long id => id,
            int id => id,
            short id => id,
            byte id => id,
            string str when long.TryParse(str, out var id) => id,
            _ when long.TryParse(value.ToString(), out var id) => id,
            _ => 0
        };
    }
    private static readonly string[] AllowedAttachmentContentTypes =
    [
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/gif",
        "application/pdf"
    ];


    private static ApiResponse Success(string message, object? result = null)
    {
        return new ApiResponse
        {
            Action = true,
            Message = message,
            Result = result
        };
    }

    private static ApiResponse Failure(string message)
    {
        return new ApiResponse
        {
            Action = false,
            Message = message
        };
    }
}
