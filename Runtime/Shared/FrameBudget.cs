using System.Diagnostics;

namespace DungeonGen
{
    /// <summary>Time slice used to spread work over several frames.</summary>
    public sealed class FrameBudget
    {
        private readonly double _milliseconds;
        private readonly Stopwatch _stopwatch = new();

        public FrameBudget(double millisecondsPerFrame) => _milliseconds = millisecondsPerFrame;

        /// <summary>A budget that is never exceeded. Use it to run a routine to completion in one go.</summary>
        public static FrameBudget Unlimited => new FrameBudget(double.PositiveInfinity);

        public bool Exceeded => _stopwatch.Elapsed.TotalMilliseconds >= _milliseconds;

        public void Begin() => _stopwatch.Restart();
    }
}
