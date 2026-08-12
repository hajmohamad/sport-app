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
        private const long MaximumAttachmentSize = 10 * 1024 * 1024;
        private static readonly string[] AllowedAttachmentContentTypes =
        [
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp",
            "image/gif",
            "application/pdf"
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
                ProfileImageUrl = supportUser.ImageProfile,
                Service = "پشتیبانی",
                Status = "Support",
                LastMessageText = conversation.LastMessageText,
                LastMessageAt = conversation.LastMessageAt,
                UnreadCount = unreadCount,
                IsSupport = true,
                LastMessageStatus = conversation.LastMessageStatus(unreadCount,supportUser.Id)
                
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


        public static bool CheckUploadAttachment(int userId, IFormFile file, out bool isPdf, out bool isImage,
            out ApiResponse? apiResponse)
        {
            if (userId <= 0)
            {
                isPdf = false;
                isImage = false;
                apiResponse = Failure("شناسه کاربر نامعتبر است.");
                return true;
            }

            if (file is null || file.Length <= 0)
            {
                isPdf = false;
                isImage = false;
                apiResponse = Failure("فایل انتخاب نشده است.");
                return true;
            }

            if (file.Length > MaximumAttachmentSize)
            {
                isPdf = false;
                isImage = false;
                apiResponse = Failure("حجم فایل نباید بیشتر از ۱۰ مگابایت باشد.");
                return true;
            }

            var contentType = file.ContentType?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(contentType) ||
                !AllowedAttachmentContentTypes.Contains(
                    contentType,
                    StringComparer.OrdinalIgnoreCase))
            {
                isPdf = false;
                isImage = false;
                apiResponse = Failure("فقط تصویر و فایل PDF مجاز هستند.");
                return true;
            }

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

            isPdf = string.Equals(
                contentType,
                "application/pdf",
                StringComparison.OrdinalIgnoreCase);

            isImage = contentType.StartsWith("image/");

            if (isPdf && extension != ".pdf")
            {
                apiResponse = Failure("پسوند فایل PDF نامعتبر است.");
                return true;
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
                    apiResponse = Failure("پسوند تصویر نامعتبر است.");
                    return true;
                }
            }

            apiResponse = null;
            return false;
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

        public static ApiResponse Failure(string message)
        {
            return new ApiResponse
            {
                Action = false,
                Message = message
            };
        }

        public static int NormalizeMessageTake(int take)
        {
            if (take <= 0) return DefaultMessageTake;
            if (take < MinimumMessageTake) return MinimumMessageTake;
            if (take > MaximumMessageTake) return MaximumMessageTake;
            return take;
        }

        private static string GetFullName(User user)
        {
            var fullName = $"{user.FirstName} {user.LastName}".Trim();

            return string.IsNullOrWhiteSpace(fullName)
                ? user.PhoneNumber
                : fullName;
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
                ChatMessageType.System => message.Text ?? "پیام سیستمی",
                _ => "پیام جدید"
            };
        }
        
    }
