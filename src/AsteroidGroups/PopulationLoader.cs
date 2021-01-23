using System;
using System.Collections.Generic;
using KSP.Localization;
using UnityEngine;

namespace Starstrider42.CustomAsteroids
{
    /// <summary>
    /// Singleton class storing raw asteroid data.
    /// </summary>
    /// <remarks>TODO: Clean up this class.</remarks>
    internal class PopulationLoader
    {
        /// <summary>The set of loaded AsteroidSet objects.</summary>
        readonly List<AsteroidSet> asteroidPops;

        /// <summary>
        /// Creates an empty solar system. Does not throw exceptions.
        /// </summary>
        PopulationLoader ()
        {
            asteroidPops = new List<AsteroidSet> ();
        }

        /// <summary>
        /// Factory method obtaining Custom Asteroids settings from KSP config state.
        /// </summary>
        ///
        /// <returns>A newly constructed PopulationLoader object containing a full list
        /// of all valid asteroid groups in asteroid config files.</returns>
        ///
        /// <exception cref="TypeInitializationException">Thrown if the PopulationLoader
        /// object could not be constructed. The program is in a consistent state in the event of
        /// an exception.</exception>
        internal static PopulationLoader load ()
        {
            try {
                // Start with an empty population list
                PopulationLoader allPops = new PopulationLoader ();

                // Search for populations in all config files
                UrlDir.UrlConfig [] configList = GameDatabase.Instance.GetConfigs ("AsteroidSets");
                foreach (UrlDir.UrlConfig curSet in configList) {
                    foreach (ConfigNode curNode in curSet.config.nodes) {
#if DEBUG
                        Debug.Log ("[CustomAsteroids]: "
                            + Localizer.Format ("#autoLOC_CustomAsteroids_LogConfig", curNode));
#endif
                        try {
                            AsteroidSet pop = null;
                            switch (curNode.name) {
                            case "ASTEROIDGROUP":
                                pop = new Population ();
                                break;
                            case "INTERCEPT":
                                pop = new Flyby ();
                                break;
                            case "DEFAULT":
#pragma warning disable 0618 // DefaultAsteroids is deprecated
                                pop = new DefaultAsteroids ();
#pragma warning restore 0618
                                break;
                                // silently ignore any other nodes present
                            }
                            if (pop != null) {
                                ConfigNode.LoadObjectFromConfig (pop, curNode);
                                allPops.asteroidPops.Add (pop);
                            }
                        } catch (Exception e) {
                            var nodeName = curNode.GetValue ("name");
                            var error = Localizer.Format (
                                "#autoLOC_CustomAsteroids_ErrorLoadGroup", nodeName);
                            Debug.LogError ($"[CustomAsteroids]: " + error);
                            Debug.LogException (e);
                            Util.errorToPlayer (e, error);
                        }   // Attempt to parse remaining populations
                    }
                }

#if DEBUG
                foreach (AsteroidSet x in allPops.asteroidPops) {
                    Debug.Log ("[CustomAsteroids]: "
                               + Localizer.Format ("#autoLOC_CustomAsteroids_LogLoadGroup", x));
                }
#endif

                if (allPops.asteroidPops.Count == 0) {
                    Debug.LogWarning ("[CustomAsteroids]: "
                                      + Localizer.Format ("#autoLOC_CustomAsteroids_ErrorNoConfig1"));
                    ScreenMessages.PostScreenMessage (
                        Localizer.Format ("#autoLOC_CustomAsteroids_ErrorNoConfig1") + "\n"
                            + Localizer.Format ("#autoLOC_CustomAsteroids_ErrorNoConfig2"),
                        10.0f, ScreenMessageStyle.UPPER_CENTER);
                }

                return allPops;
            } catch (Exception e) {
                throw new TypeInitializationException (
                    "Starstrider42.CustomAsteroids.PopulationLoader",
                    e);
            }
        }

