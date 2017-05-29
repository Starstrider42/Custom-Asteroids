using System;
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
        /// <summary>The name of asteroids belonging to this population.</summary>
        [Persistent]
        protected readonly string title;
        /// <summary>The rate, in asteroids per Earth day, at which asteroids are discovered.</summary>
        [Persistent]
        protected readonly double spawnRate;

        /// <summary>The exploration state in which these asteroids will appear. Always appear if null.</summary>
        [Persistent]
        protected readonly Condition detectable;

        /// <summary>Relative ocurrence rates of asteroid classes.</summary>
        [Persistent (name = "asteroidTypes", collectionIndex = "key")]
        protected readonly Proportions<string> classRatios;

        /// <summary>
        /// Creates a dummy population. The object is initialized to a state in which it will not be expected to
        /// generate orbits.
        /// <para>Does not throw exceptions.</para>
        /// </summary>
        internal AbstractAsteroidSet ()
        {
            name = "invalid";
            title = "Ast.";
            spawnRate = 0.0;           // Safeguard: don't make asteroids until the values are set

            detectable = null;

            classRatios = new Proportions<string> (new [] { "1.0 PotatoRoid" });
        }

        public abstract Orbit drawOrbit ();

        public string drawAsteroidType ()
        {
            try {
                return AsteroidManager.drawAsteroidType (classRatios);
            } catch (InvalidOperationException e) {
                Util.errorToPlayer (e, $"Could not select asteroid class for group '{name}'.");
                Debug.LogException (e);
                return "PotatoRoid";
            }
        }

        public double getSpawnRate ()
        {
            if (detectable == null || detectable.check ()) {
                return spawnRate;
            }
            return 0.0;
        }

        public string getName ()
        {
            return name;
        }

        public string getAsteroidName ()
        {
            return title;
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current object.
        /// </summary>
        /// <returns>A simple string identifying the object. Defaults to the internal asteroid group name.</returns>
        ///
        /// <seealso cref="object.ToString()"/>
        public override string ToString ()
        {
            return getName ();
        }

        /// <summary>
        /// Attempts to draws a value from a distribution, providing a human-friendly error otherwise.
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
                    $"Could not set property '{propertyName}' for group '{group}'.", e);
            }
        }
    }
}
