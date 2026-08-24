using System.Threading.Channels;
using sport_app_backend.Dtos.Notification;
using sport_app_backend.Interface;

namespace sport_app_backend.Services.Queue;

public class NotificationQueue : INotificationQueue
{
    private readonly Channel<NotificationRequest> _channel = Channel.CreateUnbounded<NotificationRequest>(new UnboundedChannelOptions
    {
        SingleReader = true 
    });

    public async ValueTask EnqueueAsync(NotificationRequest request)
    {
        await _channel.Writer.WriteAsync(request);
    }

    public async ValueTask<NotificationRequest> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}