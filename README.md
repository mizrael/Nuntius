# Nuntius
[![Nuget](https://img.shields.io/nuget/v/Nuntius?style=plastic)](https://www.nuget.org/packages/Nuntius/)
![GitHub CI](https://github.com/mizrael/nuntius/actions/workflows/build-and-test.yml/badge.svg)
[![GitHub Issues](https://img.shields.io/github/issues/mizrael/nuntius)](https://github.com/mizrael/nuntius/issues)
[![License](https://img.shields.io/badge/license-MIT-informational)](/LICENSE.txt)

> *Nuntius* (Latin): *messenger*

**Nuntius** is a small, opinionated, open‑source C# library inspired by [MediatR](https://github.com/LuckyPennySoftware/MediatR/), designed to cover only a **minimal, carefully chosen subset** of its features.

## What Nuntius Is

* A **simple mediator / message dispatcher**
* Focused on the most common use cases
* Easy to read, debug, and extend
* Designed to feel at home in modern C# codebases

## What Nuntius Is Not

* A full MediatR replacement
* A plugin‑heavy or pipeline‑driven system

Nuntius deliberately implements **only a small subset of MediatR concepts**, and this scope is intentional and permanent.

## Usage

Register Nuntius in your DI container (typically in `Program.cs`) and let it scan for your handlers:

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddNuntius(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
});
```
### Point to point communication

Nuntius supports the use case of point-to-point communication via requests and handlers.
Two types of request + handler pairs are supported: those without a return value, and those with a return value.

You can create a request + handler pair without a return value:

```csharp
using Nuntius;

public sealed record CreateUser(string Email) : IRequest;

public sealed class CreateUserHandler : IRequestHandler<CreateUser>
{
    public ValueTask Handle(CreateUser request, CancellationToken ct)
    {
        // ...create the user...
        return ValueTask.CompletedTask;
    }
}
```

If you need a return value, you can create a request + handler pair with a response type:

```csharp
using Nuntius;

public sealed record Ping(string Message) : IRequest<string>;

public sealed class PingHandler : IRequestHandler<Ping, string>
{
    public ValueTask<string> Handle(Ping request, CancellationToken ct)
        => ValueTask.FromResult($"Pong: {request.Message}");
}
```

Finally, resolve `IMediator` and send the request:

```csharp
var provider = services.BuildServiceProvider();

var mediator = provider.GetRequiredService<IMediator>();
await mediator.Send(new CreateUser("user@example.com"));

var result = await mediator.Send(new Ping("hello"));
```

You can also send the request using the `ISender` interface:

```csharp
var provider = services.BuildServiceProvider();

var sender = provider.GetRequiredService<ISender>();
await sender.Send(new CreateUser("user@example.com"));

var result = await sender.Send(new Ping("hello"));
```

### Pub-sub communication

Nuntius supports the use case of pub-sub communication via notifications and handlers.

You can create a notification with one or more handlers:

```csharp

public sealed record UserCreated(string Email) : INotification { }

public sealed class EmailSender : INotificationHandler<UserCreated>
{
    public ValueTask Handle(UserCreated notification, CancellationToken ct)
    {
        // ...send a confirmation email...
        return ValueTask.CompletedTask;
    }
}

public sealed class UserWorkspaceCreator : INotificationHandler<UserCreated>
{
    public ValueTask Handle(UserCreated notification, CancellationToken ct)
    {
        // ...create user workspace...
        return ValueTask.CompletedTask;
    }
}

```

Finally, resolve `IMediator` and publish the notification:

```csharp
var provider = services.BuildServiceProvider();

var mediator = provider.GetRequiredService<IMediator>();
await mediator.Publish(new UserCreated("user@example.com"));

```

You can also publish the notification using the `IPublisher` interface:

```csharp
var provider = services.BuildServiceProvider();

var publisher = provider.GetRequiredService<IPublisher>();
await publisher.Publish(new UserCreated("user@example.com"));

```

## When to Use Nuntius

Nuntius is a good fit if you:

* Like the mediator pattern
* Are looking for a simple way to implement the CQRS pattern
* Want fewer abstractions
* Prefer small, understandable libraries
* Don’t need every MediatR feature

If you need advanced pipelines, behaviors, or extensive extension points, MediatR is likely the better choice.

If you need a full message bus with support for sagas and message persistence, [OpenSleigh](https://github.com/mizrael/OpenSleigh/) can be a good alternative.

