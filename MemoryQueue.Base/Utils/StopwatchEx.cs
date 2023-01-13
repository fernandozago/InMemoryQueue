using System.Diagnostics;

namespace MemoryQueue.Base.Utils;

public class StopwatchEx
{
    private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

    /// <summary>Gets the elapsed time since the <paramref name="startingTimestamp"/> value retrieved using <see cref="GetTimestamp"/>.</summary>
    /// <param name="startingTimestamp">The timestamp marking the beginning of the time period.</param>
    /// <returns>A <see cref="TimeSpan"/> for the elapsed time between the starting timestamp and the time of this call.</returns>
    public static TimeSpan GetElapsedTime(long startingTimestamp)
    {
        long end = Stopwatch.GetTimestamp();
        long timestampDelta = end - startingTimestamp;
        long ticks = (long)(TimestampToTicks * timestampDelta);
        return new TimeSpan(ticks);
    }
}
