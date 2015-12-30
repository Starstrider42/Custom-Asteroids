namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// Represents a physical type of an asteroid. Each implementation of <c>AsteroidType</c> corresponds to one 
	/// of the statements in a <c>CustomAsteroidTypes</c> configuration block.
	/// </summary>
	internal interface AsteroidType {
		/// <summary>
		/// Returns the name of the type. Does not throw exceptions.
		/// </summary>
		/// <returns>A string identifying the type. Must be a unique machine-readable word 
		/// (e.g., "asteroid_rocky").</returns>
		string getName();

		/// <summary>
		/// Creates a <c>CustomAsteroidData</c> module representing a randomly selected asteroid of this type.
		/// </summary>
		/// 
		/// <returns>A PartModule containing data for this asteroid type.</returns>
		/// 
		/// <exception cref="System.InvalidOperationException">Thrown if the implementing object cannot produce 
		/// valid data. The program will be in a consistent state in the event of an exception.</exception>
		CustomAsteroidData drawAsteroidData();

		/// <summary>
		/// Creates a ConfigNode representing the result of <see cref="drawAsteroidData()"/>.
		/// </summary>
		/// 
		/// <returns>A ConfigNode that can be used to create a CustomAsteroidData.</returns>
		/// 
		/// <exception cref="System.InvalidOperationException">Thrown if the implementing object cannot produce 
		/// valid data. The program will be in a consistent state in the event of an exception.</exception>
		ConfigNode packedAsteroidData();
	}
}
