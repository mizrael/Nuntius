namespace Nuntius;

/// <summary>
/// Defines a handler for a request with a void response.
/// </summary>
/// <typeparam name="TRequest">
/// The type of request being handled.
/// </typeparam>
/// <remarks>
/// Use this interface for point to point communication.
/// </remarks>
public interface IRequestHandler<in TRequest>
    where TRequest : IRequest
{
    /// <summary>
    /// Handles a request with a void response.
    /// </summary>
    /// <param name="request">
    /// The request to handle.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional cancellation token.
    /// </param>
    /// <returns>
    /// A value task that represents the asynchronous operation.
    /// </returns>
    ValueTask Handle(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a handler for a request with a response.
/// </summary>
/// <typeparam name="TRequest">
/// The type of request being handled.
/// </typeparam>
/// <typeparam name="TResponse">
/// The type of response returned from the handler.
/// </typeparam>
/// <remarks>
/// Use this interface for point to point communication.
/// </remarks>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles a request with a response.
    /// </summary>
    /// <param name="request">
    /// The request to handle.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional cancellation token.
    /// </param>
    /// <returns>
    /// A value task that represents the asynchronous operation. The value task result contains the handler response.
    /// </returns>
    ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
}
