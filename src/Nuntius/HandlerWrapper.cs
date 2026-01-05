using Microsoft.Extensions.DependencyInjection;

namespace Nuntius;

internal class HandlerWrapper<TRequest, TResponse> : IHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        return await handler.Handle((TRequest)request, cancellationToken);
    }
}