using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Nuntius.Tests.DI;

namespace Nuntius.Tests.Publishing;

public class SequentialPublishStrategyTests
{
    private readonly SequentialPublishStrategy _sut = SequentialPublishStrategy.Instance;

    [Fact]
    public async Task ExecuteAsync_should_call_all_handlers()
    {
        // Arrange
        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();
        var notification = new FakeNotification();

        // Act
        await _sut.ExecuteAsync([handler1, handler2], notification, CancellationToken.None);

        // Assert
        await handler1.Received(1).Handle(notification, CancellationToken.None);
        await handler2.Received(1).Handle(notification, CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_should_not_throw_when_no_handlers()
    {
        var notification = new FakeNotification();
        var handlers = Enumerable.Empty<INotificationHandler<FakeNotification>>();

        var ex = await Record.ExceptionAsync(
            () => _sut.ExecuteAsync(handlers, notification, CancellationToken.None).AsTask());

        Assert.Null(ex);
    }

    [Fact]
    public async Task ExecuteAsync_should_execute_handlers_in_order()
    {
        // Arrange
        var callOrder = new List<int>();
        var notification = new FakeNotification();

        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler1.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask)
            .AndDoes(_ => callOrder.Add(1));

        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler2.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask)
            .AndDoes(_ => callOrder.Add(2));

        var handler3 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler3.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask)
            .AndDoes(_ => callOrder.Add(3));

        // Act
        await _sut.ExecuteAsync([handler1, handler2, handler3], notification, CancellationToken.None);

        // Assert
        Assert.Equal([1, 2, 3], callOrder);
    }

    [Fact]
    public async Task ExecuteAsync_should_stop_on_first_exception()
    {
        // Arrange
        var notification = new FakeNotification();

        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler1.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("handler1 failed"));

        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync([handler1, handler2], notification, CancellationToken.None).AsTask());

        // Assert
        await handler1.Received(1).Handle(notification, Arg.Any<CancellationToken>());
        await handler2.DidNotReceiveWithAnyArgs().Handle(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_should_propagate_exception_unchanged()
    {
        // Arrange
        var notification = new FakeNotification();
        var expectedException = new InvalidOperationException("test");

        var handler = Substitute.For<INotificationHandler<FakeNotification>>();
        handler.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Throws(expectedException);

        // Act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync([handler], notification, CancellationToken.None).AsTask());

        // Assert
        Assert.Same(expectedException, thrown);
    }

    [Fact]
    public async Task ExecuteAsync_should_pass_cancellation_token_to_handlers()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var notification = new FakeNotification();
        var handler = Substitute.For<INotificationHandler<FakeNotification>>();

        // Act
        await _sut.ExecuteAsync([handler], notification, token);

        // Assert
        await handler.Received(1).Handle(notification, token);
    }
}
