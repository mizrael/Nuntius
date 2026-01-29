namespace Nuntius;

/// <summary>
/// A request with a void response.
/// </summary>
/// <remarks>
/// Use this interface for point to point communication.
/// </remarks>
public interface IRequest { }

/// <summary>
/// A request with a response.
/// </summary>
/// <typeparam name="TResponse">
/// The response type.
/// </typeparam>
/// <remarks>
/// Use this interface for point to point communication.
/// </remarks>
public interface IRequest<out TResponse> { }

