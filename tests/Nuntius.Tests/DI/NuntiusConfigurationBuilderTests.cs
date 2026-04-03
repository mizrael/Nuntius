using Microsoft.Extensions.DependencyInjection;
using Nuntius.DI;

namespace Nuntius.Tests.DI;

public class NuntiusConfigurationBuilderTests
{
    [Fact]
    public void Build_should_use_default_values()
    {
        var config = new NuntiusConfigurationBuilder()
            .RegisterServicesFromAssemblyContaining<NuntiusConfigurationBuilderTests>()
            .Build();

        Assert.Equal(ServiceLifetime.Transient, config.Lifetime);
        Assert.IsType<SequentialPublishStrategy>(config.DefaultPublishStrategy);
    }

    [Fact]
    public void Build_should_throw_when_no_assemblies_registered()
    {
        var builder = new NuntiusConfigurationBuilder();

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void WithLifetime_should_set_lifetime(ServiceLifetime lifetime)
    {
        var config = new NuntiusConfigurationBuilder()
            .WithLifetime(lifetime)
            .RegisterServicesFromAssemblyContaining<NuntiusConfigurationBuilderTests>()
            .Build();

        Assert.Equal(lifetime, config.Lifetime);
    }

    [Fact]
    public void WithPublishStrategy_should_set_strategy()
    {
        var config = new NuntiusConfigurationBuilder()
            .WithPublishStrategy(ParallelPublishStrategy.Instance)
            .RegisterServicesFromAssemblyContaining<NuntiusConfigurationBuilderTests>()
            .Build();

        Assert.Same(ParallelPublishStrategy.Instance, config.DefaultPublishStrategy);
    }

    [Fact]
    public void WithPublishStrategy_should_throw_when_null()
    {
        var builder = new NuntiusConfigurationBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithPublishStrategy(null!));
    }

    [Fact]
    public void RegisterServicesFromAssemblyContaining_generic_should_add_assembly()
    {
        var config = new NuntiusConfigurationBuilder()
            .RegisterServicesFromAssemblyContaining<NuntiusConfigurationBuilderTests>()
            .Build();

        Assert.Single(config.AssembliesToRegister);
        Assert.Equal(typeof(NuntiusConfigurationBuilderTests).Assembly, config.AssembliesToRegister[0]);
    }

    [Fact]
    public void RegisterServicesFromAssemblyContaining_type_should_add_assembly()
    {
        var config = new NuntiusConfigurationBuilder()
            .RegisterServicesFromAssemblyContaining(typeof(NuntiusConfigurationBuilderTests))
            .Build();

        Assert.Single(config.AssembliesToRegister);
        Assert.Equal(typeof(NuntiusConfigurationBuilderTests).Assembly, config.AssembliesToRegister[0]);
    }

    [Fact]
    public void RegisterServicesFromAssemblyContaining_type_should_throw_when_null()
    {
        var builder = new NuntiusConfigurationBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.RegisterServicesFromAssemblyContaining(null!));
    }

    [Fact]
    public void Builder_should_support_fluent_chaining()
    {
        var config = new NuntiusConfigurationBuilder()
            .WithLifetime(ServiceLifetime.Scoped)
            .WithPublishStrategy(ParallelPublishStrategy.Instance)
            .RegisterServicesFromAssemblyContaining<NuntiusConfigurationBuilderTests>()
            .Build();

        Assert.Equal(ServiceLifetime.Scoped, config.Lifetime);
        Assert.Same(ParallelPublishStrategy.Instance, config.DefaultPublishStrategy);
        Assert.Single(config.AssembliesToRegister);
    }

    [Fact]
    public void Configuration_should_be_immutable()
    {
        var config = new NuntiusConfigurationBuilder()
            .RegisterServicesFromAssemblyContaining<NuntiusConfigurationBuilderTests>()
            .Build();

        Assert.IsAssignableFrom<IReadOnlyList<System.Reflection.Assembly>>(config.AssembliesToRegister);
    }
}
