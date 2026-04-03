namespace Nuntius;

/// <summary>
/// Defines a strategy for executing notification handlers during publishing.
/// </summary>
public interface IPublishStrategy
{
    /// <summary>
    /// Executes the given notification handlers according to the strategy's semantics.
    /// </summary>
    /// <param name="handlers">The resolved notification handlers to execute.</param>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <typeparam name="TNotification">The type of notification being published.</typeparam>
    /// <returns>A value task representing the asynchronous operation.</returns>
    ValueTask ExecuteAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken) where TNotification : INotification;
}
