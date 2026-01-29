using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Nuntius.DI;

/// <summary>
/// Configuration options for Nuntius library dependency injection integration.
/// </summary>
public class NuntiusConfiguration
{
    /// <summary>
    /// Gets or sets the service lifetime used to register services in the dependency injection container.
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
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

    internal List<Assembly> AssembliesToRegister { get; } = new();

    /// <summary>
    /// Register various handlers from assembly containing given type.
    /// </summary>
    /// <typeparam name="T">
    /// Type from assembly to scan.
    /// </typeparam>
    /// <remarks>
    /// Implementations of the following interfaces found in the assembly will be registered:
    /// <list type="bullet">
    ///     <item><see cref="IRequestHandler{TRequest}"/></item>
    ///     <item><see cref="IRequestHandler{TRequest, TResponse}"/></item>
    ///     <item><see cref="INotificationHandler{TNotification}"/></item>
    /// </list>
    /// </remarks>
    public void RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssembly(typeof(T).Assembly);

    /// <summary>
    /// Register various handlers from assembly containing given type.
    /// </summary>
    /// <param name="type">
    /// Type from assembly to scan.
    /// </param>
    /// <remarks>
    /// Implementations of the following interfaces found in the assembly will be registered:
    /// <list type="bullet">
    ///     <item><see cref="IRequestHandler{TRequest}"/></item>
    ///     <item><see cref="IRequestHandler{TRequest, TResponse}"/></item>
    ///     <item><see cref="INotificationHandler{TNotification}"/></item>
    /// </list>
    /// </remarks>
    public void RegisterServicesFromAssemblyContaining(Type type)
    => RegisterServicesFromAssembly(type.Assembly);

    private NuntiusConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        this.AssembliesToRegister.Add(assembly);

        return this;
    }
}
