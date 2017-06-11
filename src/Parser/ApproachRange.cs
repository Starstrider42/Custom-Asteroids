namespace Starstrider42.CustomAsteroids
{
    /// <summary>
    /// Specialization of ValueRange for incoming asteroid approach distance.
    /// </summary>
    ///
    /// TODO: I don't think that ApproachRange is a subtype of ValueRange in the Liskov sense... check!
    internal class ApproachRange : ValueRange
    {
        /// <summary>The type of parameter describing the orbit.</summary>
        [Persistent]
        Type type;

        /// <summary>
        /// Assigns situation-specific default values to the ApproachRange. The given values will
        /// be used by draw() unless they are specifically overridden by a ConfigNode. Does not
        /// throw exceptions.
        /// </summary>
        /// <param name="dist">The distribution from which the value will be drawn.</param>
        /// <param name="type">The description of approach distance that is used.</param>
        /// <param name="min">The minimum value allowed for distributions. May be unused.</param>
        /// <param name="max">The maximum value allowed for distributions. May be unused.</param>
        /// <param name="avg">The mean value returned. May be unused.</param>
        /// <param name="stddev">The standard deviation of values returned. May be unused.</param>
        internal ApproachRange (Distribution dist, Type type = Type.ImpactParameter,
            double min = 0.0, double max = 1.0, double avg = 0.0, double stddev = 0.0)
            : base (dist, min, max, avg, stddev)
        {
            this.type = type;
        }

        /// <summary>
        /// Returns the parametrization used by this ValueRange. Does not throw exceptions.
        /// </summary>
        /// <returns>The approach distance parameter represented by this object.</returns>
        internal Type getParam ()
        {
            return type;
        }

        /// <summary>Defines the parametrization of approach distance that is used.</summary>
        internal enum Type
        {
            ImpactParameter,
            Periapsis
        }
    }
}
