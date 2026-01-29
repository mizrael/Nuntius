using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Nuntius.DI;

/// <summary>
/// Extension methods for Nuntius library dependency injection integration.
/// </summary>
[ExcludeFromCodeCoverage]
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Registers handlers and mediator types from the specified assemblies.
    /// </summary>
    /// <param name="services">
    /// Service collection to register types into.
    /// </param>
    /// <param name="configuration">
    /// The action used to configure the options.
    /// </param>
    /// <returns>
    /// The provided service collection object with registered types.
    /// </returns>
    /// <remarks>
    /// The following types are registered with transient lifetime:
    /// <list type="bullet">
    ///     <item><see cref="ISender"/></item>
    ///     <item><see cref="IPublisher"/></item>
    ///     <item><see cref="IMediator"/></item>
    /// </list>
    /// The following types are registered with the lifetime specified in <see cref="NuntiusConfiguration.Lifetime"/>:
    /// <list type="bullet">
    ///     <item><see cref="IRequestHandler{TRequest}"/></item>
    ///     <item><see cref="IRequestHandler{TRequest, TResponse}"/></item>
    ///     <item><see cref="INotificationHandler{TNotification}"/></item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddNuntius(
        this IServiceCollection services,
        Action<NuntiusConfiguration> configuration)
    {
        var config = new NuntiusConfiguration();
        configuration(config);

        return AddNuntius(services, config);
    }

    /// <summary>
    /// Registers handlers and mediator types from the specified assemblies.
    /// </summary>
    /// <param name="services">
    /// Service collection to register types into.
    /// </param>
    /// <param name="configuration">
    /// Configuration options.
    /// </param>
    /// <returns>
    /// The provided service collection object with registered types.
    /// </returns>
    /// <remarks>
    /// The following types are registered with transient lifetime:
    /// <list type="bullet">
    ///     <item><see cref="ISender"/></item>
    ///     <item><see cref="IPublisher"/></item>
    ///     <item><see cref="IMediator"/></item>
    /// </list>
    /// The following types are registered with the lifetime specified in <see cref="NuntiusConfiguration.Lifetime"/>:
    /// <list type="bullet">
    ///     <item><see cref="IRequestHandler{TRequest}"/></item>
    ///     <item><see cref="IRequestHandler{TRequest, TResponse}"/></item>
    ///     <item><see cref="INotificationHandler{TNotification}"/></item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddNuntius(
        this IServiceCollection services,
        NuntiusConfiguration configuration)
    {
        NuntiusInitializer.Register(services, configuration);

        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.AddTransient<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        return services;
    }
}
