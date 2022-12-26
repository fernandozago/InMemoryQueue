using System;
using System.Diagnostics;

namespace MemoryQueue.Base.Utils;

public class StopwatchEx
{

    /// <summary>Gets the elapsed time since the <paramref name="startingTimestamp"/> value retrieved using <see cref="GetTimestamp"/>.</summary>
    /// <param name="startingTimestamp">The timestamp marking the beginning of the time period.</param>
    /// <returns>A <see cref="TimeSpan"/> for the elapsed time between the starting timestamp and the time of this call.</returns>
    public static TimeSpan GetElapsedTime(long startingTimestamp) =>
        GetElapsedTime(startingTimestamp, GetTimestamp());

    public static long GetTimestamp() =>
        Stopwatch.GetTimestamp();

    /// <summary>Gets the elapsed time between two timestamps retrieved using <see cref="GetTimestamp"/>.</summary>
    /// <param name="startingTimestamp">The timestamp marking the beginning of the time period.</param>
    /// <param name="endingTimestamp">The timestamp marking the end of the time period.</param>
    /// <returns>A <see cref="TimeSpan"/> for the elapsed time between the starting and ending timestamps.</returns>
    private static TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp)
    {
        return new(endingTimestamp - startingTimestamp);
    }


}
