using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// Singleton class storing raw asteroid data.
	/// </summary>
	/// <remarks>TODO: Clean up this class.</remarks>
	class PopulationLoader {
		/// <summary>The set of loaded asteroid Population objects.</summary>
		private List<Population> asteroidSets;
		/// <summary>Settings related to stock-like asteroids.</summary>
		private DefaultAsteroids untouchedSet;

		/// <summary>
		/// Creates an empty solar system. Does not throw exceptions.
		/// </summary>
		internal PopulationLoader() {
			asteroidSets = new List<Population>();
			untouchedSet = new DefaultAsteroids();
		}

		/// <summary>
		/// Factory method obtaining Custom Asteroids settings from KSP config state.
		/// </summary>
		/// 
		/// <returns>A newly constructed PopulationLoader object containing a full list
		/// 	of all valid asteroid groups in asteroid config files.</returns>
		/// 
		/// <exception cref="System.TypeInitializationException">Thrown if the PopulationLoader object 
		/// 	could not be constructed. The program is in a consistent state in the event of an 
		/// 	exception.</exception> 
		internal static PopulationLoader load() {
			try {
				// UrlConfig x;
				// x.parent.fullPath;		// Name of file to write to
				// x.config					// AsteroidSet node

				// Start with an empty population list
				PopulationLoader allPops = new PopulationLoader();

				// Search for populations in all config files
				UrlDir.UrlConfig[] configList = GameDatabase.Instance.GetConfigs("AsteroidSets");
				foreach (UrlDir.UrlConfig curSet in configList) {
					foreach (ConfigNode curNode in curSet.config.nodes) {
						if (curNode.name == "ASTEROIDGROUP") {
							try {
								#if DEBUG
								Debug.Log("[CustomAsteroids]: ConfigNode '" + curNode + "' loaded");
								#endif
								Population newPop = new Population();
								ConfigNode.LoadObjectFromConfig(newPop, curNode);
								allPops.asteroidSets.Add(newPop);
							} catch (Exception e) {
								Debug.LogError("[CustomAsteroids]: failed to load population '"
									+ curNode.GetValue("name") + "'");
								Debug.LogException(e);
								if (e.InnerException != null) {
									Util.errorToPlayer("Could not load asteroid group. Cause: \"{0}\"\nRoot Cause: \"{1}\".", 
										e.Message, e.GetBaseException().Message);
								} else {
									Util.errorToPlayer("Could not load asteroid group. Cause: \"{0}\".", 
										e.Message);
								}
							}	// Attempt to parse remaining populations
						} else if (curNode.name == "DEFAULT") {
							try {
								#if DEBUG
								Debug.Log("[CustomAsteroids]: ConfigNode '" + curNode + "' loaded");
								#endif
								// Construct-and-swap for better exception safety
								DefaultAsteroids oldPop = new DefaultAsteroids();
								ConfigNode.LoadObjectFromConfig(oldPop, curNode);
								allPops.untouchedSet = oldPop;
							} catch (TypeInitializationException e) {
								Debug.LogError("[CustomAsteroids]: failed to load population '"
									+ curNode.GetValue("name") + "'");
								Debug.LogException(e);
								if (e.InnerException != null) {
									Util.errorToPlayer("Could not load default asteroids. Cause: \"{0}\"\nRoot Cause: \"{1}\".", 
										e.Message, e.GetBaseException().Message);
								} else {
									Util.errorToPlayer("Could not load default asteroids. Cause: \"{0}\".", 
										e.Message);
								}
							}	// Attempt to parse remaining populations
						}
						// ignore any other nodes present
					}
				}

				#if DEBUG
				foreach (Population x in allPops.asteroidSets) {
					Debug.Log("[CustomAsteroids]: Population '" + x + "' loaded");
				}
				#endif

				if (allPops.asteroidSets.Count == 0) {
					Debug.LogWarning("[CustomAsteroids]: Custom Asteroids could not find any configs in GameData!");
					ScreenMessages.PostScreenMessage(
						"Custom Asteroids could not find any configs in GameData.\nAsteroids will not appear.", 
						10.0f, ScreenMessageStyle.UPPER_CENTER);
				}

				return allPops;
			} catch (Exception e) {
				throw new TypeInitializationException("Starstrider42.CustomAsteroids.PopulationLoader", e);
			}
		}

		/// <summary>
		/// Randomly selects an asteroid population. The selection is weighted by the spawn rate of 
		/// each population; a population with a rate of 2.0 is twice as likely to be chosen as one 
		/// with a rate of 1.0.
		/// </summary>
		/// <returns>A reference to the selected population.</returns>
		/// 
		/// <exception cref="System.InvalidOperationException">Thrown if there are no populations from 
		/// which to choose, or if all spawn rates are zero, or if any rate is negative</exception> 
		internal Population drawPopulation() {
			try {
				// A typedef! A typedef! My kerbdom for a typedef!
				List<Pair<Population, double>> bins = new List<Pair<Population, double>>();
				bins.Add(new Pair<Population, double>(null, untouchedSet.getSpawnRate()));
				foreach (Population x in asteroidSets) {
					bins.Add(new Pair<Population, double>(x, x.getSpawnRate()));
				}

				return RandomDist.weightedSample(bins);
			} catch (ArgumentException e) {
				throw new InvalidOperationException("[CustomAsteroids]: could not draw population", e);
			}
		}

		/// <summary>
		/// Returns the total spawn rate of all asteroid populations. Does not throw exceptions.
		/// </summary>
		/// <returns>The sum of all spawn rates for all populations, in asteroids per day.</returns>
		internal double getTotalRate() {
			double total = untouchedSet.getSpawnRate();
			foreach (Population x in asteroidSets) {
				total += x.getSpawnRate();
			}
			return total;
		}

		/// <summary>
		/// Returns the object used to spawn asteroids on stock orbits. Does not throw exceptions.
		/// </summary>
		/// <returns>The the default asteroid spawner. Shall not be null, but may have a spawn rate of 0.</returns>
		internal DefaultAsteroids defaultAsteroids() {
			return untouchedSet;
		}

		/// <summary>
		/// Debug function for traversing node tree. The indicated node and all nodes beneath it are printed, in 
		/// depth-first order.
		/// </summary>
		/// <param name="node">The top-level node of the tree to be printed.</param>
		private static void printNode(ConfigNode node) {
			Debug.Log("printNode: NODE = " + node.name);
			foreach (ConfigNode.Value x in node.values) {
				Debug.Log("printNode: " + x.name + " -> " + x.value);
			}
			foreach (ConfigNode x in node.nodes) {
				printNode(x);
			}
		}
	}

	/// <summary>
	/// Contains settings for asteroids that aren't affected by Custom Asteroids.
	/// </summary>
	sealed class DefaultAsteroids {
		/// <summary>The name of the group.</summary>
		[Persistent] private string name;
		/// <summary>The name of asteroids with unmodified orbits.</summary>
		[Persistent] private string title;
		/// <summary>The rate, in asteroids per day, at which asteroids appear on stock orbits.</summary>
		[Persistent] private double spawnRate;

		/// <summary>
		/// Sets default settings for asteroids with unmodified orbits. The object is initialized to a state in which 
		/// it will not be expected to generate orbits. Does not throw exceptions.
		/// </summary>
		internal DefaultAsteroids() {
			this.name = "default";
			this.title = "Ast.";
			this.spawnRate = 0.0;
		}

		/// <summary>
		/// Returns the rate at which stock-like asteroids are discovered. Does not throw exceptions.
		/// </summary>
		/// <returns>The spawn rate in asteroids per day.</returns>
		internal double getSpawnRate() {
			return spawnRate;
		}

		/// <summary>
		/// Returns the name used for stock-like asteroids. Does not throw exceptions.
		/// </summary>
		/// <returns>A human-readable string that can be used as an asteroid prefix.</returns>
		internal string getAsteroidName() {
			return title;
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current object.
		/// </summary>
		/// <returns>A simple string identifying the object.</returns>
		/// 
		/// <seealso cref="Object.ToString()"/> 
		public override string ToString() {
			return name;
		}

		/// <summary>
		/// Generates a random orbit in as similar a manner to stock as possible.
		/// </summary>
		/// <returns>The orbit of a randomly selected member of the population.</returns>
		/// 
		/// <exception cref="System.InvalidOperationException">Thrown if cannot produce stockalike orbits. The program 
		/// will be in a consistent state in the event of an exception.</exception> 
		internal Orbit drawOrbit() {
			CelestialBody kerbin = FlightGlobals.Bodies.Find(body => body.isHomeWorld);
			CelestialBody dres = FlightGlobals.Bodies.Find(body => body.name.Equals("Dres"));

			if (dres != null && reachedBody(dres) && UnityEngine.Random.Range(0, 4) == 0) {
				// Drestroids
				double a = RandomDist.drawLogUniform(0.55, 0.65) * dres.sphereOfInfluence;
				double e = RandomDist.drawRayleigh(0.005);
				double i = RandomDist.drawRayleigh(0.005);	// lAn takes care of negative inclinations
				double lAn = RandomDist.drawAngle();
				double aPe = RandomDist.drawAngle();
				double mEp = Math.PI / 180.0 * RandomDist.drawAngle();
				double epoch = Planetarium.GetUniversalTime();

				Debug.Log("[CustomAsteroids]: new orbit at " + a + " m, e = " + e + ", i = " + i
					+ ", aPe = " + aPe + ", lAn = " + lAn + ", mEp = " + mEp + " at epoch " + epoch);
				return new Orbit(i, e, a, lAn, aPe, mEp, epoch, dres);
			} else if (kerbin != null) {
				// Kerbin interceptors
				double delay = RandomDist.drawUniform(50.0, 220.0);
				Debug.Log("[CustomAsteroids]: new orbit will pass by kerbin in " + delay + " days");
				return Orbit.CreateRandomOrbitFlyBy(kerbin, delay);
			} else {
				throw new InvalidOperationException("Cannot create stockalike orbits; Kerbin not found!");
			}
		}

		/// <summary>
		/// Determines whether a body was already visited.
		/// </summary>
		/// <remarks>Borrowed from Kopernicus.</remarks>
		/// 
		/// <param name="body">The celestial body whose exploration status needs to be tested.</param>
		/// <returns><c>true</c>, if <c>body</c> was reached, <c>false</c> otherwise.</returns>
		private static bool reachedBody(CelestialBody body) {
			KSPAchievements.CelestialBodySubtree bodyTree = ProgressTracking.Instance.GetBodyTree(body.name);
			return bodyTree != null && bodyTree.IsReached;
		}
	}
}
