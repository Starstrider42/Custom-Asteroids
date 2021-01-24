using System;
using System.Reflection;
using KSP.Localization;
using UnityEngine;

// Is there a good way to sync version number between here, Xamarin solution, doxygen, the
// markdown files, and Git tags?
[assembly: AssemblyVersion ("1.9.0")]

namespace Starstrider42.CustomAsteroids
{
    /// <summary>
    /// Central class for controlling Custom Asteroids configuration.
    /// </summary>
    internal static class AsteroidManager
    {
        /// <summary>Singleton object responsible for handling Custom Asteroids
        /// configurations.</summary>
        static readonly PopulationLoader allowedPops;

        /// <summary>Singleton object responsible for handling alternative reference
        /// frames.</summary>
        static readonly ReferenceLoader knownFrames;

        /// <summary>Singleton object responsible for handling Custom Asteroids options.</summary>
        static readonly Options curOptions;

        /// <summary>
        /// Loads all Custom Asteroids settings. The class is in a consistent state in the event of
        /// an exception.
        /// </summary>
        static AsteroidManager ()
        {
            try {
                curOptions = Options.load ();
                allowedPops = PopulationLoader.load ();
                knownFrames = ReferenceLoader.load ();

                Debug.Log ("[CustomAsteroids]: "
                          + Localizer.Format ("#autoLOC_CustomAsteroids_LogLoadedStatus",
                                              allowedPops.getTotalRate ()));
            } catch {
                // Ensure the contents of AsteroidManager are predictable even in the event of an
                // exception. Though an exception thrown by a static constructor is basically
                // unrecoverable...
                curOptions = null;
                allowedPops = null;
                knownFrames = null;
                throw;
            }
        }

        /// <summary>
        /// Returns the current options used by Custom Asteroids. Does not throw exceptions.
        /// </summary>
        /// <returns>An Options object with the settings to use. Shall not be null.</returns>
        internal static Options getOptions ()
        {
            return curOptions;
        }

        /// <summary>
        /// Provides rate at which asteroids should be created. Does not throw exceptions.
        /// </summary>
        /// <returns>The total spawn rate, in asteroids per day, of all loaded asteroid
        /// sets.</returns>
        /// <remarks>The rate can be affected by situational modifiers, and may be zero.</remarks>
        internal static double spawnRate ()
        {
            return allowedPops.getTotalRate ();
        }

        /// <summary>
        /// Randomly selects an asteroid set. The selection is weighted by the spawn rate of each
        /// set; a set with a rate of 2.0 is twice as likely to be chosen as one with a rate of 1.0.
        /// </summary>
        /// <returns>A reference to the selected asteroid set. Shall not be null.</returns>
        ///
        /// <exception cref="InvalidOperationException">Thrown if there are no sets from
        /// which to choose, or if all spawn rates are zero, or if any rate is negative.</exception>
        internal static AsteroidSet drawAsteroidSet ()
        {
            return allowedPops.drawAsteroidSet ();
        }

        /// <summary>
        /// Returns the default reference frame for defining orbits.
        /// </summary>
        /// <returns>The default frame, or null if no default has been set.</returns>
        internal static ReferencePlane getDefaultPlane ()
        {
            return knownFrames.getReferenceSet ();
        }

        /// <summary>
        /// Counts the number of asteroids from a specific set.
        /// </summary>
        /// <param name="group">The AsteroidSet of interest.</param>
        /// <returns>The number of asteroids known to have been spawned from <c>group</c>.</returns>
        internal static int countAsteroidsInSet (AsteroidSet group)
        {
            int count = 0;
            string id = group.getName ();
            foreach (Vessel v in FlightGlobals.Vessels) {
                if (v.vesselType == VesselType.SpaceObject) {
                    AsteroidInfo info = CustomAsteroidRegistry.Instance.LookupAsteroid (v);
                    if (info != null && info.parentSet == id) {
                        count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Returns the specified reference frame.
        /// </summary>
        ///
        /// <param name="planeId">The (unique) name of the desired frame.</param>
        /// <returns>The frame with the matching <c>name</c> property, or null if no such frame
        /// exists.</returns>
        internal static ReferencePlane getReferencePlane (string planeId)
        {
            return knownFrames.getReferenceSet (planeId);
        }

        /// <summary>
        /// Randomly selects an asteroid class. The selection is weighted by the proportions passed
        /// to the method.
        /// </summary>
        ///
        /// <param name="typeRatios">The proportions in which to select the types.</param>
        /// <returns>The selected asteroid class. Shall not be null.</returns>
        ///
        /// <exception cref="InvalidOperationException">Thrown if there are no types from
        /// which to choose, or if all proportions are zero, or if any proportion is
        /// negative.</exception>
        internal static string drawAsteroidType<Dummy> (Proportions<Dummy> typeRatios)
        {
            try {
                return RandomDist.weightedSample (typeRatios.asPairList ());
            } catch (ArgumentException e) {
                throw new InvalidOperationException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorNoClass2"), e);
            }
        }

        /// <summary>
        /// Searches KSP for a celestial body. Assumes all loaded celestial bodies have unique names.
        /// </summary>
        ///
        /// <param name="name">The exact, case-sensitive name of the celestial body to recover.</param>
        /// <returns>The celestial body named <c>name</c>.</returns>
        ///
        /// <exception cref="ArgumentException">Thrown if no celestial body named <c>name</c>
        /// exists. The program state will be unchanged in the event of an exception.</exception>
        internal static CelestialBody getPlanetByName (string name)
        {
            // Would like to only calculate this once, but I don't know for sure that this object
            // will be initialized after FlightGlobals
            CelestialBody theBody = FlightGlobals.Bodies.Find (body => body.name == name);
            if (theBody == null) {
                throw new ArgumentException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorNoPlanet", name), nameof (name));
            }
            return theBody;
        }
    }
}
