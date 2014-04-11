/** Determines which asteroid gets which orbit
 * @file Population.cs
 * @author Starstrider42
 * @date Created April 10, 2014
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Starstrider42 {

	namespace CustomAsteroids {
		/** Central class for controlling asteroid orbits
		 */
		internal static class OrbitManager {
			static OrbitManager() {
				try {
					allowedPops = PopulationLoader.Load();
					totalRate = allowedPops.getTotalRate();
				} catch (Exception) {
					// Ensure the contents of OrbitManager are predictable even in the event of an exception
					// Though an exception thrown by a static constructor is basically unrecoverable...
					allowedPops = null;
					totalRate = 0.0;
					throw;
				}
			}

			/** Generates a random orbit, based on the settings loaded to Custom Asteroids
			 * 
			 * @return A randomly generated orbit in one of the asteroid populations.
			 * 
			 * @exceptsafe The program is in a consistent state in the event of an exception
			 */
			internal static Orbit makeOrbit() {
				Population newPop = allowedPops.drawPopulation();
				return newPop.drawOrbit();
			}

			private static PopulationLoader allowedPops;
			private static double totalRate;
		}

		/** Stores raw asteroid data
		 * 
		 * @invariant At most one instance of this class exists
		 */
		internal class PopulationLoader {
			/** Stores current Custom Asteroids settings in a config file
			 * 
			 * @warning Not exception-safe?
			 */
			internal void Save() {
				try {
					ConfigNode allData = new ConfigNode ();
					ConfigNode.CreateConfigFromObject(this, allData);
					allData.Save(popList());
					Debug.Log("CustomAsteroids: settings saved");
				} catch (StackOverflowException) {
					Debug.LogError("CustomAsteroids: settings could not be stored as a ConfigNode");
				}
			}

			/** Factory method obtaining Custom Asteroids settings from a config file
			 * 
			 * @return A newly constructed PopulationLoader object containing up-to-date 
			 * 		settings from the Custom Asteroids config file
			 * 
			 * @warning Not exception-safe?
			 */
			internal static PopulationLoader Load() {
				Debug.Log("CustomAsteroids: loading settings...");
				ConfigNode allData = ConfigNode.Load(popList());
				PopulationLoader allPops = new PopulationLoader();

				if (allData != null) {
					ConfigNode.LoadObjectFromConfig(allPops, allData);
				} else {
					// Make a copy for next time
					Debug.Log("CustomAsteroids: creating settings file...");
					allPops.Save();
				}
				Debug.Log("CustomAsteroids: settings loaded");

				return allPops;
			}

			/** Randomly selects an asteroid population
			 * 
			 * The selection is weighted by the spawn rate of each population; a population with 
			 * 		a rate of 2.0 is twice as likely to be chosen as one with a rate of 1.0.
			 * 
			 * @return A reference to the selected population
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			internal Population drawPopulation() {
				// A typedef! A typedef! My kerbdom for a typedef!
				List<Pair<Population, double>> bins = new List<Pair<Population, double>>();
				foreach (Population x in AsteroidSets) {
					bins.Add(new Pair<Population, double>(x, x.getSpawnRate()));
				}

				return RandomDist.weightedSample(bins);
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
					total += x.getSpawnRate ();
				}
				return total;
			}

			// Giving a variable an upper-case name because it looks better in the .cfg file
			[Persistent(collectionIndex="POPULATION")]
			private Population[] AsteroidSets = new Population[]{
				/* In our own solar system, NEOs have a lifetime of about 10 million years, 
				 * or 1/500 the lifetime of the solar system. Therefore, the NEO population 
				 * should be about 1/500 as large as the main belt. But that's no fun...
				 */
				// NKO orbits based on NEO population from "Debiased Orbital and Absolute Magnitude 
				//		Distribution of the Near-Earth Objects" Bottke et al. (2002), Icarus 156, 399
				new Population("Near-Kerbin", 0.3,  6799920128, 52859363534, 0.5, 7.5), 
				new Population("Main Belt",   1.0, 27292805500, 43324628162, 0.18, 7.5)
			};

			/** Identifies the Custom Asteroids config file
			 * 
			 * @return An absolute path to the config file
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			private static string popList() {
				return KSPUtil.ApplicationRootPath + "GameData/Starstrider42/CustomAsteroids/asteroids.cfg";
			}
		}
	}
}