        /// <summary>
        /// Randomly selects an asteroid set. The selection is weighted by the spawn rate of
        /// each population; a set with a rate of 2.0 is twice as likely to be chosen as one
        /// with a rate of 1.0.
        /// </summary>
        /// <returns>A reference to the selected asteroid set. Shall not be null.</returns>
        ///
        /// <exception cref="InvalidOperationException">Thrown if there are no sets from
        /// which to choose, or if all spawn rates are zero, or if any rate is negative</exception>
        internal AsteroidSet drawAsteroidSet ()
        {
            try {
                var bins = new List<Pair<AsteroidSet, double>> ();
                foreach (AsteroidSet x in asteroidPops) {
                    bins.Add (new Pair<AsteroidSet, double> (x, x.getSpawnRate ()));
                }
                return RandomDist.weightedSample (bins);
            } catch (ArgumentException e) {
                throw new InvalidOperationException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorNoGroup"), e);
            }
        }

        /// <summary>
        /// Returns the total spawn rate of all asteroid sets. Does not throw exceptions.
        /// </summary>
        /// <returns>The sum of all spawn rates for all sets, in asteroids per day.</returns>
        /// <remarks>The rate can be affected by populations' situational modifiers, and may be zero.</remarks>
        internal double getTotalRate ()
        {
            double total = 0.0;
            foreach (AsteroidSet x in asteroidPops) {
                total += x.getSpawnRate ();
            }
            return total;
        }

        /// <summary>
        /// Debug function for traversing node tree. The indicated node and all nodes beneath it
        /// are printed, in depth-first order.
        /// </summary>
        /// <param name="node">The top-level node of the tree to be printed.</param>
        static void printNode (ConfigNode node)
        {
            Debug.Log ("printNode: NODE = " + node.name);
            foreach (ConfigNode.Value x in node.values) {
                Debug.Log ("printNode: " + x.name + " -> " + x.value);
            }
            foreach (ConfigNode x in node.nodes) {
                printNode (x);
            }
        }
    }

    /// <summary>
    /// Contains settings for asteroids that aren't affected by Custom Asteroids.
    /// </summary>
    /// <remarks>To avoid breaking the persistence code, DefaultAsteroids may not have
    /// subclasses.</remarks>
    ///
    /// @deprecated Deprecated in favor of <see cref="Flyby"/>; to be removed in version 2.0.0.
    [Obsolete ("DefaultAsteroids will be removed in 2.0.0; use Flyby instead.")]
    internal sealed class DefaultAsteroids : AsteroidSet
    {
        /// <summary>The name of the group.</summary>
        [Persistent]
        string name;
        /// <summary>The name of asteroids with unmodified orbits.</summary>
        [Persistent]
        string title;
        /// <summary>The rate, in asteroids per day, at which asteroids appear on stock
        /// orbits.</summary>
        [Persistent]
        double spawnRate;

        /// <summary>Relative ocurrence rates of asteroid classes.</summary>
        [Persistent (name = "asteroidTypes", collectionIndex = "key")]
        readonly Proportions<string> classRatios;

        /// <summary>
        /// Sets default settings for asteroids with unmodified orbits. The object is initialized
        /// to a state in which it will not be expected to generate orbits. Does not throw
        /// exceptions.
        /// </summary>
        internal DefaultAsteroids ()
        {
            name = "default";
            title = Localizer.GetStringByTag ("#autoLOC_6001923");
            spawnRate = 0.0;

            classRatios = new Proportions<string> (new [] { "1.0 PotatoRoid" });
        }

        public string drawAsteroidType ()
        {
            try {
                return AsteroidManager.drawAsteroidType (classRatios);
            } catch (InvalidOperationException e) {
                Util.errorToPlayer (e, Localizer.Format (
                    "#autoLOC_CustomAsteroids_ErrorNoClass",
                    name));
                Debug.LogException (e);
                return "PotatoRoid";
            }
        }

        /// <summary>
        /// Returns the sizeCurve used by the stock spawner as of KSP 1.0.5. This corresponds to
        /// the following size distribution: 12% class A, 13% class B, 50% class C, 13% class D,
        /// and 12% class E.
        /// </summary>
        private static readonly FloatCurve stockSizeCurve = new FloatCurve (new []
            {
                new Keyframe(0.0f, 0.0f, 1.5f, 1.5f),
                new Keyframe(0.3f, 0.45f, 0.875f, 0.875f),
                new Keyframe(0.7f, 0.55f, 0.875f, 0.875f),
                new Keyframe(1.0f, 1.0f, 1.5f, 1.5f)
            });

