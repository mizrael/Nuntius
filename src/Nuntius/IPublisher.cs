namespace Nuntius;

public interface IPublisher
{
    ValueTask Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default) where TNotification : INotification;
}
