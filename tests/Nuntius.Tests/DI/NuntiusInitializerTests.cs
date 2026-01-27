using Microsoft.Extensions.DependencyInjection;
using Nuntius.DI;

namespace Nuntius.Tests.DI;

public class NuntiusInitializerTests
{
    [Theory]
    [InlineData(typeof(FakeRequestHandler), typeof(IRequestHandler<FakeRequest>))]
    [InlineData(typeof(FakeRequestWithResponseHandler), typeof(IRequestHandler<FakeRequestWithResponse, string>))]
    public void RegisterType_should_register_type_when_valid(Type typeToRegister, Type expectedHandlerType)
    {
        var services = new ServiceCollection();
        NuntiusInitializer.RegisterType(services, typeToRegister);
        var descriptor = services.FirstOrDefault(sd =>
            sd.ServiceType == expectedHandlerType &&
            sd.ImplementationType == typeToRegister);
        Assert.NotNull(descriptor);
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void RegisterType_should_register_type_with_specified_lifetime(ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();
        NuntiusInitializer.RegisterType(services, typeof(FakeRequestHandler), lifetime);

        var descriptor = GetDescriptor(services, typeof(IRequestHandler<FakeRequest>), typeof(FakeRequestHandler), lifetime);
        Assert.NotNull(descriptor);
    }

    private static ServiceDescriptor? GetDescriptor(
        ServiceCollection services,
        Type expectedHandlerType,
        Type typeToRegister,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        return services.FirstOrDefault(sd =>
            sd.ServiceType == expectedHandlerType &&
            sd.ImplementationType == typeToRegister &&
            sd.Lifetime == lifetime);
    }
}
