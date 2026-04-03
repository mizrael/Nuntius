using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Nuntius.DI;

/// <summary>
/// Builder for constructing an immutable <see cref="NuntiusConfiguration"/> instance.
/// </summary>
public sealed class NuntiusConfigurationBuilder
{
    private ServiceLifetime _lifetime = ServiceLifetime.Transient;
    private IPublishStrategy _publishStrategy = SequentialPublishStrategy.Instance;
    private readonly List<Assembly> _assemblies = new();

    /// <summary>
    /// Sets the service lifetime used to register handler services in the dependency injection container.
    /// Default value is <see cref="ServiceLifetime.Transient"/>.
    /// </summary>
    /// <param name="lifetime">The service lifetime to use for handler registrations.</param>
    /// <returns>The builder instance for chaining.</returns>
    public NuntiusConfigurationBuilder WithLifetime(ServiceLifetime lifetime)
    {
        _lifetime = lifetime;
        return this;
    }

    /// <summary>
    /// Sets the default publishing strategy used when publishing notifications.
    /// Default value is <see cref="SequentialPublishStrategy.Instance"/>.
    /// </summary>
    /// <param name="strategy">The publish strategy to use. Cannot be null.</param>
    /// <returns>The builder instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="strategy"/> is null.</exception>
    public NuntiusConfigurationBuilder WithPublishStrategy(IPublishStrategy strategy)
    {
        _publishStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        return this;
    }

    /// <summary>
    /// Register various handlers from assembly containing given type.
    /// </summary>
    /// <typeparam name="T">Type from assembly to scan.</typeparam>
    /// <remarks>
    /// Implementations of the following interfaces found in the assembly will be registered:
    /// <list type="bullet">
    ///     <item><see cref="IRequestHandler{TRequest}"/></item>
    ///     <item><see cref="IRequestHandler{TRequest, TResponse}"/></item>
    ///     <item><see cref="INotificationHandler{TNotification}"/></item>
    /// </list>
    /// </remarks>
    /// <returns>The builder instance for chaining.</returns>
    public NuntiusConfigurationBuilder RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssembly(typeof(T).Assembly);

    /// <summary>
    /// Register various handlers from assembly containing given type.
    /// </summary>
    /// <param name="type">Type from assembly to scan.</param>
    /// <remarks>
    /// Implementations of the following interfaces found in the assembly will be registered:
    /// <list type="bullet">
    ///     <item><see cref="IRequestHandler{TRequest}"/></item>
    ///     <item><see cref="IRequestHandler{TRequest, TResponse}"/></item>
    ///     <item><see cref="INotificationHandler{TNotification}"/></item>
    /// </list>
    /// </remarks>
    /// <returns>The builder instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public NuntiusConfigurationBuilder RegisterServicesFromAssemblyContaining(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return RegisterServicesFromAssembly(type.Assembly);
    }

    internal NuntiusConfiguration Build()
    {
        if (_assemblies.Count == 0)
            throw new InvalidOperationException($"At least one assembly must be registered. Call {nameof(RegisterServicesFromAssemblyContaining)} before building.");

        return new NuntiusConfiguration(
            _lifetime,
            _publishStrategy,
            _assemblies.ToArray());
    }

    private NuntiusConfigurationBuilder RegisterServicesFromAssembly(Assembly assembly)
    {
        _assemblies.Add(assembly);
        return this;
    }
}
