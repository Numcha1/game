using System.Diagnostics;
namespace SeasonsTogether;
/// <summary>Bounded main-thread samples; diagnostics controls sampling windows.</summary>
public sealed class FrameTimeMonitor
{
    public List<double> Samples { get; } = new(1800);
    long previous;
    public void Record()
    {
        long now = Stopwatch.GetTimestamp();
        if (previous != 0 && Samples.Count < 3600) Samples.Add(Stopwatch.GetElapsedTime(previous,now).TotalMilliseconds);
        previous = now;
    }
}
