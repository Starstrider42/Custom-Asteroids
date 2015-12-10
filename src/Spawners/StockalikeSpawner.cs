using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Starstrider42.CustomAsteroids {
	internal sealed class StockalikeSpawner : AbstractSpawner {
		/**
		 * Initializes this spawner's state.
		 */
		internal StockalikeSpawner() : base() {
		}

		/**
		 * Emulation of stock spawning behaviour, as best as I've been able to reconstruct it.
		 */
		protected override void checkSpawn() {
			if (areAsteroidsTrackable() 
					&& countUntrackedAsteroids() < Random.Range(spawnGroupMinLimit, spawnGroupMaxLimit)) {
				if (Random.Range(0.0f, 1.0f) < 1.0f / (1.0f + spawnOddsAgainst)) {
					spawnAsteroid();
				} else {
					Debug.Log("[StockalikeSpawner]: No new objects this time. " 
						+ "(Odds are 1:" + spawnOddsAgainst +")");
				}
			}
		}

		private int countUntrackedAsteroids() {
			int count = 0;
			foreach (Vessel v in FlightGlobals.Vessels) {
				DiscoveryInfo trackState = v.DiscoveryInfo;
				// This test will fail if and only if v is an unvisited, untracked asteroid
				// It does not matter whether or not it was tracked in the past
				if (trackState != null && !trackState.HaveKnowledgeAbout(DiscoveryLevels.StateVectors)) {
					count++;
				}
			}
			return count;
		}

		// Public fields from ScenarioDiscoverableObjects
		private const float spawnInterval = 15.0f;
		private const int spawnOddsAgainst = 2;
		private const int spawnGroupMinLimit = 3;
		private const int spawnGroupMaxLimit = 8;
	}
}
