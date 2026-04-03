using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Nuntius.Tests.DI;

namespace Nuntius.Tests.Publishing;

public class ParallelPublishStrategyTests
{
    private readonly ParallelPublishStrategy _sut = ParallelPublishStrategy.Instance;

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
    public async Task ExecuteAsync_should_execute_all_handlers_even_when_one_throws()
    {
        // Arrange
        var notification = new FakeNotification();

        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler1.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("handler1 failed"));

        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();

        // Act
        await Assert.ThrowsAsync<AggregateException>(
            () => _sut.ExecuteAsync([handler1, handler2], notification, CancellationToken.None).AsTask());

        // Assert — both handlers should have been called
        await handler1.Received(1).Handle(notification, Arg.Any<CancellationToken>());
        await handler2.Received(1).Handle(notification, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_should_aggregate_multiple_exceptions()
    {
        // Arrange
        var notification = new FakeNotification();

        var exception1 = new InvalidOperationException("first");
        var exception2 = new ArgumentException("second");

        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler1.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Throws(exception1);

        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler2.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Throws(exception2);

        // Act
        var thrown = await Assert.ThrowsAsync<AggregateException>(
            () => _sut.ExecuteAsync([handler1, handler2], notification, CancellationToken.None).AsTask());

        // Assert
        Assert.Equal(2, thrown.InnerExceptions.Count);
        Assert.Contains(thrown.InnerExceptions, e => ReferenceEquals(e, exception1));
        Assert.Contains(thrown.InnerExceptions, e => ReferenceEquals(e, exception2));
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

    [Fact]
    public async Task ExecuteAsync_should_propagate_cancellation_without_NRE()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var notification = new FakeNotification();
        var handler = Substitute.For<INotificationHandler<FakeNotification>>();
        handler.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return ValueTask.CompletedTask;
            });

        // Act & Assert — should throw OperationCanceledException, not NullReferenceException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _sut.ExecuteAsync([handler], notification, cts.Token).AsTask());
    }
}
