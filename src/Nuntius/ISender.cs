namespace Nuntius;

/// <summary>
/// Send a request to be handled by a single handler.
/// </summary>
/// <remarks>
/// Use this interface for point to point communication.
/// </remarks>
public interface ISender
{
    /// <summary>
    /// Asynchronously send a request to a single handler with no response.
    /// </summary>
    /// <param name="request">
    /// The request to be sent.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional cancellation token.
    /// </param>
    /// <typeparam name="TRequest">
    /// The type of request being sent.
    /// </typeparam>
    /// <returns>
    /// A value task that represents the asynchronous operation.
    /// </returns>
    ValueTask Send<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default) where TRequest : IRequest;

    /// <summary>
    /// Asynchronously send a request to a single handler. The handler returns a response.
    /// </summary>
    /// <param name="request">
    /// The request to be sent.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional cancellation token.
    /// </param>
    /// <typeparam name="TResponse">
    /// The type of response returned by the handler.
    /// </typeparam>
    /// <returns>
    /// A value task that represents the asynchronous operation. The value task result contains the handler response.
    /// </returns>
    ValueTask<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
