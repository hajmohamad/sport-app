using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;

namespace sport_app_backend.Hubs;

[Authorize]
public class ChatHub(ApplicationDbContext context) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var phoneNumber = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            Context.Abort();
            return;
        }

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);

        if (user is null)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{user.Id}");

        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(long conversationId)
    {
        var phoneNumber = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            Context.Abort();
            return;
        }

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);

        if (user is null)
        {
            Context.Abort();
            return;
        }

        var isParticipant = await context.ConversationParticipants
            .AsNoTracking()
            .AnyAsync(x => x.ConversationId == conversationId && x.UserId == user.Id);

        if (!isParticipant)
        {
            throw new HubException("شما عضو این گفتگو نیستید.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{conversationId}");
    }

    public async Task LeaveConversation(long conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{conversationId}");
    }

    public async Task TypingStarted(long conversationId)
    {
        var phoneNumber = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return;

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);

        if (user is null)
            return;

        var isParticipant = await context.ConversationParticipants
            .AsNoTracking()
            .AnyAsync(x => x.ConversationId == conversationId && x.UserId == user.Id);

        if (!isParticipant)
            return;

        await Clients.OthersInGroup($"chat_{conversationId}")
            .SendAsync("UserTypingStarted", new
            {
                ConversationId = conversationId,
                UserId = user.Id,
                PhoneNumber = user.PhoneNumber,
                FullName = $"{user.FirstName} {user.LastName}".Trim()
            });
    }

    public async Task TypingStopped(long conversationId)
    {
        var phoneNumber = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return;

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);

        if (user is null)
            return;

        var isParticipant = await context.ConversationParticipants
            .AsNoTracking()
            .AnyAsync(x => x.ConversationId == conversationId && x.UserId == user.Id);

        if (!isParticipant)
            return;

        await Clients.OthersInGroup($"chat_{conversationId}")
            .SendAsync("UserTypingStopped", new
            {
                ConversationId = conversationId,
                UserId = user.Id
            });
    }
}
