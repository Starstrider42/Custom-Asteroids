namespace Starstrider42.CustomAsteroids
{
    /// <summary>
    /// Represents a reference frame with respect to which orbits may be defined.
    /// </summary>
    internal interface ReferencePlane
    {
        /// <summary>
        /// A unique identifier for the frame.
        /// </summary>
        string name { get; }

        /// <summary>
        /// Transforms a vector given with respect to this frame to a vector with respect to the
        /// KSP frame. Shall not throw exceptions.
        /// </summary>
        ///
        /// <param name="inFrame">A vector defined in the frame represented by this object.</param>
        /// <returns>An equivalent vector in a form useful by the KSP API.</returns>
        Vector3d toDefaultFrame (Vector3d inFrame);
    }
}