        public UntrackedObjectClass drawAsteroidSize ()
        {
            // Asteroids appear to be hardcoded to be from size A to E, even though there are more classes now
            return (UntrackedObjectClass)(int)
                (stockSizeCurve.Evaluate ((float) RandomDist.drawUniform (0.0, 1.0)) * 5);
        }

        /// <summary>The length of an Earth day, in seconds.</summary>
        private const double SECONDS_PER_EARTH_DAY = 24.0 * 3600.0;

        public Pair<double, double> drawTrackingTime ()
        {
            Pair<float, float> trackTimes = AsteroidManager.getOptions ().getUntrackedTimes ();
            double lifetime = RandomDist.drawUniform (trackTimes.first, trackTimes.second)
                                         * SECONDS_PER_EARTH_DAY;
            double maxLifetime = trackTimes.second * SECONDS_PER_EARTH_DAY;
            return new Pair<double, double> (lifetime, maxLifetime);
        }

        public double getSpawnRate ()
        {
            return spawnRate;
        }

        public string getName ()
        {
            return name;
        }

        public string getAsteroidName ()
        {
            return title;
        }

        public string getCometOrbit () { return "intermediate"; }

        public bool getUseCometName () { return true; }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current object.
        /// </summary>
        /// <returns>A simple string identifying the object.</returns>
        ///
        /// <seealso cref="object.ToString()"/>
        public override string ToString ()
        {
            return getName ();
        }

        /// <summary>
        /// Generates a random orbit in as similar a manner to stock as possible.
        /// </summary>
        /// <returns>The orbit of a randomly selected member of the population.</returns>
        ///
        /// <exception cref="InvalidOperationException">Thrown if cannot produce stockalike
        /// orbits. The program will be in a consistent state in the event of an exception.</exception>
        public Orbit drawOrbit ()
        {
            CelestialBody kerbin = FlightGlobals.Bodies.Find (body => body.isHomeWorld);
            CelestialBody dres = FlightGlobals.Bodies.Find (body => body.name.Equals ("Dres"));

            if (dres != null && reachedBody (dres) && RandomDist.drawUniform (0.0, 1.0) < 0.2) {
                // Drestroids
                double a = RandomDist.drawLogUniform (0.55, 0.65) * dres.sphereOfInfluence;
                double e = RandomDist.drawRayleigh (0.005);
                double i = RandomDist.drawRayleigh (0.005); // lAn takes care of negative inclinations
                double lAn = RandomDist.drawAngle ();
                double aPe = RandomDist.drawAngle ();
                double mEp = Math.PI / 180.0 * RandomDist.drawAngle ();
                double epoch = Planetarium.GetUniversalTime ();

                Debug.Log ("[CustomAsteroids]: "
                          + Localizer.Format ("#autoLOC_CustomAsteroids_LogOrbit",
                                              a, e, i, aPe, lAn, mEp, epoch));
                return new Orbit (i, e, a, lAn, aPe, mEp, epoch, dres);
            }
            if (kerbin != null) {
                // Kerbin interceptors
                double delay = RandomDist.drawUniform (12.5, 55.0);
                Debug.Log ("[CustomAsteroids]: "
                           + Localizer.Format ("#autoLOC_CustomAsteroids_LogDefault", delay));
                return Orbit.CreateRandomOrbitFlyBy (kerbin, delay);
            }
            throw new InvalidOperationException (
                Localizer.Format ("#autoLOC_CustomAsteroids_ErrorDefaultNoKerbin"));
        }

        /// <summary>
        /// Determines whether a body was already visited.
        /// </summary>
        /// <remarks>Borrowed from Kopernicus.</remarks>
        ///
        /// <param name="body">The celestial body whose exploration status needs to be tested.</param>
        /// <returns><c>true</c>, if <c>body</c> was reached, <c>false</c> otherwise.</returns>
        static bool reachedBody (CelestialBody body)
        {
            KSPAchievements.CelestialBodySubtree bodyTree =
                               ProgressTracking.Instance.GetBodyTree (body.name);
            return bodyTree != null && bodyTree.IsReached;
        }
    }
}
