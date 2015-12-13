/** Determines which asteroid gets which orbit
 * @file AsteroidManager.cs
 * @author %Starstrider42
 * @date Created April 10, 2014
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Is there a good way to sync version number between here, doxygen.cfg, the markdown source, and Git tags?
[assembly:AssemblyVersion("1.2.0")]

namespace Starstrider42 {

	namespace CustomAsteroids {
		/** 
		 * Central class for controlling Custom Asteroids configuration.
		 */
		internal static class AsteroidManager {
			/** Loads all Custom Asteroids settings
			 * 
			 * @exceptsafe The object is in a consistent state in the event of an exception
			 */
			static AsteroidManager() {
				try {
					curOptions  = Options.Load();
					allowedPops = PopulationLoader.Load();

					Debug.Log("[CustomAsteroids]: " + allowedPops.getTotalRate() + " new discoveries per Earth day.");
				} catch (Exception) {
					// Ensure the contents of AsteroidManager are predictable even in the event of an exception
					// Though an exception thrown by a static constructor is basically unrecoverable...
					curOptions  = new Options();
					allowedPops = new PopulationLoader();
					throw;
				}
			}

			/** Returns the current options used by Custom Asteroids
			 * 
			 * @return An Options objects with the settings to use
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			internal static Options getOptions() {
				return curOptions;
			}

			/** Provides rate at which asteroids should be created
			 * 
			 * @return The total spawn rate of all loaded Populations
			 * 
			 * @exceptsafe Does not throw exceptions.
			 */
			internal static double spawnRate() {
				return allowedPops.getTotalRate();
			}

			/** Randomly selects an asteroid population
			 * 
			 * The selection is weighted by the spawn rate of each population; a population with 
			 * 		a rate of 2.0 is twice as likely to be chosen as one with a rate of 1.0.
			 * 
			 * @return A reference to the selected population
			 * 
			 * @exception System.InvalidOperationException Thrown if there are no populations from 
			 * 		which to choose, or if all spawn rates are zero, or if any rate is negative
			 */
			internal static Population drawPopulation() {
				return allowedPops.drawPopulation();
			}

			/** Returns info about the default population.
			 * 
			 * @return the object used to represent stock-like asteroids. 
			 * 		SHALL NOT be null, but MAY have a spawn rate of zero.
			 */
			internal static DefaultAsteroids defaultPopulation() {
				return allowedPops.defaultAsteroids();
			}

			/** Singleton object responsible for handling Custom Asteroids configurations */
			private static PopulationLoader allowedPops;

			/** Singleton object responsible for handling Custom Asteroids options */
			private static Options curOptions;
		}
	}
}
