using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Nuntius;

internal class Mediator : IMediator
{
    private readonly IServiceProvider _sp;
    private readonly ConcurrentDictionary<Type, Type> _handlersWithResponseTypesCache = new();

    public Mediator(IServiceProvider sp)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
    }

    public async ValueTask Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        using var scope = _sp.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<TRequest>>();
        await handler.Handle(request, cancellationToken);
    }

    public async ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();

        var handlerType = _handlersWithResponseTypesCache.GetOrAdd(requestType, rt =>
        {
            return typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        });

        dynamic handler = _sp.GetRequiredService(handlerType);
        return await handler.Handle((dynamic)request, cancellationToken);
    }
}
