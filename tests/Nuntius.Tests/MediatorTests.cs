using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Nuntius.Tests.DI;

namespace Nuntius.Tests;

public class MediatorTests
{
    [Fact]
    public async Task Send_should_throw_when_handler_not_registered()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var sut = new Mediator(sp);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.Send(new FakeRequest()));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.Send(new FakeRequestWithResponse()));
    }

    [Fact]
    public async Task Send_should_execute_registered_handler()
    {
        var services = new ServiceCollection();

        var handler = NSubstitute.Substitute.For<IRequestHandler<FakeRequest>>();
        services.AddTransient<IRequestHandler<FakeRequest>>(_ => handler);

        var sp = services.BuildServiceProvider();
        var sut = new Mediator(sp);

        var request = new FakeRequest();

        await sut.Send(request);

        await handler.Received(1)
                    .Handle(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendWithResponse_should_execute_registered_handler()
    {
        var services = new ServiceCollection();

        var handler = NSubstitute.Substitute.For<IRequestHandler<FakeRequestWithResponse, string>>();
        services.AddTransient<IRequestHandler<FakeRequestWithResponse, string>>(_ => handler);

        var sp = services.BuildServiceProvider();
        var sut = new Mediator(sp);

        var request = new FakeRequestWithResponse();

        await sut.Send(request);

        await handler.Received(1)
                    .Handle(request, Arg.Any<CancellationToken>());
    }
}