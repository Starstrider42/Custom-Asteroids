/** Saves and loads mod preferences
 * @file Options.cs
 * @author %Starstrider42
 * @date Created October 26, 2014
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Starstrider42 {

	namespace CustomAsteroids {

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
				errorsOnScreen       = true;
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
					Debug.Log("[CustomAsteroids]: settings saved");
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
					Debug.Log("[CustomAsteroids]: loading settings...");

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
								Debug.Log("[CustomAsteroids]: no config file found at " + optionList() + "; creating new one");
							} else {
								Debug.Log("[CustomAsteroids]: loaded config file from version " + allOptions.versionNumber +
									"; updating to version " + latestVersion());
							}
						} catch (Exception e) {
							// First priority, just in case Debug.Log*() produce I/O exceptions themselves
							Debug.LogError("[CustomAsteroids]: settings could not be saved");
							Debug.LogException(e);
						}
					}

					Debug.Log("[CustomAsteroids]: settings loaded");

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

			/** Returns whether or not asteroid spawning errors should appear in the game.
			 * 
			 * @return True if errors should be put on screen, false if logged only.
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			internal bool getErrorReporting() {
				return errorsOnScreen;
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

			/** Whether or not to report failed asteroid spawns in the game. The errors will be logged regardless. */
			[Persistent(name="ErrorsOnScreen")]
			private bool errorsOnScreen;

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
		
	}
}
