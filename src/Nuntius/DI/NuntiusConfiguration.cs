using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Nuntius.DI;

/// <summary>
/// Immutable configuration options for Nuntius library dependency injection integration.
/// Use <see cref="NuntiusConfigurationBuilder"/> to construct an instance.
/// </summary>
public sealed class NuntiusConfiguration
{
    /// <summary>
    /// Gets the service lifetime used to register services in the dependency injection container.
    /// Default value is <see cref="ServiceLifetime.Transient"/>.
    /// </summary>
    /// <remarks>
    /// This service lifetime is used to register implementations of the following interfaces:
    /// <list type="bullet">
    ///     <item><see cref="IRequestHandler{TRequest}"/></item>
    ///     <item><see cref="IRequestHandler{TRequest, TResponse}"/></item>
    ///     <item><see cref="INotificationHandler{TNotification}"/></item>
    /// </list>
    /// </remarks>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Gets the default publishing strategy used when publishing notifications.
    /// Default value is <see cref="SequentialPublishStrategy.Instance"/>.
    /// </summary>
    /// <remarks>
    /// This strategy is used when <see cref="IPublisher.Publish{TNotification}"/>
    /// is called without an explicit strategy.
    /// Can be overridden per-call by passing a strategy to the publish method.
    /// </remarks>
    public IPublishStrategy DefaultPublishStrategy { get; }

    public IReadOnlyList<Assembly> AssembliesToRegister { get; }

    internal NuntiusConfiguration(
        ServiceLifetime lifetime,
        IPublishStrategy defaultPublishStrategy,
        IReadOnlyList<Assembly> assembliesToRegister)
    {
        Lifetime = lifetime;
        DefaultPublishStrategy = defaultPublishStrategy;
        AssembliesToRegister = assembliesToRegister;
    }
}
