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



public async Task<ApiResponse> GetConversationMessages(
    int userId,
    long conversationId,
    long? beforeMessageId,
    int take)
{
    if (userId <= 0 || conversationId <= 0)
        return Failure("اطلاعات گفتگو نامعتبر است.");

    var conversation = await context.Conversations
        .AsNoTracking()
        .Select(c => new { c.Id, c.Type })
        .FirstOrDefaultAsync(c => c.Id == conversationId);

    if (conversation == null) return Failure("گفتگو یافت نشد.");

    bool isChannel = conversation.Type == ConversationType.Channel;
    
    object? otherUserNameAndPhoto = new
    {
        FullName = $"اطلاع رسانی چارست",
        Photo = "",
        phoneNumber = "09395327229",
        athleteStatus = "Channel"
    };
    long? otherLastReadId = null;

    if (!isChannel)
    {
        var participants = await context.ConversationParticipants
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .Select(p => new
            {
                p.UserId,
                p.Role,
                p.LastReadMessageId,
                User = new { p.User.FirstName, p.User.LastName, p.User.ImageProfile, p.User.PhoneNumber }
            })
            .ToListAsync();

        var userParticipant = participants.FirstOrDefault(x => x.UserId == userId);
        if (userParticipant is null) return Failure("شما عضو این گفتگو نیستید.");

        var otherParticipant = participants.FirstOrDefault(x => x.UserId != userId);
        if (otherParticipant is null) return Failure("کاربر مقابل پیدا نشد.");

        otherLastReadId = otherParticipant.LastReadMessageId;

        var athleteStatus = "";
        if (otherParticipant.Role == ConversationParticipantRole.Athlete)
        {
            var programs = await workoutCache.GetCoachWorkoutProgramAthleteUserIdByCoachUserId(userId);
            if (programs.TryGetValue(otherParticipant.UserId, out var program))
            {
                athleteStatus = program.GetStatus();
            }
        }

        otherUserNameAndPhoto = new
        {
            FullName = $"{otherParticipant.User.FirstName} {otherParticipant.User.LastName}".Trim(),
            Photo = otherParticipant.User.ImageProfile,
            phoneNumber = otherParticipant.User.PhoneNumber,
            athleteStatus
        };
    }
  
    await context.ConversationParticipants
        .Where(x => x.ConversationId == conversationId && x.UserId == userId)
        .ExecuteUpdateAsync(update => update.SetProperty(x => x.UnreadCount, 0));

    take = ChatMapper.NormalizeMessageTake(take);
    var messagesQuery = context.ChatMessages
        .AsNoTracking()
        .Where(x => x.ConversationId == conversationId);

    if (beforeMessageId.HasValue && beforeMessageId.Value > 0)
    {
        messagesQuery = messagesQuery.Where(x => x.Id < beforeMessageId.Value);
    }

    var messages = await messagesQuery
        .OrderByDescending(x => x.Id)
        .Take(take)
        .ToListAsync();

    var messageList = messages
        .OrderBy(x => x.Id)
        .Select(x => x.ChatMessageDto(userId, isChannel ? null : otherLastReadId))
        .ToList();

    return Success("پیام‌ها با موفقیت دریافت شدند.", new
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

    var (action, m, conversationId) = await ResolveConversationIdOrCreateSupport(
        senderUserId,
        dto.ConversationId);

    if (!action)
    {
        return Failure(m);
    }

    var conversation = await context.Conversations
        .Include(x => x.Participants)
            .ThenInclude(p => p.User) 
        .FirstOrDefaultAsync(x => x.Id == conversationId);

    if (conversation is null)
    {
        return Failure("گفتگو یافت نشد.");
    }

    if (conversation.IsClosed||conversation.Type==ConversationType.Channel)
    {
        return Failure("این گفتگو بسته شده است.");
    }
    
    var senderParticipant = conversation.Participants.FirstOrDefault(p => p.UserId == senderUserId);
    
    if (senderParticipant is null)
    {
        return Failure("شما عضو این گفتگو نیستید یا اطلاعات کاربری شما یافت نشد.");
    }

    var now = DateTime.Now;

    var message = new ChatMessage
    {
        ConversationId = conversation.Id,
        SenderUserId = senderUserId,
        Type = ChatMessageType.Text,
        Text = normalizedText,
        SentAt = now,
        SenderUser = senderParticipant.User 
    };

    await context.ChatMessages.AddAsync(message);

    conversation.LastMessageAt = now;
    conversation.LastMessageText = ChatMapper.BuildConversationPreview(message);
    
    var otherParticipant = conversation.Participants.FirstOrDefault(x => x.UserId != senderUserId);
    if (otherParticipant is null)
    {
        return Failure("کاربر مقابل پیدا نشد");
    }
    otherParticipant.UnreadCount++;
    conversation.LastMessageSenderId = message.SenderUserId;
    
    await context.SaveChangesAsync();


    ChatMessageDto? senderMessageDto = null;

    foreach (var participant in conversation.Participants)
    {
        var other = conversation.Participants
            .FirstOrDefault(x => x.UserId != participant.UserId);

        var otherLastReadId = other?.LastReadMessageId;
        var otherUnread = other?.UnreadCount ?? 0;

        var messageDto = message.ChatMessageDto(
            participant.UserId,
            otherLastReadId);

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
                ConversationId = conversation.Id,
                LastMessageId = message.Id,
                LastMessageStatus = conversation.LastMessageStatus(
                    otherUnread,
                    other?.UserId ?? 0),
                LastMessageText = conversation.LastMessageText,
                LastMessageAt = conversation.LastMessageAt,
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
        participant.LastReadAt = DateTime.Now;
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

    await hubContext.Clients
        .Group($"user_{userId}")
        .SendAsync("ChatListUpdated", new
        {
            ConversationId = conversationId,
            UnreadCount = 0 
        });

    return Success("پیام‌ها به‌عنوان خوانده‌شده ثبت شدند.");
}

    public async Task<ApiResponse> UploadAttachment(
    int senderUserId,
    long? conversationId,
    IFormFile file)
{
    if (ChatMapper.CheckUploadAttachment(
            senderUserId,
            file,
            out var isPdf,
            out var isImage,
            out var isVideo,
            out var apiResponse))
        return apiResponse!;
    
    var resolvedConversation = await ResolveConversationIdOrCreateSupport(
        senderUserId,
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
    
    var senderParticipant = conversation.Participants.FirstOrDefault(p => p.UserId == senderUserId);
    
    if (senderParticipant is null)
    {
        return Failure("شما عضو این گفتگو نیستید.");
    }
    var otherParticipant = conversation.Participants.FirstOrDefault(x => x.UserId != senderUserId);
    if (otherParticipant is null)
    {
        return Failure("کاربر مقابل پیدا نشد");
    }
    
    var folderPath = $"chat/{resolvedConversationId}";
    var uploadResult = (isVideo || isPdf)
        ? await storage.UploadFile(file, string.Empty, folderPath)
        : await storage.UploadImage(file, string.Empty, folderPath);



    if (!uploadResult.Action || uploadResult.Result is null)
    {
        return uploadResult;
    }

    var now = DateTime.Now;

    var message = new ChatMessage
    {
        ConversationId = conversation.Id,
        SenderUserId = senderUserId,
        Type = isVideo
            ? ChatMessageType.Video
            : (isPdf ? ChatMessageType.File : ChatMessageType.Image),
        SentAt = now,
        FileUrl = uploadResult.Result.ToString(),
        SenderUser = senderParticipant.User 
    };

    context.ChatMessages.Add(message);

    conversation.LastMessageAt = now;
    conversation.LastMessageText = ChatMapper.BuildConversationPreview(message);
    
    
    otherParticipant.UnreadCount++;
    conversation.LastMessageSenderId = message.SenderUserId;
    await context.SaveChangesAsync(); 


    ChatMessageDto? senderMessageDto = null;
    
    foreach (var participant in conversation.Participants)
    {
        var other = conversation.Participants
            .FirstOrDefault(x => x.UserId != participant.UserId);

        var otherLastReadId = other?.LastReadMessageId;
        var otherUnread = other?.UnreadCount ?? 0;

        var messageDto = message.ChatMessageDto(
            participant.UserId,
            otherLastReadId);

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
                ConversationId = conversation.Id,
                LastMessageId = message.Id,
                LastMessageStatus = conversation.LastMessageStatus(
                    otherUnread,
                    other?.UserId ?? 0),
                 conversation.LastMessageText,
                 conversation.LastMessageAt,
                 participant.UnreadCount
            });
    }
    
    return Success("پیام با موفقیت ارسال شد.", senderMessageDto); }


 
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

        var now = DateTime.Now;

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

        var now = DateTime.Now;

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

public async Task<ApiResponse> GetCoachChatList(int coachId, int coachUserId, string? status = null)
{
    if (coachId <= 0)
    {
        return Failure("شناسه مربی نامعتبر است.");
    }

    var conversations = await GetCoachAthleteConversations(coachUserId);
    var coachPrograms = await workoutCache.GetCoachWorkoutProgramAthleteUserIdByCoachUserId(coachUserId);

    var result = new CoachChatListDto
    {
        Support = await GetSupportChatItem(coachUserId),
        SupportChannel = await GetSupportChannelChatItem(coachUserId)
        
    };

    foreach (var conversation in conversations)
    {
        var coachParticipant = conversation.Participants.FirstOrDefault(u => u.UserId == coachUserId);
        var athleteParticipant = conversation.Participants.FirstOrDefault(x => x.UserId != coachUserId);
        
        if (athleteParticipant == null || !coachPrograms.TryGetValue(athleteParticipant.UserId, out var program))
        {
            continue; 
        }

        var item = conversation.BuildChatListItem( athleteParticipant.User, program, coachParticipant!.UnreadCount, athleteParticipant.UnreadCount);

        result.All.Add(item);

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
    
    

    if (string.IsNullOrWhiteSpace(status))
    {
        return Success("لیست چت‌های مربی با موفقیت دریافت شد.", result);
    }
    else
    {
        object responseData;
        switch (status.ToLowerInvariant())
        {
            case "all":
                responseData = result.All;
                break;
            case "active":
                responseData = result.Active;
                break;
            case "needsfollowup":
                responseData = result.NeedsFollowUp;
                break;
            case "nearingcompletion":
                responseData = result.NearingCompletion;
                break;
            case "inactive":
                responseData = result.Inactive;
                break;
            default:
                return Failure("استاتوس ارسالی نامعتبر است.");
        }
        return Success("لیست فیلتر شده چت‌ها با موفقیت دریافت شد.", responseData);
    }
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
            Support = await GetSupportChatItem(athleteUserId),
            SupportChannel = await GetSupportChannelChatItem(athleteUserId)
        };

        foreach (var conversation in conversations)
        {
            var coach = conversation.Participants.FirstOrDefault(x => x.UserId != athleteUserId);
            if (coach == null) continue;
            
            unreadCountsDict.TryGetValue(conversation.Id, out int unreadCount);

            var item = conversation.BuildChatListItem(
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
    public async Task<ApiResponse> AddNewUserToChannel(int userId, bool isCoach)
    {
        if (userId <= 0)
        {
            return Failure("شناسه کاربر نامعتبر است.");
        }

        var supportChannelId = GetSupportChannelId();
        if (supportChannelId <= 0)
        {
            return Failure("شناسه کانال اطلاع‌رسانی در تنظیمات سیستم یافت نشد.");
        }

        var channel = await context.Conversations
            .FirstOrDefaultAsync(x => x.Id == supportChannelId && x.Type == ConversationType.Channel);

        if (channel == null)
        {
            return Failure("کانال اطلاع‌رسانی یافت نشد.");
        }

        // var existingParticipant = await context.ConversationParticipants
        //     .AnyAsync(x => x.ConversationId == supportChannelId && x.UserId == userId);
        //
        // if (existingParticipant)
        // {
        //     return Success("کاربر در حال حاضر عضو کانال می‌باشد.");
        // }

        var now = DateTime.Now;
        var newParticipant = new ConversationParticipant
        {
            ConversationId = supportChannelId,
            UserId = userId,
            // نقش کاربر در کانال معمولاً User یا Participant ساده است
            Role = isCoach ? ConversationParticipantRole.Coach : ConversationParticipantRole.Athlete,
            JoinedAt = now,
            UnreadCount = 0 // یا می‌توانید تعداد پیام‌های کل کانال را بشمارید و اینجا قرار دهید
        };

        await context.ConversationParticipants.AddAsync(newParticipant);
    
        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return Failure("خطا در عضویت در کانال: " + ex.Message);
        }

        

        return Success("کاربر با موفقیت به کانال اضافه شد.");
    }


    public async Task<ApiResponse> AddSystemMessage(int coachUserId, int athleteUserId, string text)
{
    if (string.IsNullOrWhiteSpace(text))
    {
        return Failure("متن پیام سیستمی نمی‌تواند خالی باشد.");
    }

    var conversation = await context.Conversations
        .Include(x => x.Participants)
        .FirstOrDefaultAsync(x =>
            x.Type == ConversationType.CoachAthlete &&
            x.Participants.Any(p => p.UserId == coachUserId) &&
            x.Participants.Any(p => p.UserId == athleteUserId));

    if (conversation is null)
    {
        return Failure("گفتگوی بین مربی و ورزشکار یافت نشد.");
    }

    if (conversation.IsClosed)
    {
        return Failure("این گفتگو بسته شده است.");
    }

    var senderParticipant = conversation.Participants
        .FirstOrDefault(p => p.UserId == coachUserId);

    if (senderParticipant is null)
    {
        return Failure("عضو ارسال‌کننده در گفتگو یافت نشد.");
    }

    var now = DateTime.Now;

    var systemMessage = new ChatMessage
    {
        ConversationId = conversation.Id,
        SenderUserId = null,
        Type = ChatMessageType.System,
        Text = text.Trim(),
        SentAt = now,
    };

    context.ChatMessages.Add(systemMessage);

    conversation.LastMessageAt = now;
    conversation.LastMessageText = ChatMapper.BuildConversationPreview(systemMessage);
    conversation.LastMessageSenderId = 0;

    await context.SaveChangesAsync();

    var messageWithSender = await context.ChatMessages
        .AsNoTracking()
        .Include(x => x.SenderUser)
        .FirstAsync(x => x.Id == systemMessage.Id);

    var messageDto = messageWithSender.ChatMessageDto(senderParticipant.UserId, null);

    await hubContext.Clients
        .Group($"chat_{conversation.Id}")
        .SendAsync("ReceiveMessage", messageDto);

    foreach (var participant in conversation.Participants)
    {
        await hubContext.Clients
            .Group($"user_{participant.UserId}")
            .SendAsync("ChatListUpdated", new
            {
                ConversationId = conversation.Id,
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

        return conversation.ToSupportListItem(
            supportParticipant.User, 
            userParticipant.UnreadCount); 
    }
    private async Task<ChatListItemDto?> GetSupportChannelChatItem(int userId)
    {
        var supportChannelId = GetSupportChannelId();
        var conversation = await context.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Type == ConversationType.Channel && x.Id == supportChannelId);
        
        if (conversation is null) return null;
  
        var unreadCount = await context.ConversationParticipants
            .Where(cp => cp.ConversationId == conversation.Id && cp.UserId == userId)
            .Select(p => p.UnreadCount)
            .FirstOrDefaultAsync();

        return conversation.ToChannelListItem(unreadCount); 
    }




   

    private static string? NormalizeMessageText(string? text)
    {
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }



    private static string GetFullName(User user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.PhoneNumber : fullName;
    }
    

    private int GetSupportUserId()
    {
        return configuration.GetValue<int>("Chat:SupportUserId", 1);
    }
    private int GetSupportChannelId()
    {
        return configuration.GetValue<int>("Chat:SupportChannelId", 2);
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
        result.All= SortChatItems(result.All);
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
