using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Nuntius.DI;

[ExcludeFromCodeCoverage]
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddNuntius(this IServiceCollection services, Action<NuntiusConfiguration> configuration)
    {
        var config = new NuntiusConfiguration();
        configuration(config);

        return AddNuntius(services, config);
    }

    public static IServiceCollection AddNuntius(this IServiceCollection services, NuntiusConfiguration configuration)
    {
        NuntiusInitializer.Register(services, configuration);

        return services.AddTransient<IMediator, Mediator>();
    }
}
