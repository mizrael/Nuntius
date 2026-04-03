using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Nuntius.Tests.DI;

namespace Nuntius.Tests.Publishing;

public class SequentialContinueOnErrorPublishStrategyTests
{
    private readonly SequentialContinueOnErrorPublishStrategy _sut = SequentialContinueOnErrorPublishStrategy.Instance;

    [Fact]
    public async Task ExecuteAsync_should_do_nothing_when_no_handlers()
    {
        var handlers = Enumerable.Empty<INotificationHandler<FakeNotification>>();
        var notification = new FakeNotification();

        var ex = await Record.ExceptionAsync(
            () => _sut.ExecuteAsync(handlers, notification, CancellationToken.None).AsTask());

        Assert.Null(ex);
    }

    [Fact]
    public async Task ExecuteAsync_should_call_all_handlers_even_when_one_throws()
    {
        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler1.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Throws<InvalidOperationException>();

        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();

        var handlers = new[] { handler1, handler2 };
        var notification = new FakeNotification();

        await Assert.ThrowsAsync<AggregateException>(
            () => _sut.ExecuteAsync(handlers, notification, CancellationToken.None).AsTask());

        await handler2.Received(1).Handle(notification, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_should_throw_AggregateException_with_all_failures()
    {
        var ex1 = new InvalidOperationException("error 1");
        var ex2 = new ArgumentException("error 2");

        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler1.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Throws(ex1);

        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();

        var handler3 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler3.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Throws(ex2);

        var handlers = new[] { handler1, handler2, handler3 };
        var notification = new FakeNotification();

        var thrown = await Assert.ThrowsAsync<AggregateException>(
            () => _sut.ExecuteAsync(handlers, notification, CancellationToken.None).AsTask());

        Assert.Equal(2, thrown.InnerExceptions.Count);
        Assert.Same(ex1, thrown.InnerExceptions[0]);
        Assert.Same(ex2, thrown.InnerExceptions[1]);
    }

    [Fact]
    public async Task ExecuteAsync_should_not_throw_when_no_handler_fails()
    {
        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();

        var handlers = new[] { handler1, handler2 };
        var notification = new FakeNotification();

        var ex = await Record.ExceptionAsync(
            () => _sut.ExecuteAsync(handlers, notification, CancellationToken.None).AsTask());

        Assert.Null(ex);
    }

    [Fact]
    public async Task ExecuteAsync_should_execute_handlers_sequentially()
    {
        var callOrder = new List<int>();

        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler1.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask)
            .AndDoes(_ => callOrder.Add(1));

        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler2.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask)
            .AndDoes(_ => callOrder.Add(2));

        var handlers = new[] { handler1, handler2 };
        var notification = new FakeNotification();

        await _sut.ExecuteAsync(handlers, notification, CancellationToken.None);

        Assert.Equal([1, 2], callOrder);
    }

    [Fact]
    public async Task ExecuteAsync_should_pass_cancellation_token_to_handlers()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var notification = new FakeNotification();
        var handler = Substitute.For<INotificationHandler<FakeNotification>>();

        await _sut.ExecuteAsync([handler], notification, token);

        await handler.Received(1).Handle(notification, token);
    }
}
