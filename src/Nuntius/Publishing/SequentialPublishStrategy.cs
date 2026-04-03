namespace Nuntius;

/// <summary>
/// Executes notification handlers sequentially in order.
/// If any handler throws, execution stops and the exception propagates immediately.
/// </summary>
/// <remarks>
/// This is the default publishing strategy and matches the original Nuntius behavior.
/// </remarks>
public sealed class SequentialPublishStrategy : IPublishStrategy
{
    /// <summary>
    /// Singleton instance. This strategy is stateless and thread-safe.
    /// </summary>
    public static readonly SequentialPublishStrategy Instance = new();

    /// <inheritdoc />
    public async ValueTask ExecuteAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken) where TNotification : INotification
    {
        foreach (var handler in handlers)
        {
            await handler.Handle(notification, cancellationToken);
        }
    }
}
