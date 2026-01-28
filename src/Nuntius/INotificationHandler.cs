namespace Nuntius;

public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    ValueTask Handle(TNotification request, CancellationToken cancellationToken = default);
}
