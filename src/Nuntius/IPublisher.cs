namespace Nuntius;

/// <summary>
/// Publish a notification to be handled by multiple handlers.
/// </summary>
/// <remarks>
/// Use this interface for pub-sub scenarios.
/// </remarks>
public interface IPublisher
{
    /// <summary>
    /// Asynchronously publish a notification to multiple handlers.
    /// </summary>
    /// <param name="notification">
    /// The notification to publish.
    /// </param>
    /// <param name="strategy">
    /// Optional publishing strategy. When <c>null</c>, the default strategy
    /// configured at DI registration time is used.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional cancellation token.
    /// </param>
    /// <typeparam name="TNotification">
    /// The type of notification to publish.
    /// </typeparam>
    /// <returns>
    /// A value task that represents the asynchronous operation.
    /// </returns>
    ValueTask Publish<TNotification>(
        TNotification notification,
        IPublishStrategy? strategy = null,
        CancellationToken cancellationToken = default) where TNotification : INotification;
}
