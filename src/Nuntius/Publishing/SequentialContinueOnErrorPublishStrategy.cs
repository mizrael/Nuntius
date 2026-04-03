namespace Nuntius;

/// <summary>
/// Executes notification handlers sequentially, continuing execution when a handler throws.
/// All exceptions are collected and thrown as an <see cref="AggregateException"/> after all handlers complete.
/// </summary>
/// <remarks>
/// Useful for fire-and-forget scenarios (telemetry, logging, cache invalidation)
/// where one handler's failure should not prevent others from running.
/// </remarks>
public sealed class SequentialContinueOnErrorPublishStrategy : IPublishStrategy
{
    /// <summary>
    /// Singleton instance. This strategy is stateless and thread-safe.
    /// </summary>
    public static readonly SequentialContinueOnErrorPublishStrategy Instance = new();

    /// <inheritdoc />
    public async ValueTask ExecuteAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken) where TNotification : INotification
    {
        List<Exception>? exceptions = null;

        foreach (var handler in handlers)
        {
            try
            {
                await handler.Handle(notification, cancellationToken);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException(exceptions);
        }
    }
}
