namespace Nuntius;

/// <summary>
/// Executes all notification handlers in parallel.
/// All handlers are started concurrently and awaited together.
/// If one or more handlers throw, an <see cref="AggregateException"/> is thrown
/// containing all exceptions.
/// </summary>
public sealed class ParallelPublishStrategy : IPublishStrategy
{
    /// <summary>
    /// Shared instance. This strategy is stateless and thread-safe.
    /// </summary>
    public static readonly ParallelPublishStrategy Instance = new();

    private ParallelPublishStrategy() { }

    /// <inheritdoc />
    public async ValueTask ExecuteAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken) where TNotification : INotification
    {
        var tasks = new List<Task>();

        foreach (var handler in handlers)
        {
            try
            {
                tasks.Add(handler.Handle(notification, cancellationToken).AsTask());
            }
            catch (Exception ex)
            {
                tasks.Add(Task.FromException(ex));
            }
        }

        var whenAll = Task.WhenAll(tasks);

        try
        {
            await whenAll;
        }
        catch
        {
            if (whenAll.Exception is { InnerExceptions: var inners }
                && inners.All(e => e is OperationCanceledException))
            {
                throw inners[0];
            }

            if (whenAll.Exception is not null)
                throw whenAll.Exception;

            throw;
        }
    }
}
