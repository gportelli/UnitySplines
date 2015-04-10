namespace AClockworkBerry.Splines
{
    /// <summary>
    /// Defines modes for spline walk.
    /// </summary>
    public enum SplineWalkerMode
    {
        /// <summary>
        /// Walk the path once from start to end.
        /// </summary>
        Once,

        /// <summary>
        /// Walk the path from start to end and then again from start to end in a loop.
        /// </summary>
        Loop,

        /// <summary>
        /// Cycle from start to end and then go back from end to start.
        /// </summary>
        PingPong
    }
}