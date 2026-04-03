using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Nuntius.Tests.DI;

namespace Nuntius.Tests;

public class MediatorTests
{
    [Fact]
    public async Task Send_should_throw_when_handler_not_registered()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var sut = new Mediator(sp, new SequentialPublishStrategy());
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
        var sut = new Mediator(sp, new SequentialPublishStrategy());

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
        var sut = new Mediator(sp, new SequentialPublishStrategy());

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
        var sut = new Mediator(sp, new SequentialPublishStrategy());

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
        var sut = new Mediator(sp, new SequentialPublishStrategy());

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

    [Fact]
    public async Task Publish_should_delegate_to_publish_strategy()
    {
        // Arrange
        var services = new ServiceCollection();

        var handler = Substitute.For<INotificationHandler<FakeNotification>>();
        services.AddTransient(_ => handler);

        await using var sp = services.BuildServiceProvider();
        var strategy = Substitute.For<IPublishStrategy>();
        var sut = new Mediator(sp, strategy);

        var notification = new FakeNotification();

        // Act
        await sut.Publish(notification);

        // Assert
        await strategy
            .Received(1)
            .ExecuteAsync(
                Arg.Any<IEnumerable<INotificationHandler<FakeNotification>>>(),
                notification,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_should_use_provided_strategy_when_specified()
    {
        // Arrange
        var services = new ServiceCollection();

        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler1.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Throws<InvalidOperationException>();
        services.AddTransient(_ => handler1);

        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();
        services.AddTransient(_ => handler2);

        await using var sp = services.BuildServiceProvider();

        // Default strategy is Sequential (stops on error), but we override with ContinueOnError
        var sut = new Mediator(sp, SequentialPublishStrategy.Instance);
        var notification = new FakeNotification();

        // Act — override with continue-on-error strategy
        await Assert.ThrowsAsync<AggregateException>(
            () => sut.Publish(notification, SequentialContinueOnErrorPublishStrategy.Instance).AsTask());

        // Assert — handler2 was called despite handler1 throwing
        await handler2.Received(1).Handle(notification, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_should_use_default_strategy_when_no_strategy_specified()
    {
        // Arrange
        var services = new ServiceCollection();

        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler1.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Throws<InvalidOperationException>();
        services.AddTransient(_ => handler1);

        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();
        services.AddTransient(_ => handler2);

        await using var sp = services.BuildServiceProvider();

        // Default strategy is SequentialContinueOnError
        var sut = new Mediator(sp, SequentialContinueOnErrorPublishStrategy.Instance);
        var notification = new FakeNotification();

        // Act — no strategy override, should use the default (continue on error)
        await Assert.ThrowsAsync<AggregateException>(
            () => sut.Publish(notification).AsTask());

        // Assert — handler2 was called (continue-on-error is the default)
        await handler2.Received(1).Handle(notification, Arg.Any<CancellationToken>());
    }
}