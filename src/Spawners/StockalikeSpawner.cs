using KSP.Localization;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Starstrider42.CustomAsteroids
{
    internal sealed class StockalikeSpawner : AbstractSpawner
    {
        // <remarks>Public fields from ScenarioDiscoverableObjects.</remarks>
        /// <summary>The number of in-game seconds between asteroid checks.</summary>
        private const float SPAWN_INTERVAL = 15.0f;
        /// <summary>
        /// Controls the fraction of spawn checks in which new asteroids are generated. The
        /// probability of spawning an asteroid is 1 / (1 + <c>spawnOddsAgainst</c>).
        /// </summary>
        private const int SPAWN_ODDS_AGAINST = 2;
        /// <summary>Number of untracked asteroids at which spawn rate begins to slow.</summary>
        private const int SPAWN_GROUP_MIN_LIMIT = 3;
        /// <summary>Number of untracked asteroids at which spawn rate stops completely.</summary>
        private const int SPAWN_GROUP_MAX_LIMIT = 8;

        /// <summary>
        /// Initializes this spawner's state.
        /// </summary>
        internal StockalikeSpawner ()
        {
        }

        protected override float checkInterval ()
        {
            return SPAWN_INTERVAL;
        }

        /// <summary>
        /// Uses criteria similar to ScenarioDiscoverableObjects to decide whether to create an
        /// asteroid.
        /// </summary>
        protected override void checkSpawn ()
        {
            if (areAsteroidsTrackable ()
                    && countUntrackedAsteroids () < Random.Range (SPAWN_GROUP_MIN_LIMIT,
                                                                  SPAWN_GROUP_MAX_LIMIT)) {
                if (Random.Range (0.0f, 1.0f) < 1.0f / (1.0f + SPAWN_ODDS_AGAINST)) {
                    spawnAsteroid ();
                } else {
                    Debug.Log ("[StockalikeSpawner]: "
                              + Localizer.Format ("#autoLOC_CustomAsteroids_LogSpawnStock",
                                                  SPAWN_ODDS_AGAINST));
                }
            }
        }

        /// <summary>
        /// Counts the untracked asteroids currently in the game. Does not throw exceptions.
        /// </summary>
        /// <returns>The number of untracked asteroids.</returns>
        private static int countUntrackedAsteroids ()
        {
            int count = 0;
            foreach (Vessel v in FlightGlobals.Vessels) {
                DiscoveryInfo trackState = v.DiscoveryInfo;
                // This test will fail if and only if v is an unvisited, untracked asteroid
                // It does not matter whether or not it was tracked in the past
                if (trackState != null
                        && !trackState.HaveKnowledgeAbout (DiscoveryLevels.StateVectors)) {
                    count++;
                }
            }
            return count;
        }
    }
}
