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
[assembly:AssemblyVersion("1.1.0")]

namespace Starstrider42 {

	namespace CustomAsteroids {
		/** Central class for controlling asteroid orbits
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

					Debug.Log("CustomAsteroids: " + allowedPops.getTotalRate() + " new discoveries per Earth day");
				} catch (Exception) {
					// Ensure the contents of AsteroidManager are predictable even in the event of an exception
					// Though an exception thrown by a static constructor is basically unrecoverable...
					curOptions  = new Options();
					allowedPops = new PopulationLoader();
					throw;
				}
			}

			/** Customizes an asteroid, based on the settings loaded to Custom Asteroids
			 * 
			 * @param[in,out] asteroid The asteroid to be modified
			 * 
			 * @pre @p asteroid is a valid asteroid object in the game
			 * @pre @p asteroid has never been loaded in physics range
			 * 
			 * @post @p asteroid has properties consistent with membership in a randomly 
			 * 		chosen population
			 * 
			 * @exception System.InvalidOperationException Thrown if there are no populations in 
			 * 		which to place the asteroid
			 * @exception AsteroidManager.BadPopulationException Thrown if a 
			 * 		population exists, but cannot generate valid data
			 * 
			 * @exceptsafe The program is in a consistent state in the event of an exception
			 */
			internal static void editAsteroid(Vessel asteroid) {
				Population newPop = allowedPops.drawPopulation();

				// newPop == null means "leave asteroid in default population"
				if (newPop != null) {
					try {
						asteroid.orbitDriver.orbit = newPop.drawOrbit();
					} catch (InvalidOperationException e) {
						throw new BadPopulationException (newPop, 
							"CustomAsteroids: Selected invalid population " + newPop, e);
					}
				}

				if (curOptions.getRenameOption() && asteroid.GetName() != null) {
					string asteroidId = asteroid.GetName();
					string    newName = (newPop != null ? newPop.getAsteroidName() : allowedPops.defaultName());
					if (asteroidId.IndexOf("Ast. ") >= 0) {
						// Keep only the ID number
						asteroidId = asteroidId.Substring(asteroidId.IndexOf("Ast. ") + "Ast. ".Length);
						asteroid.vesselName = newName + " " + asteroidId;
					} 	// if asteroid name doesn't match expected format, leave it as-is
				}

				AsteroidDataRepository repo = AsteroidDataRepository.findModule();
				if (repo != null) {
					repo.register(asteroid, new CustomAsteroidData());
					#if DEBUG
					Debug.Log("CustomAsteroids: added " + asteroid.GetName() + " to repository");
					#endif
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

			/** Singleton object responsible for handling Custom Asteroids configurations */
			private static PopulationLoader allowedPops;

			/** Singleton object responsible for handling Custom Asteroids options */
			private static Options curOptions;

			/** Exception indicating that a Population is in an invalid state
			 */
			internal class BadPopulationException : InvalidOperationException {
				/** Constructs an exception with no specific information
				 * 
				 * @exceptsafe Does not throw exceptions
				 */
				public BadPopulationException() : base() {
					badPop = null;
				}

				/** Constructs an exception with a reference to the invalid Population
				 *
				 * @param[in] which The population that triggered the exception
				 *
				 * @post getPop() = @p which
				 * 
				 * @exceptsafe Does not throw exceptions
				 */
				public BadPopulationException(Population which) : base() {
					badPop = which;
				}

				/** Constructs an exception with a reference to the invalid Population
				 *
				 * @param[in] which The population that triggered the exception
				 * @param[in] message A description of the detected problem
				 *
				 * @post getPop() = @p which
				 * @post @p base.Message = @p message
				 * 
				 * @exceptsafe Does not throw exceptions
				 * 
				 * @see [InvalidOperationException(string)](http://msdn.microsoft.com/en-us/library/7yaybx04%28v=vs.90%29.aspx)
				 */
				public BadPopulationException(Population which, string message) : base(message) {
					badPop = which;
				}

				/** Constructs an exception with a reference to the invalid Population
				 *
				 * @param[in] which The population that triggered the exception
				 * @param[in] message A description of the detected problem
				 * @param[in] inner The exception thrown when the problem was detected
				 *
				 * @post getPop() = @p which
				 * @post @p base.Message = @p message
				 * @post @p base.InnerException = @p inner
				 * 
				 * @exceptsafe Does not throw exceptions
				 * 
				 * @see [InvalidOperationException(string, Exception)](http://msdn.microsoft.com/en-us/library/x4zw1bf5%28v=vs.90%29.aspx)
				 */
				public BadPopulationException(Population which, string message, Exception inner) 
					: base(message, inner) {
					badPop = which;
				}

				/** Constructs an exception with a reference to the invalid Population
				 *
				 * @param[in] info The object that holds the serialized object data.
				 * @param[in] context The contextual information about the source or destination. 
				 * 
				 * @exceptsafe Does not throw exceptions
				 * 
				 * @see [InvalidOperationException(SerializationInfo, StreamingContext)](http://msdn.microsoft.com/en-us/library/x5c916ac%28v=vs.90%29.aspx)
				 */
				protected BadPopulationException(System.Runtime.Serialization.SerializationInfo info, 
						System.Runtime.Serialization.StreamingContext context)
					: base(info, context) {}

				/** Provides the invalid Population that triggered the exception
				 *
				 * @return A reference to the faulty object, or `null` if no 
				 *	object was stored.
				 * 
				 * @exceptsafe Does not throw exceptions
				 */
				public Population getPop() {
					return badPop;
				}

				/** The invalid Population that triggered the exception */
				private Population badPop;
			}
		}
	}
}
