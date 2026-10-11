namespace SeasonsTogether.Core.Application;

public enum ConnectionState { Disconnected, Connecting, Connected, Recovering, Closed }
public sealed record RoomRequest(string Session, string Name, string Action = "sync", string Command = "", int Index = -1, string Seed = "carrot", double X = 6, double Y = 4);
public sealed record RoomSnapshot(GameReadModel State, string Message);
public interface IRoomTransport : IDisposable
{
    Task<RoomSnapshot> SendAsync(RoomRequest request, CancellationToken cancellationToken);
}
/// <summary>Future online seam: serialized authoritative snapshots; never ticks or predicts crops.</summary>
public sealed class NetworkService : IAsyncDisposable
{
    readonly IRoomTransport transport;
    readonly string session, name;
    readonly TimeSpan timeout;
    readonly SemaphoreSlim operations = new(1);
    readonly CancellationTokenSource lifetime = new();
    readonly IGameLog log;
    volatile ConnectionState state;
    RoomSnapshot? current;
    public ConnectionState State => state;
    public RoomSnapshot? Current => Volatile.Read(ref current);
    public NetworkService(IRoomTransport adapter, string playerSession, string playerName, TimeSpan? requestTimeout = null, IGameLog? logger = null)
    {
        if (!Guid.TryParseExact(playerSession,"N",out _)) throw new ArgumentException("Invalid player session");
        timeout = requestTimeout ?? TimeSpan.FromSeconds(8);
        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromSeconds(60)) throw new ArgumentOutOfRangeException(nameof(requestTimeout));
        session = playerSession; name = playerName; transport = adapter; log = logger ?? NullGameLog.Instance;
    }
    public Task<RoomSnapshot> ConnectAsync(CancellationToken ct = default) => Run(new(session,name,"join"),ct);
    public Task<RoomSnapshot> ReconnectAsync(CancellationToken ct = default) => ConnectAsync(ct);
    public Task<RoomSnapshot> SyncAsync(double x, double y, CancellationToken ct = default) => Run(new(session,name,X:x,Y:y),ct);
    public Task<RoomSnapshot> CommandAsync(GameCommand command, double x, double y, CancellationToken ct = default)
    {
        if (!GameSession.ValidAction(command.Action) || !double.IsFinite(x) || !double.IsFinite(y)) throw new ArgumentException("Invalid command");
        if ((command.Action is "plant" or "water" or "harvest" or "treat" or "fertilize" or "stake" or "clear") && command.Plot is < 0 or >= 24) throw new ArgumentException("Invalid plot");
        if (command.Action == "plant" && !Domain.GameCatalog.Crops.ContainsKey(command.Seed ?? "")) throw new ArgumentException("Invalid seed");
        string id = (command.Id == Guid.Empty ? Guid.NewGuid() : command.Id).ToString("N");
        return Run(new(session,name,command.Action,id,command.Plot,command.Seed ?? "carrot",x,y),ct);
    }
    async Task<RoomSnapshot> Send(RoomRequest request, CancellationToken ct)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct); deadline.CancelAfter(timeout);
        try { return await transport.SendAsync(request, deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new TimeoutException("Room request timed out"); }
    }
    async Task<RoomSnapshot> Run(RoomRequest request, CancellationToken ct)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct,lifetime.Token);
        await operations.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            linked.Token.ThrowIfCancellationRequested();
            state = request.Action == "join" ? ConnectionState.Connecting : state;
            RoomSnapshot snapshot;
            try { snapshot = await Send(request, linked.Token).ConfigureAwait(false); }
            catch (Exception e) when ((e is IOException or TimeoutException) && !linked.IsCancellationRequested)
            {
                state = ConnectionState.Recovering; log.Write("warning","network.retry",e.GetType().Name);
                if (request.Action != "join") _ = await Send(new(session,name,"join"), linked.Token).ConfigureAwait(false);
                // Reuse the original UUID: a lost acknowledgement cannot spend twice on the legacy server.
                snapshot = await Send(request, linked.Token).ConfigureAwait(false);
            }
            linked.Token.ThrowIfCancellationRequested();
            Volatile.Write(ref current,snapshot); state = ConnectionState.Connected; return snapshot;
        }
        catch (Exception e)
        {
            if (state != ConnectionState.Closed) state = ConnectionState.Disconnected;
            log.Write("error","network.failed",e.GetType().Name); throw;
        }
        finally { operations.Release(); }
    }
    public async ValueTask DisposeAsync()
    {
        if (state == ConnectionState.Closed) return;
        state = ConnectionState.Closed; lifetime.Cancel();
        await operations.WaitAsync().ConfigureAwait(false);
        try { transport.Dispose(); Volatile.Write(ref current,null); state = ConnectionState.Closed; }
        finally { operations.Release(); }
        // Keep cancellation/semaphore objects alive for callers already awaiting cancellation.
    }
}
