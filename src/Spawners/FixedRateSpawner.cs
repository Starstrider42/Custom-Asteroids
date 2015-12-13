using System;
using UnityEngine;

namespace Starstrider42.CustomAsteroids {
	internal sealed class FixedRateSpawner : AbstractSpawner {
		/**
		 * Initializes this spawner's state.
		 */
		internal FixedRateSpawner() : base() {
			// Yay for memoryless distributions -- we don't care how long it's been since an asteroid was detected
			resetAsteroidSearches();
		}

		/**
		 * Forgets this spawner's previous state, recalculating when the next asteroid will be detected.
		 */
		private void resetAsteroidSearches() {
			nextAsteroid = Planetarium.GetUniversalTime() + waitForAsteroid();
		}

		/** Returns the time until the next asteroid should be detected
		 * 
		 * @return The number of seconds before an asteroid detection, or infinity if asteroids should not spawn.
		 * 
		 * @exceptsafe Does not throw exceptions
		 */
		private static double waitForAsteroid() {
			double rate = AsteroidManager.spawnRate();	// asteroids per day

			if (rate > 0.0) {
				rate /= SECONDS_PER_EARTH_DAY;			// asteroids per second
				// Waiting time in a Poisson process follows an exponential distribution
				return RandomDist.drawExponential(1.0/rate);
			} else {
				return double.PositiveInfinity;
			}
		}

		/**
		 * The interval, in in-game seconds, at which the spawner checks for asteroid creation or deletion.
		 * 
		 * @return number of KSP seconds between consecutive spawn/despawn checks
		 */
		protected override float checkInterval() {
			// If player suddenly jumps from 1× to 100,000×, each second of interval will 
			// delay asteroid check by 1.16 days
			return 5.0f;
		}

		protected override void checkSpawn() {
			if(areAsteroidsTrackable()) {
				// More than one asteroid per tick is unlikely even at 100,000×
				while (Planetarium.GetUniversalTime() > nextAsteroid) {
					Debug.Log("[FixedRateSpawner]: asteroid discovered at UT " + nextAsteroid);
					spawnAsteroid();

					nextAsteroid += waitForAsteroid();
				}
			}
		}

		/** The time at which the next asteroid will be placed */
		private double nextAsteroid;
	}
}

