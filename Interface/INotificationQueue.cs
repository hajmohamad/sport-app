using sport_app_backend.Dtos.Notification;

namespace sport_app_backend.Interface;

public interface INotificationQueue
{
    ValueTask EnqueueAsync(NotificationRequest request);
    ValueTask<NotificationRequest> DequeueAsync(CancellationToken cancellationToken);

}