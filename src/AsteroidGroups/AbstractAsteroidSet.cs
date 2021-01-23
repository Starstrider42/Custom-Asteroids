using System;
using KSP.Localization;
using UnityEngine;

namespace Starstrider42.CustomAsteroids
{
    /// <summary>
    /// Contains default implementations of methods from AsteroidSet.
    /// </summary>
    internal abstract class AbstractAsteroidSet : AsteroidSet
    {
        // Persistent fields must be directly visible to subclasses :(
        /// <summary>A unique name for the population.</summary>
        [Persistent]
        protected readonly string name;
        /// <summary>The name of asteroids belonging to this population, or a localization format
        /// string where &lt;&lt;1&gt;&gt; is a placeholder for the asteroid ID.</summary>
        [Persistent]
        protected readonly string title;
        /// <summary>The rate, in asteroids per Earth day, at which asteroids are discovered.</summary>
        [Persistent]
        protected readonly double spawnRate;
        /// <summary>The maximum number of asteroids which can exist at any given time.</summary>
        [Persistent]
        protected readonly int spawnMax;

        /// <summary>The exploration state in which these asteroids will appear. Always appear
        /// if null.</summary>
        [Persistent]
        protected readonly Condition detectable;

        /// <summary>Relative ocurrence rates of asteroid classes.</summary>
        [Persistent (name = "asteroidTypes", collectionIndex = "key")]
        protected readonly Proportions<string> classRatios;

        /// <summary>Relative ocurrence rates of asteroid sizes. Revert to default algorithm
        /// if null.</summary>
        [Persistent (name = "sizes", collectionIndex = "key")]
        protected readonly Proportions<string> sizeRatios;

        /// <summary>The orbit type to use for any comets from this set. Ignored for non-comets.</summary>
        [Persistent]
        protected readonly string orbitType;

        /// <summary>Whether to use comet-like names for any comets from this set. Objects without
        /// a ModuleComet always use asteroid-like names, regardless of this flag.</summary>
        [Persistent]
        protected readonly bool useCometName;

        /// <summary>
        /// Creates a dummy population. The object is initialized to a state in which it will not
        /// be expected to generate orbits.
        /// <para>Does not throw exceptions.</para>
        /// </summary>
        internal AbstractAsteroidSet ()
        {
            name = "invalid";
            title = Localizer.GetStringByTag ("#autoLOC_6001923");
            spawnRate = 0.0;           // Safeguard: don't make asteroids until the values are set
            spawnMax = int.MaxValue;

            detectable = null;

            classRatios = new Proportions<string> (new [] { "1.0 PotatoRoid" });

            sizeRatios = null;

            orbitType = "intermediate";
            useCometName = true;
        }

        public abstract Orbit drawOrbit ();

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
            if (sizeRatios != null)
            {
                string sizeName = RandomDist.weightedSample (sizeRatios.asPairList ());
                if (Enum.TryParse (sizeName, true, out UntrackedObjectClass size))
                {
                    return size;
                }
                else
                {
                    Util.errorToPlayerLoc ("#autoLOC_CustomAsteroids_ErrorNoSize", sizeName, name);
                    Debug.LogError ($"[CustomAsteroids]: Invalid asteroid size {sizeName} in group {name}.");
                    return UntrackedObjectClass.C;
                }
            }
            else
            {
                // Stock asteroids appear to be hardcoded to be from size A to E, even though there are more classes now
                return (UntrackedObjectClass)(int)
                    (stockSizeCurve.Evaluate ((float) RandomDist.drawUniform (0, 1)) * 5);
            }
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
            if (detectable != null && !detectable.check ()) {
                return 0.0;
            }
            if (AsteroidManager.countAsteroidsInSet (this) >= spawnMax) {
                return 0.0;
            }
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

        public string getCometOrbit () { return orbitType; }

        public bool getUseCometName () { return useCometName; }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current object.
        /// </summary>
        /// <returns>A simple string identifying the object. Defaults to the internal asteroid
        /// group name.</returns>
        ///
        /// <seealso cref="object.ToString()"/>
        public override string ToString ()
        {
            return getName ();
        }

        /// <summary>
        /// Attempts to draws a value from a distribution, providing a human-friendly error
        /// otherwise.
        /// </summary>
        /// <returns>The drawn value.</returns>
        /// <param name="property">The distribution to draw from.</param>
        /// <param name="group">The name of the asteroid set needing the value.</param>
        /// <param name="propertyName">The name of the distribution.</param>
        protected static double wrappedDraw (ValueRange property, string group, string propertyName)
        {
            try {
                return property.draw ();
            } catch (ArgumentException e) {
                throw new InvalidOperationException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorNoValue",
                                      propertyName, group),
                    e);
            }
        }
    }
}
