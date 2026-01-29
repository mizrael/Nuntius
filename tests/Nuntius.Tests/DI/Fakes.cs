namespace Nuntius.Tests.DI;

public class FakeRequest : IRequest { }

public class FakeRequestHandler : IRequestHandler<FakeRequest>
{
    public ValueTask Handle(FakeRequest request, CancellationToken cancellationToken)
    => ValueTask.CompletedTask;
}

public class FakeRequestWithResponse : IRequest<string> { }

public class FakeRequestWithResponseHandler : IRequestHandler<FakeRequestWithResponse, string>
{
    public ValueTask<string> Handle(FakeRequestWithResponse request, CancellationToken cancellationToken)
        => ValueTask.FromResult("response");
}

public class FakeNotification : INotification { }

public class FakeNotificationHandler : INotificationHandler<FakeNotification>
{
    public ValueTask Handle(FakeNotification notification, CancellationToken cancellationToken)
    => ValueTask.CompletedTask;
}