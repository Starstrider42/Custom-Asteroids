namespace Starstrider42.CustomAsteroids
{
    /// <summary>
    /// Represents any collection of asteroids from which asteroids may be generated. Each
    /// implementation of <c>AsteroidSet</c> corresponds to one of the statements in an
    /// <c>AsteroidSets</c> configuration block.
    /// </summary>
    internal interface AsteroidSet
    {
        /// <summary>
        /// Returns the rate at which asteroids of this type are discovered. Does not throw
        /// exceptions.
        /// </summary>
        /// <returns>The number of asteroids discovered per Earth day.</returns>
        /// <remarks>The rate can be affected by situational modifiers, and may be zero.</remarks>
        double getSpawnRate ();

        /// <summary>
        /// Returns the name of the type. Does not throw exceptions.
        /// </summary>
        /// <returns>A string identifying the population. Must be a unique machine-readable word
        /// (e.g., "kspAsteroids").</returns>
        string getName ();

        /// <summary>
        /// Returns the name of asteroids of this type. Does not throw exceptions.
        /// </summary>
        /// <returns>A human-readable string that can be used as an asteroid prefix. No format
        /// restrictions, but should be shorter than 20 characters for best readability.</returns>
        string getAsteroidName ();

        /// <summary>
        /// Returns the comet orbit to use for any comets of this type. Does not throw exceptions.
        /// </summary>
        /// <returns>A string identifying the <c>COMET_ORBIT_TYPE</c> block to use. The name may be unvalidated.</returns>
        string getCometOrbit ();

        /// <summary>
        /// Returns whether any comets of this type should have comet-like names. Does not throw exceptions.
        /// </summary>
        /// <returns><c>true</c> if comet-like names should be used,
        /// <c>false</c> if asteroid-like names should be used.</returns>
        bool getUseCometName ();

        /// <summary>
        /// Generates a random orbit appropriate for this type.
        /// </summary>
        /// <returns>The orbit of a randomly selected member of the asteroid type.</returns>
        ///
        /// <exception cref="System.InvalidOperationException">Thrown if the implementing object
        /// cannot produce valid orbits. The program will be in a consistent state in the event of
        /// an exception.</exception>
        Orbit drawOrbit ();

        /// <summary>
        /// Generates a random asteroid class appropriate for this type.
        /// </summary>
        ///
        /// <returns>Name of a part suitable for use as an asteroid.</returns>
        ///
        /// <exception cref="System.InvalidOperationException">Thrown if the implementing object
        /// cannot produce valid types. The program will be in a consistent state in the event of
        /// an exception.</exception>
        string drawAsteroidType ();

        /// <summary>
        /// Generates a random asteroid size appropriate for this type.
        /// </summary>
        /// <returns>A standard KSP size class.</returns>
        ///
        /// <exception cref="System.InvalidOperationException">Thrown if the implementing object
        /// cannot produce valid sizes. The program will be in a consistent state in the event of
        /// an exception.</exception>
        UntrackedObjectClass drawAsteroidSize ();

        /// <summary>
        /// Generates a random tracking lifetime appropriate for this type.
        /// </summary>
        /// <returns>The time left until the asteroid is lost and the time represented by a full
        /// "Signal Strength" bar, both in seconds.</returns>
        ///
        /// <exception cref="System.InvalidOperationException">Thrown if the implementing object
        /// cannot produce valid lifetimes. The program will be in a consistent state in the event of
        /// an exception.</exception>
        System.Tuple<double, double> drawTrackingTime ();
    }
}
