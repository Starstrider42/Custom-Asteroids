namespace Starstrider42.CustomAsteroids {

	/// <summary>
	/// Specialization of ValueRange for orbital phase parameter.
	/// </summary>
	/// 
	/// TODO: I don't think that PhaseRange is a subtype of ValueRange in the Liskov sense... check!
	internal class PhaseRange : ValueRange {
		/// <summary>The type of parameter describing the orbit.</summary>
		[Persistent] private PhaseType type;
		/// <summary>The time at which the parameter should be calculated.</summary>
		[Persistent] private EpochType epoch;

		/// <summary>
		/// Assigns situation-specific default values to the ValueRange. The given values will be used by draw() 
		/// unless they are specifically overridden by a ConfigNode. Does not throw exceptions.
		/// </summary>
		/// <param name="dist">The distribution from which the value will be drawn.</param>
		/// <param name="type">The description of orbit position that is used.</param>
		/// <param name="epoch">The time at which the orbit position should be measured.</param>
		/// <param name="min">The minimum value allowed for distributions. May be unused.</param>
		/// <param name="max">The maximum value allowed for distributions. May be unused.</param>
		/// <param name="avg">The mean value returned. May be unused.</param>
		/// <param name="stddev">The standard deviation of values returned. May be unused.</param>
		internal PhaseRange(Distribution dist, 
			PhaseType type = PhaseType.MeanAnomaly, EpochType epoch = EpochType.GameStart, 
			double min = 0.0, double max = 1.0, double avg = 0.0, double stddev = 0.0)
			: base(dist, min, max, avg, stddev) {
			this.type = type;
			this.epoch = epoch;
		}

		/// <summary>
		/// Returns the parametrization used by this ValueRange. Does not throw exceptions.
		/// </summary>
		/// <returns>The orbit position parameter represented by this object.</returns>
		internal PhaseType getParam() {
			return type;
		}

		/// <summary>
		/// Returns the epoch at which the phase is evaluated. Does not throw exceptions.
		/// </summary>
		/// <returns>The epoch at which the orbital position is specified.</returns>
		internal EpochType getEpoch() {
			return epoch;
		}

		/// <summary>Defines the parametrization of orbit phase that is used.</summary>
		internal enum PhaseType {
			MeanLongitude,
			MeanAnomaly
		}

		/// <summary>Defines the time at which the phase is measured.</summary>
		internal enum EpochType {
			GameStart,
			Now
		}
	}
}