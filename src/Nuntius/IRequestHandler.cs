namespace Nuntius;

public interface IRequestHandler<in TRequest>
    where TRequest : IRequest
{
    ValueTask Handle(TRequest request, CancellationToken cancellationToken = default);
}

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
}
