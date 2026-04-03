# Publishing Strategies Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Introduce an `IPublishStrategy` abstraction so users can configure how notification handlers are executed (sequentially or in parallel, with configurable error handling).

**Architecture:** Extract handler execution from `Mediator.Publish` into strategy classes behind an `IPublishStrategy` interface. Three built-in strategies: `SequentialStrategy` (default, current behavior), `SequentialContinueOnErrorStrategy`, `ParallelStrategy`. Configured at DI time via `NuntiusConfiguration.DefaultPublishStrategy`, overridable per-call via nullable parameter on `IPublisher.Publish`.

**Tech Stack:** .NET 10 / C# 12, xUnit 2.9.3, NSubstitute 5.3.0, Microsoft.Extensions.DependencyInjection.Abstractions 10.0.1

**Design doc:** `docs/plans/2026-04-02-publishing-strategies-design.md`

**Build & test command:** `dotnet test` from repo root (13 tests baseline, all passing)

**Key codebase facts:**
- `Mediator` is `internal` — tests construct it directly via `InternalsVisibleTo`
- Test fakes live in `tests/Nuntius.Tests/DI/Fakes.cs` (`FakeNotification`, `FakeNotificationHandler`)
- `IPublisher` currently has one method: `ValueTask Publish<T>(T notification, CancellationToken ct = default)`
- `NuntiusConfiguration` lives in `src/Nuntius/DI/NuntiusConfiguration.cs`
- DI registration lives in `src/Nuntius/DI/IServiceCollectionExtensions.cs`

---

### Task 1: Create `IPublishStrategy` Interface

**Files:**
- Create: `src/Nuntius/Publishing/IPublishStrategy.cs`

**Step 1: Create the interface**

```csharp
namespace Nuntius;

/// <summary>
/// Defines a strategy for executing notification handlers during publishing.
/// </summary>
public interface IPublishStrategy
{
    /// <summary>
    /// Executes the given notification handlers according to the strategy's semantics.
    /// </summary>
    /// <param name="handlers">The resolved notification handlers to execute.</param>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <typeparam name="TNotification">The type of notification being published.</typeparam>
    /// <returns>A value task representing the asynchronous operation.</returns>
    ValueTask ExecuteAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken) where TNotification : INotification;
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Nuntius/Nuntius.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/Nuntius/Publishing/IPublishStrategy.cs
git commit -m "feat: add IPublishStrategy interface

Defines the core abstraction for notification publishing strategies.
Strategies receive resolved handlers and execute them according to
their specific semantics (sequential, parallel, etc.)

Refs #4

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 2: Implement `SequentialStrategy` (TDD)

**Files:**
- Create: `tests/Nuntius.Tests/Publishing/SequentialStrategyTests.cs`
- Create: `src/Nuntius/Publishing/SequentialStrategy.cs`

**Step 1: Write the failing tests**

Create `tests/Nuntius.Tests/Publishing/SequentialStrategyTests.cs`:

```csharp
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Nuntius.Tests.DI;

namespace Nuntius.Tests.Publishing;

public class SequentialStrategyTests
{
    private readonly SequentialStrategy _sut = SequentialStrategy.Instance;

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
    public async Task ExecuteAsync_should_call_all_handlers_sequentially()
    {
        var callOrder = new List<int>();

        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler1.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Returns(ci => { callOrder.Add(1); return ValueTask.CompletedTask; });

        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler2.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Returns(ci => { callOrder.Add(2); return ValueTask.CompletedTask; });

        var handlers = new[] { handler1, handler2 };
        var notification = new FakeNotification();

        await _sut.ExecuteAsync(handlers, notification, CancellationToken.None);

        Assert.Equal([1, 2], callOrder);
    }

    [Fact]
    public async Task ExecuteAsync_should_stop_on_first_exception()
    {
        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler1.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Throws<InvalidOperationException>();

        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();

        var handlers = new[] { handler1, handler2 };
        var notification = new FakeNotification();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(handlers, notification, CancellationToken.None).AsTask());

