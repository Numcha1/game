using SuanPleng;
using SeasonsTogether.Core.Domain;

namespace SeasonsTogether.Core.Application;

public interface ISaveService : IAsyncDisposable
{
    string SavePath { get; }
    Farm Load();
    BuildingState LoadBuildings() => new();
    string LoadPlayerSession();
    void RequestSave(Farm state);
    void RequestSave(Farm state, BuildingState buildings) {
        if(buildings.Interiors.Count!=0)throw new NotSupportedException("Persistence adapter does not support buildings");
        RequestSave(state);
    }
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
