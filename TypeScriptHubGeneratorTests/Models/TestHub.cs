using Microsoft.AspNetCore.SignalR;

namespace TypeScriptHubGeneratorTests;

public enum EventType
{
    SomeEvent,
    SomeOtherEvent,
}

public record SomeObjectModel
{
    // todo more...
    public required bool SomeBoolean { get; init; }
    public required bool? SomeNullableBoolean { get; init; }
    public required string SomeString { get; init; }
    public required string? SomeNullableString { get; init; }
    public required int SomeInt { get; init; }
    public required int? SomeNullableInt { get; init; }
    public required DateTime SomeDateTime { get; init; }
    public required DateTime? SomeNullableDateTime { get; init; }
    public required EventType SomeEvent { get; init; }
}

public record EventModel(EventType EventType, string Message);

public interface ITestHub
{
    Task Pong();

    Task SomethingHappened(string? message);

    Task SomethingHappenedNullable(string? message);

    Task SomethingHappenedModel(EventType someEvent);

    Task SomethingHappenedModelList(List<EventType> someEventsList);
}

public class TestHub : Hub<ITestHub>
{
    public async Task<SomeObjectModel> DoStuffObject(SomeObjectModel someObject)
    {
        await Task.CompletedTask;
        return someObject;
    }

    public async Task<SomeObjectModel?> DoStuffObjectNullable(SomeObjectModel? someObject)
    {
        await Task.CompletedTask;
        return someObject;
    }


    public async Task<int?> DoStuffNullableInt(int? number, int otherNumber)
    {
        var result = number + otherNumber;
        await Clients.Caller.SomethingHappened(result.ToString());
        return result;
    }

    public async Task<EventType?> DoStuffWithEnum(EventType? someNullableEnum, EventType someEnum)
    {
        await Task.CompletedTask;
        return null;
    }
}