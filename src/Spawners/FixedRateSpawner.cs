using System;
using KSP.Localization;
using UnityEngine;

namespace Starstrider42.CustomAsteroids
{
    internal sealed class FixedRateSpawner : AbstractSpawner
    {
        /// <summary>The time (UT) of the last spawn check.</summary>
        private double lastCheck;

        /// <summary>
        /// Initializes this spawner's state.
        /// </summary>
        internal FixedRateSpawner ()
        {
            lastCheck = Planetarium.GetUniversalTime ();
        }

        /// <summary>
        /// Returns the probability of detecting an asteroid in one tick. Does not throw exceptions.
        /// </summary>
        /// <param name="tickLength">The number of seconds in the current tick.</param>
        /// <returns>The asteroid discovery probability, scaled for the current time warp.</returns>
        private static float asteroidChance (double tickLength)
        {
            if (tickLength <= 0.0) {
                return 0.0f;
            }
            double rate = AsteroidManager.spawnRate () / SECONDS_PER_EARTH_DAY; // asteroids per second
            if (rate > 0.0) {
                float expected = (float)(rate * tickLength);
                return Mathf.Max(0.0f, Mathf.Min(1.0f - Mathf.Exp(-expected), 1.0f));
            } else {
                return 0.0f;     // Avoid small nonzero probability if rate == 0.0
            }
        }

        protected override float checkInterval ()
        {
            // If player suddenly jumps from 1× to 100,000×, each second of interval will
            // delay asteroid check by 1.16 days
            return 5.0f;
        }

        protected override void checkSpawn ()
        {
            double ut = Planetarium.GetUniversalTime ();
            try {
                // Cap at one asteroid per tick; this is unlikely to reduce the rate even at 100,000×
                if (areAsteroidsTrackable () && RandomDist.drawUniform (0, 1) < asteroidChance(ut - lastCheck)) {
                    Debug.Log ("[FixedRateSpawner]: "
                              + Localizer.Format ("#autoLOC_CustomAsteroids_LogSpawnInterval",
                                                  ut));
                    spawnAsteroid ();
                }
            } finally {
                lastCheck = ut;
            }
        }
    }
}
