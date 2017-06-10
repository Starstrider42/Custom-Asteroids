using KSP.Localization;
using UnityEngine;

namespace Starstrider42.CustomAsteroids {
	internal sealed class FixedRateSpawner : AbstractSpawner {
		/// <summary>The time at which the next asteroid will be placed.</summary>
		private double nextAsteroid;

		/// <summary>
		/// Initializes this spawner's state.
		/// </summary>
		internal FixedRateSpawner() {
			// Yay for memoryless distributions -- we don't care how long it's been since an asteroid was detected
			resetAsteroidSearches();
		}

		/// <summary>
		/// Forgets this spawner's previous state, recalculating when the next asteroid will be detected.
		/// </summary>
		private void resetAsteroidSearches() {
			nextAsteroid = Planetarium.GetUniversalTime() + asteroidWaitTime();
		}

		/// <summary>
		/// Returns the time until the next asteroid should be detected. Does not throw exceptions.
		/// </summary>
		/// <returns>The number of seconds before an asteroid detection, or infinity if asteroids should not 
		/// spawn.</returns>
		private static double asteroidWaitTime() {
			double rate = AsteroidManager.spawnRate();	// asteroids per day

			if (rate > 0.0) {
				rate /= SECONDS_PER_EARTH_DAY;			// asteroids per second
				// Waiting time in a Poisson process follows an exponential distribution
				return RandomDist.drawExponential(1.0 / rate);
			} else {
				return double.PositiveInfinity;
			}
		}

		protected override float checkInterval() {
			// If player suddenly jumps from 1× to 100,000×, each second of interval will 
			// delay asteroid check by 1.16 days
			return 5.0f;
		}

		protected override void checkSpawn() {
			// More than one asteroid per tick is unlikely even at 100,000×
			while (Planetarium.GetUniversalTime() > nextAsteroid) {
				if (areAsteroidsTrackable()) {
					Debug.Log("[FixedRateSpawner]: "
							  + Localizer.Format ("#autoLOC_CustomAsteroids_LogSpawnInterval", nextAsteroid));
					spawnAsteroid();
				}

				nextAsteroid += asteroidWaitTime();
			}
		}
	}
}

