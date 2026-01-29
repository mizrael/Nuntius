namespace Nuntius;

internal interface IRequestHandlerWrapper<TResponse>
{
    ValueTask<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}
