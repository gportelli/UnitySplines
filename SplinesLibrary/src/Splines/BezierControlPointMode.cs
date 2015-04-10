namespace AClockworkBerry.Splines
{
    /// <summary>
    /// The type of control point.
    /// </summary>
    public enum BezierControlPointMode
    {
        /// <summary>
        /// The handles sizes and positions are free.
        /// </summary>
        Corner,

        /// <summary>
        /// The handles directions are linked, the handles sizes are free.
        /// </summary>
        Aligned,
        /// <summary>
        /// The handles directions and sizes are linked.
        /// </summary>
        Smooth
    }
}
