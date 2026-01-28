namespace Nuntius;

public interface INotificationHandler<TNotification>
    where TNotification : INotification
{
    ValueTask Handle(TNotification request, CancellationToken cancellationToken = default);
}
