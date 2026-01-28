namespace Nuntius;

/// <summary>
/// A mediator which is capable of doing both point to point and pub-sub communication patterns.
/// </summary>
public interface IMediator : ISender, IPublisher
{
}
