using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Nuntius;

internal class Mediator : IMediator
{
    private readonly IServiceProvider _sp;
    private readonly ConcurrentDictionary<Type, object> _wrappersCache = new();

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
        var wrapper = (IHandlerWrapper<TResponse>)_wrappersCache.GetOrAdd(requestType, rt =>
        {
            var wrapperType = typeof(HandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse));
            var wrapper = Activator.CreateInstance(wrapperType) ?? 
                    throw new InvalidOperationException($"Could not create handler wrapper for '{requestType.FullName}'.");
            return wrapper;
        });
       
        return await wrapper.Handle(request, _sp, cancellationToken);
    }
}