        await handler2.DidNotReceiveWithAnyArgs().Handle(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_should_throw_original_exception_unchanged()
    {
        var exception = new InvalidOperationException("test error");

        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler1.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Throws(exception);

        var handlers = new[] { handler1 };
        var notification = new FakeNotification();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExecuteAsync(handlers, notification, CancellationToken.None).AsTask());

        Assert.Same(exception, thrown);
    }

    [Fact]
    public async Task ExecuteAsync_should_pass_notification_and_cancellation_token()
    {
        var handler = Substitute.For<INotificationHandler<FakeNotification>>();
        var notification = new FakeNotification();
        using var cts = new CancellationTokenSource();

        await _sut.ExecuteAsync(new[] { handler }, notification, cts.Token);

        await handler.Received(1).Handle(notification, cts.Token);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Nuntius.Tests --filter "FullyQualifiedName~SequentialStrategyTests" --verbosity quiet`
Expected: FAIL — `SequentialStrategy` does not exist

**Step 3: Write minimal implementation**

Create `src/Nuntius/Publishing/SequentialStrategy.cs`:

```csharp
namespace Nuntius;

/// <summary>
/// Executes notification handlers sequentially, stopping on the first exception.
/// The original exception is rethrown unchanged.
/// </summary>
/// <remarks>
/// This is the default publishing strategy and matches the original Nuntius behavior.
/// </remarks>
public sealed class SequentialStrategy : IPublishStrategy
{
    /// <summary>
    /// Singleton instance. This strategy is stateless and thread-safe.
    /// </summary>
    public static readonly SequentialStrategy Instance = new();

    private SequentialStrategy() { }

    /// <inheritdoc />
    public async ValueTask ExecuteAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken) where TNotification : INotification
    {
        foreach (var handler in handlers)
        {
            await handler.Handle(notification, cancellationToken);
        }
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Nuntius.Tests --filter "FullyQualifiedName~SequentialStrategyTests" --verbosity quiet`
Expected: PASS (5 tests)

**Step 5: Commit**

```bash
git add src/Nuntius/Publishing/SequentialStrategy.cs tests/Nuntius.Tests/Publishing/SequentialStrategyTests.cs
git commit -m "feat: add SequentialStrategy (default publishing strategy)

Executes handlers one-by-one, stopping on first exception.
Matches the current Mediator.Publish behavior exactly.

Refs #4

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 3: Implement `SequentialContinueOnErrorStrategy` (TDD)

**Files:**
- Create: `tests/Nuntius.Tests/Publishing/SequentialContinueOnErrorStrategyTests.cs`
- Create: `src/Nuntius/Publishing/SequentialContinueOnErrorStrategy.cs`

**Step 1: Write the failing tests**

Create `tests/Nuntius.Tests/Publishing/SequentialContinueOnErrorStrategyTests.cs`:

```csharp
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Nuntius.Tests.DI;

namespace Nuntius.Tests.Publishing;

public class SequentialContinueOnErrorStrategyTests
{
    private readonly SequentialContinueOnErrorStrategy _sut = SequentialContinueOnErrorStrategy.Instance;

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
            .Returns(ci => { callOrder.Add(1); return ValueTask.CompletedTask; });

        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler2.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Returns(ci => { callOrder.Add(2); return ValueTask.CompletedTask; });

        var handlers = new[] { handler1, handler2 };
        var notification = new FakeNotification();

        await _sut.ExecuteAsync(handlers, notification, CancellationToken.None);

        Assert.Equal([1, 2], callOrder);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Nuntius.Tests --filter "FullyQualifiedName~SequentialContinueOnErrorStrategyTests" --verbosity quiet`
Expected: FAIL — `SequentialContinueOnErrorStrategy` does not exist

**Step 3: Write minimal implementation**

Create `src/Nuntius/Publishing/SequentialContinueOnErrorStrategy.cs`:

```csharp
namespace Nuntius;

/// <summary>
/// Executes notification handlers sequentially, continuing execution when a handler throws.
/// All exceptions are collected and thrown as an <see cref="AggregateException"/> after all handlers complete.
/// </summary>
/// <remarks>
/// Useful for fire-and-forget scenarios (telemetry, logging, cache invalidation)
/// where one handler's failure should not prevent others from running.
/// </remarks>
public sealed class SequentialContinueOnErrorStrategy : IPublishStrategy
{
    /// <summary>
    /// Singleton instance. This strategy is stateless and thread-safe.
    /// </summary>
    public static readonly SequentialContinueOnErrorStrategy Instance = new();

    private SequentialContinueOnErrorStrategy() { }

    /// <inheritdoc />
    public async ValueTask ExecuteAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken) where TNotification : INotification
    {
        List<Exception>? exceptions = null;

        foreach (var handler in handlers)
        {
            try
            {
                await handler.Handle(notification, cancellationToken);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException(exceptions);
        }
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Nuntius.Tests --filter "FullyQualifiedName~SequentialContinueOnErrorStrategyTests" --verbosity quiet`
Expected: PASS (5 tests)

**Step 5: Commit**

```bash
git add src/Nuntius/Publishing/SequentialContinueOnErrorStrategy.cs tests/Nuntius.Tests/Publishing/SequentialContinueOnErrorStrategyTests.cs
git commit -m "feat: add SequentialContinueOnErrorStrategy

Executes handlers one-by-one, collecting exceptions from each.
Throws AggregateException with all failures after all handlers complete.

Refs #4

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 4: Implement `ParallelStrategy` (TDD)

**Files:**
- Create: `tests/Nuntius.Tests/Publishing/ParallelStrategyTests.cs`
- Create: `src/Nuntius/Publishing/ParallelStrategy.cs`

**Step 1: Write the failing tests**

Create `tests/Nuntius.Tests/Publishing/ParallelStrategyTests.cs`:

```csharp
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Nuntius.Tests.DI;

namespace Nuntius.Tests.Publishing;

public class ParallelStrategyTests
{
    private readonly ParallelStrategy _sut = ParallelStrategy.Instance;

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
    public async Task ExecuteAsync_should_call_all_handlers()
    {
        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();

        var handlers = new[] { handler1, handler2 };
        var notification = new FakeNotification();

        await _sut.ExecuteAsync(handlers, notification, CancellationToken.None);

        await handler1.Received(1).Handle(notification, Arg.Any<CancellationToken>());
        await handler2.Received(1).Handle(notification, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_should_execute_handlers_concurrently()
    {
        var barrier = new TaskCompletionSource();
        var handler1Started = new TaskCompletionSource();
        var handler2Started = new TaskCompletionSource();

        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler1.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                handler1Started.SetResult();
                await barrier.Task;
            });

        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler2.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                handler2Started.SetResult();
                await barrier.Task;
            });

        var handlers = new[] { handler1, handler2 };
        var notification = new FakeNotification();

        var publishTask = _sut.ExecuteAsync(handlers, notification, CancellationToken.None).AsTask();

        // Both handlers should start before the barrier is released
        var startTasks = Task.WhenAll(handler1Started.Task, handler2Started.Task);
        var completed = await Task.WhenAny(startTasks, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(startTasks, completed);

        barrier.SetResult();
        await publishTask;
    }

    [Fact]
    public async Task ExecuteAsync_should_throw_when_handler_fails()
    {
        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        handler1.Handle(Arg.Any<FakeNotification>(), Arg.Any<CancellationToken>())
            .Throws<InvalidOperationException>();

        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();

        var handlers = new[] { handler1, handler2 };
        var notification = new FakeNotification();

        var ex = await Record.ExceptionAsync(
            () => _sut.ExecuteAsync(handlers, notification, CancellationToken.None).AsTask());

        Assert.NotNull(ex);
    }

    [Fact]
    public async Task ExecuteAsync_should_pass_cancellation_token_to_all_handlers()
    {
        var handler1 = Substitute.For<INotificationHandler<FakeNotification>>();
        var handler2 = Substitute.For<INotificationHandler<FakeNotification>>();

        using var cts = new CancellationTokenSource();
        var notification = new FakeNotification();

        await _sut.ExecuteAsync(new[] { handler1, handler2 }, notification, cts.Token);

        await handler1.Received(1).Handle(notification, cts.Token);
        await handler2.Received(1).Handle(notification, cts.Token);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Nuntius.Tests --filter "FullyQualifiedName~ParallelStrategyTests" --verbosity quiet`
Expected: FAIL — `ParallelStrategy` does not exist

**Step 3: Write minimal implementation**

Create `src/Nuntius/Publishing/ParallelStrategy.cs`:

```csharp
namespace Nuntius;

/// <summary>
/// Executes notification handlers concurrently using <see cref="Task.WhenAll"/>.
/// </summary>
/// <remarks>
/// Useful when handlers are independent and throughput matters.
/// Exception semantics follow <see cref="Task.WhenAll"/> behavior.
/// </remarks>
public sealed class ParallelStrategy : IPublishStrategy
{
    /// <summary>
    /// Singleton instance. This strategy is stateless and thread-safe.
    /// </summary>
    public static readonly ParallelStrategy Instance = new();

    private ParallelStrategy() { }

    /// <inheritdoc />
    public async ValueTask ExecuteAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken) where TNotification : INotification
    {
        var tasks = handlers.Select(h => h.Handle(notification, cancellationToken).AsTask());
        await Task.WhenAll(tasks);
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Nuntius.Tests --filter "FullyQualifiedName~ParallelStrategyTests" --verbosity quiet`
Expected: PASS (5 tests)

**Step 5: Commit**

```bash
git add src/Nuntius/Publishing/ParallelStrategy.cs tests/Nuntius.Tests/Publishing/ParallelStrategyTests.cs
git commit -m "feat: add ParallelStrategy

Executes all handlers concurrently via Task.WhenAll.
Useful when handlers are independent and throughput matters.

Refs #4

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 5: Update `IPublisher` and `IMediator` Interfaces

**Files:**
- Modify: `src/Nuntius/IPublisher.cs`

**Step 1: Update the `Publish` method signature**

In `src/Nuntius/IPublisher.cs`, change the `Publish` method to accept nullable `IPublishStrategy`:

```csharp
namespace Nuntius;

/// <summary>
/// Publish a notification to be handled by multiple handlers.
/// </summary>
/// <remarks>
/// Use this interface for pub-sub scenarios.
/// </remarks>
public interface IPublisher
{
    /// <summary>
    /// Asynchronously publish a notification to multiple handlers.
    /// </summary>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="strategy">
    /// Optional publishing strategy. When <c>null</c>, the default strategy
    /// configured at DI registration time is used.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <typeparam name="TNotification">The type of notification to publish.</typeparam>
    /// <returns>A value task that represents the asynchronous operation.</returns>
    ValueTask Publish<TNotification>(
        TNotification notification,
        IPublishStrategy? strategy = null,
        CancellationToken cancellationToken = default) where TNotification : INotification;
}
```

**Step 2: Verify it compiles** (will fail — `Mediator` doesn't match yet, that's expected)

Run: `dotnet build src/Nuntius/Nuntius.csproj`
Expected: FAIL — `Mediator` does not implement updated `IPublisher.Publish`

**Step 3: Commit** (compile error is temporary — fixed in Task 7)

```bash
git add src/Nuntius/IPublisher.cs
git commit -m "feat: update IPublisher.Publish to accept optional IPublishStrategy

Adds nullable strategy parameter with default null. When null,
the DI-configured default strategy is used.

BREAKING: Callers using positional CancellationToken must switch
to named arguments: Publish(notification, cancellationToken: ct)

Refs #4

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 6: Update `NuntiusConfiguration`

**Files:**
- Modify: `src/Nuntius/DI/NuntiusConfiguration.cs`

**Step 1: Add `DefaultPublishStrategy` property**

Add the following property to `NuntiusConfiguration` class, after the `Lifetime` property:

```csharp
/// <summary>
/// Gets or sets the default publishing strategy used when publishing notifications.
/// Default value is <see cref="SequentialStrategy.Instance"/>.
/// </summary>
/// <remarks>
/// This strategy is used when <see cref="IPublisher.Publish{TNotification}"/>
/// is called without an explicit strategy.
/// Can be overridden per-call by passing a strategy to the publish method.
/// </remarks>
public IPublishStrategy DefaultPublishStrategy { get; set; } = SequentialStrategy.Instance;
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Nuntius/Nuntius.csproj`
Expected: FAIL — `Mediator` still doesn't implement updated interface (fixed in Task 7)

**Step 3: Commit**

```bash
git add src/Nuntius/DI/NuntiusConfiguration.cs
git commit -m "feat: add DefaultPublishStrategy to NuntiusConfiguration

Defaults to SequentialStrategy.Instance (backward compatible).
Users can override at DI time for a different global default.

Refs #4

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 7: Update `Mediator` and DI Registration (TDD)

This is the integration task that wires everything together and makes the project compile again.

**Files:**
- Modify: `src/Nuntius/Mediator.cs`
- Modify: `src/Nuntius/DI/IServiceCollectionExtensions.cs`
- Modify: `tests/Nuntius.Tests/MediatorTests.cs`

**Step 1: Update `Mediator` to inject and use strategy**

Replace the `Mediator` class in `src/Nuntius/Mediator.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Nuntius;

internal class Mediator : IMediator
{
    private readonly IServiceProvider _sp;
    private readonly IPublishStrategy _defaultStrategy;
    private readonly ConcurrentDictionary<Type, object> _requestHandlerWrappersCache = new();

    public Mediator(IServiceProvider sp, IPublishStrategy defaultStrategy)
    {
        _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        _defaultStrategy = defaultStrategy ?? throw new ArgumentNullException(nameof(defaultStrategy));
    }

    public async ValueTask Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        var handler = _sp.GetRequiredService<IRequestHandler<TRequest>>();
        await handler.Handle(request, cancellationToken);
    }

    public async ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var wrapper = (IRequestHandlerWrapper<TResponse>)_requestHandlerWrappersCache.GetOrAdd(requestType, rt =>
        {
            var requestHandlerWrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse));
            var requestHandlerWrapper = Activator.CreateInstance(requestHandlerWrapperType) ??
                    throw new InvalidOperationException($"Could not create request handler wrapper for '{requestType.FullName}'.");
            return requestHandlerWrapper;
        });

        return await wrapper.Handle(request, _sp, cancellationToken);
    }

    public async ValueTask Publish<TNotification>(
        TNotification notification,
        IPublishStrategy? strategy = null,
        CancellationToken cancellationToken = default) where TNotification : INotification
    {
        var effectiveStrategy = strategy ?? _defaultStrategy;
        var handlers = _sp.GetServices<INotificationHandler<TNotification>>();
        await effectiveStrategy.ExecuteAsync(handlers, notification, cancellationToken);
    }
}
```

**Step 2: Update DI registration to register strategy**

In `src/Nuntius/DI/IServiceCollectionExtensions.cs`, update the `AddNuntius(services, configuration)` method.
Add `services.AddSingleton(configuration.DefaultPublishStrategy);` before the `AddTransient<IMediator>` line:

```csharp
public static IServiceCollection AddNuntius(
    this IServiceCollection services,
    NuntiusConfiguration configuration)
{
    NuntiusInitializer.Register(services, configuration);

    services.AddSingleton(configuration.DefaultPublishStrategy);

    services.AddTransient<IMediator, Mediator>();
    services.AddTransient<ISender>(sp => sp.GetRequiredService<IMediator>());
    services.AddTransient<IPublisher>(sp => sp.GetRequiredService<IMediator>());

    return services;
}
```

**Step 3: Verify the project compiles**

Run: `dotnet build`
Expected: Build succeeded

**Step 4: Update existing `MediatorTests` to pass strategy to constructor**

In `tests/Nuntius.Tests/MediatorTests.cs`, every test that creates `new Mediator(sp)` must be updated to `new Mediator(sp, SequentialStrategy.Instance)`.

The Publish tests already verify sequential-stop-on-error behavior, which `SequentialStrategy.Instance` preserves.

Update each occurrence of `new Mediator(sp)` → `new Mediator(sp, SequentialStrategy.Instance)`.

There are 6 occurrences in the file (lines 15, 29, 46, 64, 86, 119 approximately).

**Step 5: Run ALL tests**

Run: `dotnet test --verbosity quiet`
Expected: ALL tests pass (13 existing + 15 new strategy tests = 28 total)

**Step 6: Commit**

```bash
git add src/Nuntius/Mediator.cs src/Nuntius/DI/IServiceCollectionExtensions.cs tests/Nuntius.Tests/MediatorTests.cs
git commit -m "feat: wire publishing strategies into Mediator and DI

- Mediator now accepts IPublishStrategy and delegates Publish to it
- DI registration registers configured default strategy as singleton
- Existing Mediator tests updated to pass SequentialStrategy.Instance
- All existing behavior preserved (default is SequentialStrategy)

Refs #4

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 8: Add Mediator Integration Tests for Strategy Override

**Files:**
- Modify: `tests/Nuntius.Tests/MediatorTests.cs`

**Step 1: Write test for publish-time strategy override**

Add the following test to `MediatorTests.cs`:

```csharp
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
    var sut = new Mediator(sp, SequentialStrategy.Instance);
    var notification = new FakeNotification();

    // Act — override with continue-on-error strategy
    await Assert.ThrowsAsync<AggregateException>(
        () => sut.Publish(notification, SequentialContinueOnErrorStrategy.Instance).AsTask());

    // Assert — handler2 was called despite handler1 throwing (because we used continue strategy)
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
    var sut = new Mediator(sp, SequentialContinueOnErrorStrategy.Instance);
    var notification = new FakeNotification();

    // Act — no strategy override, should use the default (continue on error)
    await Assert.ThrowsAsync<AggregateException>(
        () => sut.Publish(notification).AsTask());

    // Assert — handler2 was called (continue-on-error is the default)
    await handler2.Received(1).Handle(notification, Arg.Any<CancellationToken>());
}
```

**Step 2: Run the new tests**

Run: `dotnet test tests/Nuntius.Tests --filter "FullyQualifiedName~Publish_should_use" --verbosity quiet`
Expected: PASS (2 tests)

**Step 3: Run ALL tests**

Run: `dotnet test --verbosity quiet`
Expected: ALL pass (30 total)

**Step 4: Commit**

```bash
git add tests/Nuntius.Tests/MediatorTests.cs
git commit -m "test: add integration tests for strategy override on Mediator

Verifies that Publish uses provided strategy when specified
and falls back to DI-configured default when no strategy is given.

Refs #4

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 9: Update README

**Files:**
- Modify: `README.md`

**Step 1: Add publishing strategies documentation**

Find the existing notifications section in the README (look for "Notifications" or "Publish") and add a new subsection after it documenting the publishing strategies feature.

Content to add:

```markdown
### Publishing Strategies

By default, notification handlers are executed sequentially, and if one throws an exception, the remaining handlers are not called. You can customize this behavior by configuring a publishing strategy.

#### Built-in Strategies

| Strategy | Behavior |
|----------|----------|
| `SequentialStrategy` (default) | Handlers run one-by-one. Stops on first exception. |
| `SequentialContinueOnErrorStrategy` | Handlers run one-by-one. Continues on error, throws `AggregateException` with all failures. |
| `ParallelStrategy` | Handlers run concurrently via `Task.WhenAll`. |

#### Configuring at Registration Time

```csharp
services.AddNuntius(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.DefaultPublishStrategy = ParallelStrategy.Instance;
});
```

#### Overriding at Publish Time

```csharp
await publisher.Publish(notification, SequentialContinueOnErrorStrategy.Instance);
```

#### Custom Strategies

Implement `IPublishStrategy` for custom behavior:

```csharp
public class MyCustomStrategy : IPublishStrategy
{
    public async ValueTask ExecuteAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken) where TNotification : INotification
    {
        // Your custom execution logic here
    }
}
```
```

**Step 2: Commit**

```bash
git add README.md
git commit -m "docs: add publishing strategies documentation to README

Documents built-in strategies, DI configuration, per-call override,
and custom strategy implementation.

Refs #4

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 10: Final Verification

**Step 1: Run full test suite**

Run: `dotnet test --verbosity normal`
Expected: ALL 30 tests pass

**Step 2: Build in Release mode**

Run: `dotnet build -c Release`
Expected: Build succeeded, no warnings

**Step 3: Verify git status is clean**

Run: `git status`
Expected: nothing to commit, working tree clean
