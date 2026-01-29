using Microsoft.Extensions.DependencyInjection;

namespace Nuntius.DI;

internal static class NuntiusInitializer
{
    private static readonly HashSet<Type> _handlerTypes;

    static NuntiusInitializer()
    {
        _handlerTypes = [typeof(IRequestHandler<>), typeof(IRequestHandler<,>), typeof(INotificationHandler<>)];
    }

    public static void Register(IServiceCollection services, NuntiusConfiguration configuration)
    {
        if (!configuration.AssembliesToRegister.Any())
            throw new ApplicationException("No assemblies have been registered.");

        foreach (var assembly in configuration.AssembliesToRegister)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                RegisterType(services, type, configuration.Lifetime);
            }
        }
    }

    internal static void RegisterType(
        IServiceCollection services,
        Type type,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var interfaces = type.GetInterfaces();

        foreach (var ti in interfaces)
        {
            if (!ti.IsGenericType)
                continue;

            var openGeneric = ti.GetGenericTypeDefinition();

            var implementationType = _handlerTypes.FirstOrDefault(ht => openGeneric.IsAssignableFrom(ht));
            if (implementationType is not null)
                services.Add(new ServiceDescriptor(ti, type, lifetime));
        }
    }
}