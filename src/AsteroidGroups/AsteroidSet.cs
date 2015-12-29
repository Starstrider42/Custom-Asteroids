namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// Represents any collection of asteroids from which asteroids may be generated. Each implementation of 
	/// <c>AsteroidSet</c> corresponds to one of the statements in an <c>AsteroidSets</c> configuration block.
	/// </summary>
	internal interface AsteroidSet {
		/// <summary>
		/// Returns the rate at which asteroids of this type are discovered. Does not throw exceptions.
		/// </summary>
		/// <returns>The number of asteroids discovered per Earth day.</returns>
		double getSpawnRate();

		/// <summary>
		/// Returns the name of the type. Does not throw exceptions.
		/// </summary>
		/// <returns>A string identifying the population. Must be a unique machine-readable word 
		/// (e.g., "kspAsteroids").</returns>
		string getName();

		/// <summary>
		/// Returns the name of asteroids of this type. Does not throw exceptions.
		/// </summary>
		/// <returns>A human-readable string that can be used as an asteroid prefix. No format restrictions, but 
		/// should be shorter than 20 characters for best readability.</returns>
		string getAsteroidName();

		/// <summary>
		/// Generates a random orbit appropriate for this type.
		/// </summary>
		/// <returns>The orbit of a randomly selected member of the asteroid type.</returns>
		/// 
		/// <exception cref="System.InvalidOperationException">Thrown if the implementing object cannot produce 
		/// valid orbits. The program will be in a consistent state in the event of an exception.</exception>
		Orbit drawOrbit();
	}
}
