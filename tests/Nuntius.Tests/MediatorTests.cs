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

    [Fact]
    public async Task Publish_should_not_throw_when_handler_not_registered()
    {
        // Arrange
        var services = new ServiceCollection();
        await using var sp = services.BuildServiceProvider();
        var sut = new Mediator(sp);

        // Act
        var ex = await Record.ExceptionAsync(async () => await sut.Publish(new FakeNotification()));

        // Assert
        Assert.Null(ex);
    }

    [Fact]
    public async Task Publish_should_execute_all_registered_handlers()
    {
        // Arrange
        var services = new ServiceCollection();

        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        services.AddTransient(_ => handler1);

        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();
        services.AddTransient(_ => handler2);

        await using var sp = services.BuildServiceProvider();
        var sut = new Mediator(sp);

        var notification = new FakeNotification();

        // Act
        await sut.Publish(notification);

        // Assert
        await handler1
            .Received(1)
            .Handle(notification, Arg.Any<CancellationToken>());

        await handler2
            .Received(1)
            .Handle(notification, Arg.Any<CancellationToken>());
    }
}