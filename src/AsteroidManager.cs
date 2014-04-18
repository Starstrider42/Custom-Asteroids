/** Determines which asteroid gets which orbit
 * @file Population.cs
 * @author Starstrider42
 * @date Created April 10, 2014
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Is there a good way to sync version number between here, doxygen.cfg, the markdown source, and Git tags?
[assembly:AssemblyVersion("0.2.0")]

namespace Starstrider42 {

	namespace CustomAsteroids {
		/** Central class for controlling asteroid orbits
		 */
		internal static class AsteroidManager {
			static AsteroidManager() {
				try {
					allowedPops = PopulationLoader.Load();
					totalRate = allowedPops.getTotalRate();
				} catch (Exception) {
					// Ensure the contents of AsteroidManager are predictable even in the event of an exception
					// Though an exception thrown by a static constructor is basically unrecoverable...
					allowedPops = null;
					totalRate = 0.0;
					throw;
				}
			}

			private static PopulationLoader allowedPops;
			private static double totalRate;

			/** Generates a random orbit, based on the settings loaded to Custom Asteroids
			 * 
			 * @return A randomly generated orbit in one of the asteroid populations.
			 * 
			 * @exception System.InvalidOperationException Thrown if there are no populations from which 
			 * 		to generate an orbit
			 * @exception Starstrider42.CustomAsteroids.AsteroidManager.BadPopulationException Thrown if a 
			 * 		population exists, but cannot generate valid orbits
			 * 
			 * @exceptsafe The program is in a consistent state in the event of an exception
			 */
			internal static Orbit makeOrbit() {
				Population newPop = allowedPops.drawPopulation();
				try {
					return newPop.drawOrbit();
				} catch (InvalidOperationException e) {
					throw new BadPopulationException (newPop, 
						"CustomAsteroids: Selected invalid population " + newPop, e);
				}
			}

			internal class BadPopulationException : System.InvalidOperationException {
				public BadPopulationException() : base() {
					badPop = null;
				}

				public BadPopulationException(Population which) : base() {
					badPop = which;
				}

				public BadPopulationException(Population which, string message) : base(message) {
					badPop = which;
				}

				public BadPopulationException(Population which, string message, Exception inner) 
					: base(message, inner) {
					badPop = which;
				}

				protected BadPopulationException(System.Runtime.Serialization.SerializationInfo info, 
						System.Runtime.Serialization.StreamingContext context)
					: base(info, context) {}

				public Population getPop() {
					return badPop;
				}

				private Population badPop;
			}
		}

		/** Stores raw asteroid data
		 * 
		 * @invariant At most one instance of this class exists
		 * 
		 * @todo Clean up this class
		 */
		internal class PopulationLoader {
			/** Sets the asteroid model to its default settings, if possible
			 * 
			 * @exceptsafe Does not throw exceptions.
			 * 
			 * @note The initialized object may not contain any Population objects, 
			 * 		if their initializations failed.
			 */
			internal PopulationLoader() {
				try {
					AsteroidSets = new Population[]{
						/* In our own solar system, NEOs have a lifetime of about 10 million years, 
						 * or 1/500 the lifetime of the solar system. Therefore, the NEO population 
						 * should be about 1/500 as large as the main belt, assuming the belt has 
						 * decayed only slowly since the LHB. But that's no fun...
						 */
						// NKO orbits based on NEO population from "Debiased Orbital and Absolute Magnitude 
						//		Distribution of the Near-Earth Objects", Bottke et al. (2002), Icarus 156, 399
						new Population("Near-Kerbin", "Sun", 0.3,  6799920128, 52859363534, 0.5, 7.5), 
						new Population("Main Belt",   "Sun", 1.0, 27292805500, 43324628162, 0.18, 7.5)
					};
				// ConfigNode makes an initialization failure recoverable
				} catch (Exception e) {
					Debug.LogError("CustomAsteroids: PopulationLoader default initialization failed");
					Debug.LogException(e);
					AsteroidSets = new Population[0];
				}

				VersionNumber = latestVersion();
			}

			/** Stores current Custom Asteroids settings in a config file
			 * 
			 * @post The current settings are stored to the config file
			 * @post The current Custom Asteroids version is stored to the config file
			 * 
			 * @todo Identify exception conditions
			 * 
			 * @exceptsafe The program is in a consistent state in the event of an exception
			 * 
			 * @warning A backup system attempts to ensure that the config file does not get corrupted in the 
			 * 		event of an I/O error, but it is not foolproof.
			 */
			internal void Save() {
				// Require a successful backup before proceeding with the save
				if (System.IO.File.Exists(popList())) {
					// File.Move() may be faster, but it requires you to manually delete the backup file first
					// Atomicity requires backing up the backup before deleting it, which quickly gets messy
					System.IO.File.Copy(popList(), backup(), true);
				}
				// assert: popList() unchanged, and popList() and backup() are identical

				// File may have been loaded with a previous version
				VersionNumber = latestVersion();

				ConfigNode allData = new ConfigNode();
				ConfigNode.CreateConfigFromObject(this, allData);
				allData.Save(popList());
				Debug.Log("CustomAsteroids: settings saved");
			}

			/** Factory method obtaining Custom Asteroids settings from a config file
			 * 
			 * @return A newly constructed PopulationLoader object containing up-to-date 
			 * 		settings from the Custom Asteroids config file, or the default settings 
			 * 		if no such file exists.
			 * 
			 * @exception Throws System.TypeInitializationException if the PopulationLoader object 
			 * 		could not be constructed
			 * 
			 * @exceptsafe The program is in a consistent state in the event of an exception
			 * 
			 * @todo Can I make Load() atomic?
			 * 
			 * @todo How to allow backward compatibility if the config file changes?
			 */
			internal static PopulationLoader Load() {
				Debug.Log("CustomAsteroids: loading settings...");
				try {
					ConfigNode allData = ConfigNode.Load(popList());
					PopulationLoader allPops = new PopulationLoader();

					if (allData != null) {
						ConfigNode.LoadObjectFromConfig(allPops, allData);
						if (!allData.HasNode("VersionNumber")) {
							allPops.VersionNumber = "0.1.0";
						}
					} else {
						allPops.VersionNumber = "";
					}
						
					if (allPops.VersionNumber != latestVersion()) {
						// Config file is either missing or out of date, make a new one
						// Any information loaded from config file will be preserved
						try {
							if (allPops.VersionNumber.Length == 0) {
								Debug.Log("CustomAsteroids: no config file found at " + popList() + "; creating new one");
							} else {
								Debug.Log("CustomAsteroids: loaded config file from version " + allPops.VersionNumber +
									"; updating to version " + latestVersion());
							}
							allPops.Save();
						} catch (Exception e) {
							// First priority, just in case Debug.Log*() produce I/O exceptions themselves
							ScreenMessages.PostScreenMessage("WARNING: could not save Custom Asteroids settings, please check " 
								+ popList() + " before starting a new game", 5.0f, ScreenMessageStyle.UPPER_CENTER);
							Debug.LogError("CustomAsteroids: settings could not be saved");
							Debug.LogException(e);
						}
					}
					Debug.Log("CustomAsteroids: settings loaded");

					return allPops;
				// No idea what kinds of exceptions are thrown by ConfigNode
				} catch (Exception e) {
					Debug.LogException(e);
					throw new TypeInitializationException("Starstrider42.CustomAsteroids.PopulationLoader", e);
				}
			}

			/** Randomly selects an asteroid population
			 * 
			 * The selection is weighted by the spawn rate of each population; a population with 
			 * 		a rate of 2.0 is twice as likely to be chosen as one with a rate of 1.0.
			 * 
			 * @return A reference to the selected population
			 * 
			 * @exception System.InvalidOperationException Thrown if there are no populations from 
			 * 		which to choose one, or if all spawn rates are zero, or if any rate is negative
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			internal Population drawPopulation() {
				try {
					// A typedef! A typedef! My kerbdom for a typedef!
					List<Pair<Population, double>> bins = new List<Pair<Population, double>>();
					foreach (Population x in AsteroidSets) {
						bins.Add(new Pair<Population, double>(x, x.getSpawnRate()));
					}

					return RandomDist.weightedSample(bins);
				} catch (ArgumentException e) {
					throw new InvalidOperationException("CustomAsteroids: could not draw population", e);
				}
			}

			/** Returns the total spawn rate of all asteroid populations. Currently only needed 
			 * 		for normalizing the asteroid selection.
			 * 
			 * @return The sum of all spawn rates for all populations.
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			internal double getTotalRate() {
				double total = 0.0;
				foreach (Population x in AsteroidSets) {
					total += x.getSpawnRate();
				}
				return total;
			}

			/** Identifies the Custom Asteroids config file
			 * 
			 * @return An absolute path to the config file
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			private static string popList() {
				return KSPUtil.ApplicationRootPath + "GameData/Starstrider42/CustomAsteroids/asteroids.cfg";
			}

			/** Identifies the backup Custom Asteroids config file
			 * 
			 * @return An absolute path to the backup file
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			private static string backup() {
				return KSPUtil.ApplicationRootPath + "GameData/Starstrider42/CustomAsteroids/asteroids.backup";
			}

			/** Returns the mod's current version number
			 * 
			 * @return a version number in major.minor.patch form
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			private static string latestVersion() {
				return Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
			}

			/////////////////////////////////////////////////////////
			// Config options
			// Giving variables upper-case names because it looks better in the .cfg file
			[Persistent(collectionIndex="POPULATION")]
			private Population[] AsteroidSets;

			[Persistent] private string VersionNumber;
		}
	}
}
