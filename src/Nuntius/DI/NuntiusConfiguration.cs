using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Nuntius.DI;

public class NuntiusConfiguration
{
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

    internal List<Assembly> AssembliesToRegister { get; } = new();

    public void RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssembly(typeof(T).Assembly);
    
    public void RegisterServicesFromAssemblyContaining(Type type)
    => RegisterServicesFromAssembly(type.Assembly);

    private NuntiusConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        this.AssembliesToRegister.Add(assembly);

        return this;
    }
}
