# Publishing Strategies for Nuntius

**Issue:** [#4 — Introduce the concept of publishing strategy](https://github.com/mizrael/Nuntius/issues/4)

## Problem Statement

`Mediator.Publish` currently executes notification handlers sequentially, stopping on the first exception. Users have no way to:

- Execute handlers in parallel for performance
- Continue execution when a handler fails (collect errors instead of stopping)
- Provide custom execution strategies

## Design

### Core Abstraction

A single interface that decouples handler execution logic from handler resolution:

```csharp
namespace Nuntius;

public interface IPublishStrategy
{
    ValueTask ExecuteAsync<TNotification>(
        IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken) where TNotification : INotification;
}
```

**Why an interface (not a delegate):** DI-friendly, mockable, supports decorator composition (e.g., logging, retry wrappers), discoverable via IntelliSense.

**Why handlers are passed in:** The Mediator owns handler resolution from DI. Strategies are pure execution logic — they don't know about `IServiceProvider`.

### Built-in Strategies

Three sealed, stateless classes with singleton instances:

#### `SequentialStrategy` (Default)

Executes handlers one-by-one. Stops and rethrows the original exception on first failure. This preserves the current behavior exactly.

#### `SequentialContinueOnErrorStrategy`

Executes handlers one-by-one. Catches exceptions from each handler, continues with remaining handlers, then throws `AggregateException` containing all collected failures. Useful for fire-and-forget scenarios (telemetry, logging, cache invalidation).

#### `ParallelStrategy`

Fires all handlers concurrently via `Task.WhenAll`. Natural `Task.WhenAll` exception semantics apply. Useful when handlers are independent and throughput matters.

### `IPublisher` Interface Change

The `Publish` method signature changes to accept an optional strategy parameter:

```csharp
public interface IPublisher
{
    ValueTask Publish<TNotification>(
        TNotification notification,
        IPublishStrategy? strategy = null,
        CancellationToken cancellationToken = default) where TNotification : INotification;
}
```

When `strategy` is `null`, the Mediator uses the DI-configured default (which itself defaults to `SequentialStrategy`).

**Breaking change:** Callers using `Publish(notification, cancellationToken)` positionally must switch to named arguments: `Publish(notification, cancellationToken: ct)`. Acceptable for v0.x.

### Configuration

Add `DefaultPublishStrategy` to `NuntiusConfiguration`:

```csharp
public class NuntiusConfiguration
{
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;
    public IPublishStrategy DefaultPublishStrategy { get; set; } = SequentialStrategy.Instance;
    // ... existing members unchanged
}
```

Usage:

```csharp
// Default: sequential, stop on error (backward compatible)
services.AddNuntius(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<Program>());

// Global parallel
services.AddNuntius(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.DefaultPublishStrategy = ParallelStrategy.Instance;
});

// Per-call override
await publisher.Publish(notification, SequentialContinueOnErrorStrategy.Instance);
```

### `Mediator` Changes

```csharp
internal class Mediator : IMediator
{
    private readonly IServiceProvider _sp;
    private readonly IPublishStrategy _defaultStrategy;

    public Mediator(IServiceProvider sp, IPublishStrategy defaultStrategy) { ... }

    public async ValueTask Publish<TNotification>(
        TNotification notification,
        IPublishStrategy? strategy = null,
        CancellationToken cancellationToken = default) where TNotification : INotification
    {
        var effectiveStrategy = strategy ?? _defaultStrategy;
        var handlers = _sp.GetServices<INotificationHandler<TNotification>>();
        await effectiveStrategy.ExecuteAsync(handlers, notification, cancellationToken);
    }

    // Send methods unchanged
}
```

### DI Registration Changes

Register the configured default strategy as a singleton in the container:

```csharp
public static IServiceCollection AddNuntius(
    this IServiceCollection services, NuntiusConfiguration configuration)
{
    NuntiusInitializer.Register(services, configuration);
    services.AddSingleton(configuration.DefaultPublishStrategy);
    services.AddTransient<IMediator, Mediator>();
    services.AddTransient<ISender>(sp => sp.GetRequiredService<IMediator>());
    services.AddTransient<IPublisher>(sp => sp.GetRequiredService<IMediator>());
    return services;
}
```

### Exception Handling

| Strategy | Behavior |
|----------|----------|
| Sequential (stop) | Rethrows original exception as-is (preserves type and stack trace) |
| Sequential (continue) | `AggregateException` containing all handler failures |
| Parallel | `Task.WhenAll` natural semantics (`AggregateException`) |

No custom exception types. `AggregateException` is standard .NET — developers already know how to handle it.

### Custom Strategies

Users implement `IPublishStrategy` for any custom behavior:

```csharp
public class LoggingStrategy : IPublishStrategy
{
    private readonly IPublishStrategy _inner;
    private readonly ILogger _logger;

    public LoggingStrategy(IPublishStrategy inner, ILogger logger) { ... }

    public async ValueTask ExecuteAsync<TNotification>(...) where TNotification : INotification
    {
        _logger.LogInformation("Publishing {Type}", typeof(TNotification).Name);
        await _inner.ExecuteAsync(handlers, notification, cancellationToken);
    }
}
```

### File Organization

```
src/Nuntius/
├── Publishing/                              (NEW)
│   ├── IPublishStrategy.cs
│   ├── SequentialStrategy.cs
│   ├── SequentialContinueOnErrorStrategy.cs
│   └── ParallelStrategy.cs
├── DI/
│   ├── NuntiusConfiguration.cs              (MODIFIED — add DefaultPublishStrategy)
│   └── IServiceCollectionExtensions.cs      (MODIFIED — register strategy)
├── IPublisher.cs                            (MODIFIED — add strategy parameter)
├── Mediator.cs                              (MODIFIED — inject + delegate to strategy)
└── ...
```

## Design Decisions Log

| Decision | Chosen | Rejected | Reason |
|----------|--------|----------|--------|
| Abstraction type | Interface | Delegate, abstract class | DI-friendly, mockable, decorator-composable |
| Built-in count | 3 strategies | 4 (skip ParallelContinueOnError) | Serial × 2 + Parallel covers >95% of use cases; parallel continue is niche |
| Config scope | DI default + per-call | Per-notification-type, 3-tier | YAGNI — per-call override handles special cases |
| Exception type | AggregateException | Custom PublishException | Standard .NET, zero learning curve |
| IPublisher change | Single method, nullable strategy | Two overloads, extension method | Cleaner API, single method, backward compat via default null |

## Out of Scope

- Per-notification-type strategy configuration (use per-call override)
- Handler execution ordering (pub/sub handlers should be independent)
- Built-in retry / circuit breaker (use Polly in handlers)
- Strategy selection via attributes
