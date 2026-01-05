namespace Nuntius;

internal interface IHandlerWrapper<TResponse>
{
    ValueTask<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}
