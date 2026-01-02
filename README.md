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

Then create a request + handler pair (without a return value):

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

And, when you need a return value, create a request + handler pair with a response type:

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

## When to Use Nuntius

Nuntius is a good fit if you:

* Like the mediator pattern
* Are looking for a simple way to implement the CQRS pattern
* Want fewer abstractions
* Prefer small, understandable libraries
* Don’t need every MediatR feature

If you need advanced pipelines, behaviors, or extensive extension points, MediatR is likely the better choice.

If you need a full message bus with support for sagas and message persistence, [OpenSleigh](https://github.com/mizrael/OpenSleigh/) can be a good alternative.

