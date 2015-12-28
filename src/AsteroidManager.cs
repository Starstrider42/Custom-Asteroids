using System;
using System.Reflection;
using UnityEngine;

// Is there a good way to sync version number between here, doxygen.cfg, the markdown source, and Git tags?
[assembly:AssemblyVersion("1.2.0")]

namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// Central class for controlling Custom Asteroids configuration.
	/// </summary>
	static class AsteroidManager {
		/// <summary>Singleton object responsible for handling Custom Asteroids configurations.</summary>
		private static readonly PopulationLoader allowedPops;

		/// <summary>Singleton object responsible for handling Custom Asteroids options.</summary>
		private static readonly Options curOptions;

		/// <summary>
		/// Loads all Custom Asteroids settings. The class is in a consistent state in the event of an exception.
		/// </summary>
		static AsteroidManager() {
			try {
				curOptions = Options.load();
				allowedPops = PopulationLoader.load();

				Debug.Log("[CustomAsteroids]: " + allowedPops.getTotalRate() + " new discoveries per Earth day.");
			} catch {
				// Ensure the contents of AsteroidManager are predictable even in the event of an exception
				// Though an exception thrown by a static constructor is basically unrecoverable...
				curOptions = null;
				allowedPops = null;
				throw;
			}
		}

		/// <summary>
		/// Returns the current options used by Custom Asteroids. Does not throw exceptions.
		/// </summary>
		/// <returns>An Options object with the settings to use. Shall not be null.</returns>
		internal static Options getOptions() {
			return curOptions;
		}

		/// <summary>
		/// Provides rate at which asteroids should be created. Does not throw exceptions.
		/// </summary>
		/// <returns>The total spawn rate, in asteroids per day, of all loaded asteroid sets.</returns>
		internal static double spawnRate() {
			return allowedPops.getTotalRate();
		}

		/// <summary>
		/// Randomly selects an asteroid set. The selection is weighted by the spawn rate of each set; a set with 
		/// a rate of 2.0 is twice as likely to be chosen as one with a rate of 1.0.
		/// </summary>
		/// <returns>A reference to the selected asteroid set. Shall not be null.</returns>
		/// 
		/// <exception cref="System.InvalidOperationException">Thrown if there are no sets from which to choose, 
		/// or if all spawn rates are zero, or if any rate is negative.</exception> 
		internal static AsteroidSet drawAsteroidSet() {
			return allowedPops.drawAsteroidSet();
		}

		/// <summary>
		/// Searches KSP for a celestial body. Assumes all loaded celestial bodies have unique names.
		/// </summary>
		/// 
		/// <param name="name">The exact, case-sensitive name of the celestial body to recover.</param>
		/// <returns>The celestial body named <c>name</c>.</returns>
		/// 
		/// <exception cref="ArgumentException">Thrown if no celestial body named <c>name</c> exists. The program state 
		/// will be unchanged in the event of an exception.</exception> 
		internal static CelestialBody getPlanetByName(string name) {
			// Would like to only calculate this once, but I don't know for sure that this object will 
			//		be initialized after FlightGlobals
			CelestialBody theBody = FlightGlobals.Bodies.Find(body => body.name == name);
			if (theBody == null) {
				throw new ArgumentException("[CustomAsteroids]: could not find celestial body named " + name, 
					"name");
			}
			return theBody;
		}
	}
}
