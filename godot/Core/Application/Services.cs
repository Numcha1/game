using SuanPleng;

namespace SeasonsTogether.Core.Application;

public interface ISaveService : IAsyncDisposable
{
    string SavePath { get; }
    Farm Load();
    string LoadPlayerSession();
    void RequestSave(Farm state);
    Task FlushAsync(CancellationToken cancellationToken = default);
    bool TryReadFailure(out string error);
}

public interface IGameLog
{
    void Write(string level, string code, string? exceptionType = null);
}

public sealed class NullGameLog : IGameLog
{
    public static readonly NullGameLog Instance = new();
    public void Write(string level, string code, string? exceptionType = null) { }
}
