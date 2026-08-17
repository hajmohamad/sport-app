    using sport_app_backend.Dtos.Chat;
    using sport_app_backend.Models;
    using sport_app_backend.Models.Account;
    using sport_app_backend.Models.Chat;
    using sport_app_backend.Models.Program;

    namespace sport_app_backend.Mappers;

    public static class ChatMapper
    {
        private const int DefaultMessageTake = 50;
        private const int MinimumMessageTake = 20;
        private const int MaximumMessageTake = 100;
        private const long MaximumAttachmentSize = 10 * 1024 * 1024;   // تصویر/PDF
        private const long MaximumVideoSize = 20 * 1024 * 1024;   
        private static readonly string[] AllowedAttachmentContentTypes =
        [
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp",
            "image/gif",
            "image/heic",
            "image/heif",
            "application/pdf",
            "video/mp4",
            "video/webm",
            "video/quicktime",     // .mov
            "video/x-msvideo",     // .avi
            "video/x-matroska",    // .mkv
            "video/mpeg",
            "video/ogg",
            "video/3gpp",
            "video/mp2t"
        ];
        public static ChatMessageDto ChatMessageDto(
            this ChatMessage message,
            int currentUserId,
            long? lastReadMessageId){

            return new ChatMessageDto
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                UserId = message.SenderUserId,
                Type = message.Type.ToString(),
                Text = message.Text,
                SentAt = message.SentAt,
                IsMine = message.SenderUserId == currentUserId,
                FileUrl = message.FileUrl,
                IsRead = message.SenderUserId == currentUserId &&
                         lastReadMessageId.HasValue &&
                         lastReadMessageId.Value >= message.Id
            };
        }

     
        public static ChatListItemDto ToSupportListItem(
            this Conversation conversation,
            User supportUser,
            int unreadCount)
        {
            return new ChatListItemDto
            {
                ConversationId = conversation.Id,
                UserId = supportUser.Id,
                FullName = "پشتیبانی چارست",
                PhoneNumber = supportUser.PhoneNumber,
                ProfileImageUrl = "https://chaarset.s3.ir-thr-at1.arvanstorage.ir/support.jpg",
                Service = "پشتیبانی",
                Status = "Support",
                LastMessageText = conversation.LastMessageText,
                LastMessageAt = conversation.LastMessageAt,
                UnreadCount = unreadCount,
                IsSupport = true,
                LastMessageStatus = conversation.LastMessageStatus(unreadCount,supportUser.Id)
                
            };
        }
        public static ChatListItemDto ToChannelListItem(
            this Conversation conversation,
           int unreadCount)
        {
            return new ChatListItemDto
            {
                ConversationId = conversation.Id,
                UserId = 0,
                FullName = "کانال اطلاع رسانی چارست",
                PhoneNumber = "09395327229",
                ProfileImageUrl = "https://chaarset.s3.ir-thr-at1.arvanstorage.ir/channel.jpg",
                Service = "پشتیبانی",
                Status = "Channel",
                LastMessageText = conversation.LastMessageText,
                LastMessageAt = conversation.LastMessageAt,
                UnreadCount = unreadCount,
                IsSupport = true,
                LastMessageStatus = "noMessage"
                
            };
        }

        public static string GetStatus(this WorkoutProgram program)
        {
            if (program.Status != WorkoutProgramStatus.ACTIVE)
            {
                return "Inactive";
            }

            if (program.LastExerciseDate is null ||
                program.LastExerciseDate.Value.Date < DateTime.Now.Date.AddDays(-4))
            {
                return "NeedsFollowUp";
            }

            if (program.TotalSessionCount > 0 &&
                program.TotalSessionCount - program.CompletedSessionCount < 5)
            {
                return "NearingCompletion";
            }

            return "Active";
        }

        public static ChatListItemDto BuildChatListItem(
            this Conversation conversation,
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
                LastMessageText = conversation.LastMessageText,
                LastMessageAt = conversation.LastMessageAt,
                UnreadCount = unreadCount,
                IsSupport = false,
                LastMessageStatus = conversation.LastMessageStatus(otherUserUnreadCount,otherUser.Id)
                
            };
        }

        public static string LastMessageStatus(this Conversation conversation,int otherUserUnreadCount,int otherUserId)
        {
            if (conversation.LastMessageText is null || conversation.LastMessageSenderId == 0)
                return "noMessage";
            if (conversation.LastMessageText is null||otherUserId==conversation.LastMessageSenderId)
            {
                return "noMessage";
            }
            return otherUserUnreadCount==0 ? "readMessage" : "unreadMessage";
        }




        public static bool CheckUploadAttachment(
            int userId,
            IFormFile? file,
            out bool isPdf,
            out bool isVideo,
            out ApiResponse? apiResponse)
        {
            isPdf = false;
            isVideo = false;
            apiResponse = null;

            if (userId <= 0)
            {
                apiResponse = Failure("شناسه کاربر نامعتبر است.");
                return true;
            }

            if (file is null || file.Length <= 0)
            {
                apiResponse = Failure("فایل انتخاب نشده است.");
                return true;
            }

            var contentType = file.ContentType.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(contentType) ||
                !AllowedAttachmentContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            {
                apiResponse = Failure("فقط تصویر، فایل PDF و ویدیو مجاز هستند.");
                return true;
            }

            isVideo = contentType.StartsWith("video/");
            var isImage = contentType.StartsWith("image/");
            isPdf = string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // ── بررسی سایز (ویدیو ۲۰ مگ، بقیه ۱۰ مگ) ──
            if (isVideo)
            {
                if (file.Length > MaximumVideoSize)
                {
                    isPdf = false;
                    isVideo = false;
                    apiResponse = Failure("حجم ویدیو نباید بیشتر از ۲۰ مگابایت باشد.");
                    return true;
                }
            }
            else if (file.Length > MaximumAttachmentSize)
            {
                isPdf = false;
                apiResponse = Failure("حجم فایل نباید بیشتر از ۱۰ مگابایت باشد.");
                return true;
            }

            // ── بررسی پسوند ──
            if (string.IsNullOrWhiteSpace(extension))
            {
                isPdf = false;
                isVideo = false;
                apiResponse = Failure("پسوند فایل نامعتبر است.");
                return true;
            }

            if (isPdf && extension != ".pdf")
            {
                apiResponse = Failure("پسوند فایل PDF نامعتبر است.");
                return true;
            }

            if (isImage)
            {
                var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".heic", ".heif" };
                if (!allowedImageExtensions.Contains(extension))
                {
                    apiResponse = Failure("پسوند تصویر نامعتبر است.");
                    return true;
                }
            }

            if (isVideo)
            {
                if (!IsValidVideoExtension(contentType, extension))
                {
                    apiResponse = Failure("پسوند ویدیو نامعتبر است.");
                    return true;
                }
            }

            return false;
        }

