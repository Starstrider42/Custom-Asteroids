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
[assembly:AssemblyVersion("1.0.0")]

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

		/** Stores a set of configuration options for Custom Asteroids
		 * 
		 * ConfigNodes are used to manage option persistence
		 */
		internal class Options {
			/** Sets all options to their default values
			 * 
			 * @exceptsafe Does not throw exceptions.
			 */
			internal Options() {
				versionNumber        = latestVersion();
				renameAsteroids      = true;
				minUntrackedLifetime = 1.0f;
				maxUntrackedLifetime = 20.0f;
				useCustomSpawner     = true;
			}

			/** Stores current Custom Asteroids options in a config file
			 * 
			 * @post The current settings are stored to the config file
			 * @post The current Custom Asteroids version is stored to the config file
			 * 
			 * @todo Identify exception conditions
			 * 
			 * @exceptsafe The program is in a consistent state in the event of an exception
			 */
			internal void Save() {
				// File may have been loaded from a previous version
				string trueVersion = versionNumber;
				try {
					versionNumber = latestVersion();

					ConfigNode allData = new ConfigNode();
					ConfigNode.CreateConfigFromObject(this, allData);		// Only overload that works!

					// Create directories if necessary
					System.IO.FileInfo outFile = new System.IO.FileInfo(optionList());
					System.IO.Directory.CreateDirectory(outFile.DirectoryName);
					allData.Save(outFile.FullName);
					Debug.Log("CustomAsteroids: settings saved");
				} finally {
					versionNumber = trueVersion;
				}
			}

			/** Factory method obtaining Custom Asteroids settings from a config file
			 * 
			 * @return A newly constructed Options object containing up-to-date 
			 * 		settings from the Custom Asteroids config file, or the default settings 
			 * 		if no such file exists.
			 * 
			 * @exception System.TypeInitializationException Thrown if the Options object 
			 * 		could not be constructed
			 * 
			 * @exceptsafe The program is in a consistent state in the event of an exception
			 * 
			 * @todo Can I make Load() atomic?
			 */
			internal static Options Load() {
				try {
					// Start with the default options
					Options allOptions = new Options();

					// Load options
					Debug.Log("CustomAsteroids: loading settings...");

					ConfigNode optFile = ConfigNode.Load(optionList());
					if (optFile != null) {
						ConfigNode.LoadObjectFromConfig(allOptions, optFile);
						// Backward-compatible with initial release
						if (!optFile.HasValue("VersionNumber")) {
							allOptions.versionNumber = "0.1.0";
						}
					} else {
						allOptions.versionNumber = "";
					}

					if (allOptions.versionNumber != latestVersion()) {
						// Config file is either missing or out of date, make a new one
						// Any information loaded from previous config file will be preserved
						try {
							allOptions.Save();
							if (allOptions.versionNumber.Length == 0) {
								Debug.Log("CustomAsteroids: no config file found at " + optionList() + "; creating new one");
							} else {
								Debug.Log("CustomAsteroids: loaded config file from version " + allOptions.versionNumber +
									"; updating to version " + latestVersion());
							}
						} catch (Exception e) {
							// First priority, just in case Debug.Log*() produce I/O exceptions themselves
							Debug.LogError("CustomAsteroids: settings could not be saved");
							Debug.LogException(e);
						}
					}

					Debug.Log("CustomAsteroids: settings loaded");

					return allOptions;
					// No idea what kinds of exceptions are thrown by ConfigNode
				} catch (Exception e) {
					throw new TypeInitializationException("Starstrider42.CustomAsteroids.Options", e);
				}
			}

			/** Returns whether or not asteroids may be renamed by their population
			 * 
			 * @return True if renaming allowed, false otherwise.
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			internal bool getRenameOption() {
				return renameAsteroids;
			}

			/** Returns whether or not the ARM asteroid spawner is used
			 * 
			 * @return True if custom spawner used, false if stock.
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			internal bool getCustomSpawner() {
				return useCustomSpawner;
			}

			/** Returns the time range in which untracked asteroids will disappear
			 * 
			 * @return The minimum (@p first) and maximum (@p second) number of days an asteroid 
			 * 		can go untracked
			 * 
			 * @exception System.InvalidOperationException Thrown if @p first is negative, @p second 
			 * 		is nonpositive, or @p first > @p second
			 * 
			 * @exceptsafe Program state is unchanged in the event of an exception
			 */
			internal Pair<float, float> getUntrackedTimes() {
				if (minUntrackedLifetime < 0.0f) {
					throw new InvalidOperationException("Minimum untracked time may not be negative (gave " 
						+ minUntrackedLifetime+ ")");
				}
				if (maxUntrackedLifetime <= 0.0f) {
					throw new InvalidOperationException("Maximum untracked time must be positive (gave " 
						+ maxUntrackedLifetime+ ")");
				}
				if (maxUntrackedLifetime < minUntrackedLifetime) {
					throw new InvalidOperationException("Maximum untracked time must be at least minimum time (gave " 
						+ minUntrackedLifetime + " > " + maxUntrackedLifetime+ ")");
				}
				return new Pair<float, float>(minUntrackedLifetime, maxUntrackedLifetime);
			}

			/** Identifies the Custom Asteroids config file
			 * 
			 * @return An absolute path to the config file
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			private static string optionList() {
				return KSPUtil.ApplicationRootPath + "GameData/CustomAsteroids/PluginData/Custom Asteroids Settings.cfg";
			}

			/** Returns the mod's current version number
			 *
			 * @return A version number in major.minor.patch form
			 *
			 * @exceptsafe Does not throw exceptions
			 */
			private static string latestVersion() {
				return Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
			}

			/////////////////////////////////////////////////////////
			// Config options

			/** Whether or not make to asteroid names match their population */
			[Persistent(name="RenameAsteroids")]
			private bool renameAsteroids;

			/** Whether or not to use custom spawning behavior */
			[Persistent(name="UseCustomSpawner")]
			private bool useCustomSpawner;

			/** Minimum number of days an asteroid goes untracked */
			[Persistent(name="MinUntrackedTime")]
			private float minUntrackedLifetime;

			/** Maximum number of days an asteroid goes untracked */
			[Persistent(name="MaxUntrackedTime")]
			private float maxUntrackedLifetime;

			/** The plugin version for which the settings file was written */
			[Persistent(name="VersionNumber")]
			private string versionNumber;
		}

		/** Stores raw asteroid data
		 * 
		 * @invariant At most one instance of this class exists
		 * 
		 * @todo Clean up this class
		 */
		internal class PopulationLoader {
			/** Creates an empty solar system
			 * 
			 * @post No asteroids will be created
			 * 
			 * @exceptsafe Does not throw exceptions.
			 */
			internal PopulationLoader() {
				asteroidSets = new List<Population>();
				untouchedSet = new DefaultAsteroids();
			}

			/** Factory method obtaining Custom Asteroids settings from a config file
			 * 
			 * @return A newly constructed PopulationLoader object containing a full list
			 * 		of all valid asteroid groups in asteroid config files
			 * 
			 * @exception System.TypeInitializationException Thrown if the PopulationLoader object 
			 * 		could not be constructed
			 * 
			 * @exceptsafe The program is in a consistent state in the event of an exception
			 * 
			 * @todo Can I make Load() atomic?
			 */
			internal static PopulationLoader Load() {
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
									Debug.Log("Customasteroids: ConfigNode '" + curNode + "' loaded");
									#endif
									Population newPop = new Population();
									ConfigNode.LoadObjectFromConfig(newPop, curNode);
									allPops.asteroidSets.Add(newPop);
								} catch (TypeInitializationException e) {
									Debug.LogError("CustomAsteroids: failed to load population '" + curNode.GetValue("name") + "'");
									Debug.LogException(e);
								}	// Attempt to parse remaining populations
							}
							else if (curNode.name == "DEFAULT") {
								try {
									#if DEBUG
									Debug.Log("Customasteroids: ConfigNode '" + curNode + "' loaded");
									#endif
									// Construct-and-swap for better exception safety
									DefaultAsteroids oldPop = new DefaultAsteroids();
									ConfigNode.LoadObjectFromConfig(oldPop, curNode);
									allPops.untouchedSet = oldPop;
								} catch (TypeInitializationException e) {
									Debug.LogError("CustomAsteroids: failed to load population '" + curNode.GetValue("name") + "'");
									Debug.LogException(e);
								}	// Attempt to parse remaining populations
							}
							// ignore any other nodes present
						}
					}

					#if DEBUG
					foreach (Population x in allPops.asteroidSets) {
						Debug.Log("Customasteroids: Population '" + x + "' loaded");
					}
					#endif

					return allPops;
				// No idea what kinds of exceptions are thrown by ConfigNode
				} catch (Exception e) {
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
			 * 		which to choose, or if all spawn rates are zero, or if any rate is negative
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
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
					throw new InvalidOperationException("CustomAsteroids: could not draw population", e);
				}
			}

			/** Returns the total spawn rate of all asteroid populations.
			 * 
			 * @return The sum of all spawn rates for all populations.
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			internal double getTotalRate() {
				double total = 0.0;
				foreach (Population x in asteroidSets) {
					total += x.getSpawnRate();
				}
				return total;
			}

			/** Returns the name used for asteroids on stock orbits
			 * 
			 * @return The name with which to replace "Ast."
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			internal string defaultName() {
				return untouchedSet.getAsteroidName();
			}

			/** Debug function for traversing node tree
			 *
			 * @param[in] node The top-level node of the tree to be printed
			 * 
			 * @post The current node and all nodes beneath it are printed, in depth-first order
			 */
			private static void printNode(ConfigNode node) {
				Debug.Log("printNode: NODE = " + node.name);
				foreach (ConfigNode.Value x in node.values) {
					Debug.Log("printNode: " + x.name + " -> " + x.value);
				}
				foreach (ConfigNode x in node.nodes) {
					printNode(x);
				}
			}

			/** The set of loaded asteroid Population objects
			 *
			 * @note Initialized via ConfigNode
			 */
			private List<Population> asteroidSets;
			/** Settings related to stock-like asteroids
			 *
			 * @note Initialized via ConfigNode
			 */
			private DefaultAsteroids untouchedSet;

			/** Contains settings for asteroids that aren't affected by Custom Asteroids
			 */
			private sealed class DefaultAsteroids
			{
				/** Sets default settings for asteroids with unmodified orbits
				 * 
				 * @post The object is initialized to a state in which it will not be expected to generate orbits.
				 * 
				 * @exceptsafe Does not throw exceptions.
				 * 
				 * @note Required by interface of ConfigNode.LoadObjectFromConfig()
				 */
				internal DefaultAsteroids() {
					this.name         = "default";
					this.title        = "Ast.";
					this.spawnRate    = 0.0;
				}

				/** Returns the rate at which stock-like asteroids are discovered
				 * 
				 * @return The rate relative to the rates of all other populations.
				 * 
				 * @exceptsafe Does not throw exceptions.
				 */
				internal double getSpawnRate() {
					return spawnRate;
				}

				/** Returns the name used for stock-like asteroids
				 * 
				 * @return A human-readable string identifying the population. May not be unique.
				 * 
				 * @exceptsafe Does not throw exceptions.
				 */
				internal string getAsteroidName() {
					return title;
				}

				/** Returns a string that represents the current object.
				 *
				 * @return A simple string identifying the object
				 * 
				 * @see [Object.ToString()](http://msdn.microsoft.com/en-us/library/system.object.tostring%28v=vs.90%29.aspx)
				 */
				public override string ToString() {
					return name;
				}

				////////////////////////////////////////////////////////
				// Population properties

				/** The name of the group */
				[Persistent] private string name;
				/** The name of asteroids with unmodified orbits */
				[Persistent] private string title;
				/** The rate, in asteroids per day, at which asteroids appear on stock orbits */
				[Persistent] private double spawnRate;
			}
		}
	}
}
