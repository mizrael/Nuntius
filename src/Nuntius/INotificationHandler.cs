namespace Nuntius;

/// <summary>
/// Defines a handler for a notification.
/// </summary>
/// <typeparam name="TNotification">
/// The type of notification being handled.
/// </typeparam>
/// <remarks>
/// Use this interface for pub-sub scenarios.
/// </remarks>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Handles a notification.
    /// </summary>
    /// <param name="notification">
    /// The notification to handle.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional cancellation token.
    /// </param>
    /// <returns>
    /// A value task that represents the asynchronous operation.
    /// </returns>
    ValueTask Handle(TNotification notification, CancellationToken cancellationToken = default);
}