private static bool IsValidVideoExtension(string contentType, string extension)
{
    return contentType switch
    {
        "video/mp4" => extension is ".mp4" or ".m4v",
        "video/webm" => extension == ".webm",
        "video/quicktime" => extension is ".mov" or ".qt",
        "video/x-msvideo" => extension == ".avi",
        "video/x-matroska" => extension == ".mkv",
        "video/mpeg" => extension is ".mpeg" or ".mpg",
        "video/ogg" => extension is ".ogv" or ".ogg",
        "video/3gpp" => extension == ".3gp",
        "video/mp2t" => extension == ".ts",

        _ => true // در حالت غیرقابل‌پیش‌بینی، اگر contentType معتبر بود، رد نکن
    };
}

        public static ApiResponse Success(string message, object? result = null)
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

        public static int NormalizeMessageTake(int take)
        {
            return take switch
            {
                <= 0 => DefaultMessageTake,
                < MinimumMessageTake => MinimumMessageTake,
                > MaximumMessageTake => MaximumMessageTake,
                _ => take
            };
        }

        public static string GetConversationService(
            ConversationType conversationType)
        {
            return conversationType switch
            {
                ConversationType.CoachAthlete => "برنامه تمرینی",
                ConversationType.UserSupport => "پشتیبانی",
                _ => "گفتگو"
            };
        }
        public static string BuildConversationPreview(ChatMessage message)
        {
            return message.Type switch
            {
                ChatMessageType.Text => message.Text ?? string.Empty,
                ChatMessageType.Image => "تصویر",
                ChatMessageType.File => "فایل",
                ChatMessageType.Video => "ویدیو",
                ChatMessageType.System => message.Text ?? "پیام سیستمی",
                _ => "پیام جدید"
            };
        }
        
    }
