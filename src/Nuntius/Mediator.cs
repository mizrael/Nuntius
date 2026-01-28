using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Nuntius;

internal class Mediator : IMediator
{
    private readonly IServiceProvider _sp;
    private readonly ConcurrentDictionary<Type, object> _requestHandlerWrappersCache = new();

    public Mediator(IServiceProvider sp)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
    }

    public async ValueTask Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        var handler = _sp.GetRequiredService<IRequestHandler<TRequest>>();
        await handler.Handle(request, cancellationToken);
    }

    public async ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var wrapper = (IRequestHandlerWrapper<TResponse>)_requestHandlerWrappersCache.GetOrAdd(requestType, rt =>
        {
            var requestHandlerWrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse));
            var requestHandlerWrapper = Activator.CreateInstance(requestHandlerWrapperType) ??
                    throw new InvalidOperationException($"Could not create request handler wrapper for '{requestType.FullName}'.");
            return requestHandlerWrapper;
        });

        return await wrapper.Handle(request, _sp, cancellationToken);
    }

    public async ValueTask Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default) where TNotification : INotification
    {
        var handlers = _sp.GetServices<INotificationHandler<TNotification>>();

        foreach (var handler in handlers)
        {
            await handler.Handle(notification, cancellationToken);
        }
    }
}
