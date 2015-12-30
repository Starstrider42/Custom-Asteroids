namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// Represents a class of identical asteroids, with no individual variation.
	/// </summary>
	internal class FixedClass : AsteroidType {
		/// <summary>A unique identifier for this type.</summary>
		[Persistent] private readonly string name;

		/// <summary>The name of the composition class to use.</summary>
		[Persistent] private readonly string title;

		/// <summary>Density, in tons/m^3</summary>
		[Persistent] private readonly float density;

		/// <summary>Asteroid sampling experiment.</summary>
		[Persistent] private readonly string sampleExperimentId;

		/// <summary>Fraction of science recovered by transmitting back to Kerbin.</summary>
		[Persistent] private readonly float sampleExperimentXmitScalar;

		/// <summary>Creates an <c>AsteroidType</c> that produces stockalike asteroids.</summary>
		internal FixedClass() {
			this.name = "invalid";
			this.title = "Stony";

			// Defaults for stock ModuleAsteroid
			this.density = 0.03f;
			this.sampleExperimentId = "asteroidSample";
			this.sampleExperimentXmitScalar = 0.3f;
		}

		public string getName() {
			return name;
		}

		public override string ToString() {
			return name;
		}

		public CustomAsteroidData drawAsteroidData() {
			var data = new CustomAsteroidData();

			data.composition = title;
			data.density = density;
			data.sampleExperimentId = sampleExperimentId;
			data.sampleExperimentXmitScalar = sampleExperimentXmitScalar;

			return data;
		}

		public ConfigNode packedAsteroidData() {
			return drawAsteroidData().toProtoConfigNode();
		}
	}
}
