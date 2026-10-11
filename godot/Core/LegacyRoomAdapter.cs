using SuanPleng;

namespace SeasonsTogether.Core;

/// <summary>Transport seam for a later online scene. Never ticks or predicts authoritative crops.</summary>
public sealed class LegacyRoomAdapter : IDisposable
{
    readonly RoomClient client;
    readonly string session;
    public LegacyRoomAdapter(string address, string bearerToken, string playerSession)
    {
        if (!Guid.TryParseExact(playerSession, "N", out _)) throw new ArgumentException("Invalid player session", nameof(playerSession));
        session = playerSession; client = new RoomClient(address, bearerToken);
    }
    public Task<Reply> Join(string name) => client.Send(new Request { Session = session, Name = name, Action = "join" });
    public Task<Reply> Sync(double x, double y) => client.Send(new Request { Session = session, X = x, Y = y });
    public Task<Reply> Command(string action, double x, double y, int index, string seed, string commandId)
        => client.Send(new Request { Session = session, Action = action, X = x, Y = y, Index = index, Seed = seed, Command = commandId });
    public void Dispose() => client.Dispose();
}
