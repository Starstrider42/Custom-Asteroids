using System;
using System.Reflection;
using UnityEngine;

namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// Represents the types of spawning behaviour supported by Custom Asteroids.
	/// </summary>
	internal enum SpawnerType {
		Stock,
		FixedRate
	}

	/// <summary>
	/// Stores a set of configuration options for Custom Asteroids. ConfigNodes are used to manage option persistence.
	/// </summary>
	internal class Options {
		/// <summary>Whether or not make to asteroid names match their population.</summary>
		[Persistent(name = "RenameAsteroids")]
		private bool renameAsteroids;

		/// <summary>The spawner to use for creating and removing asteroids.</summary>
		[Persistent(name = "Spawner")]
		private SpawnerType spawner;

		/// <summary>
		/// Whether or not to report failed asteroid spawns in the game. 
		/// The errors will be logged regardless.
		/// </summary>
		[Persistent(name = "ErrorsOnScreen")]
		private bool errorsOnScreen;

		/// <summary>Minimum number of days an asteroid goes untracked.</summary>
		[Persistent(name = "MinUntrackedTime")]
		private float minUntrackedLifetime;

		/// <summary>Maximum number of days an asteroid goes untracked.</summary>
		[Persistent(name = "MaxUntrackedTime")]
		private float maxUntrackedLifetime;

		/// <summary>The plugin version for which the settings file was written.</summary>
		[Persistent(name = "VersionNumber")]
		private string versionNumber;

		/// <summary>
		/// Sets all options to their default values. Does not throw exceptions.
		/// </summary>
		private Options() {
			this.versionNumber = latestVersion();
			this.renameAsteroids = true;
			this.minUntrackedLifetime = 1.0f;
			this.maxUntrackedLifetime = 20.0f;
			this.spawner = SpawnerType.FixedRate;
			this.errorsOnScreen = true;
		}

		/// <summary>
		/// Stores current Custom Asteroids options in a config file. The version is stored as well. The program is 
		/// in a consistent state in the event of an exception.
		/// </summary>
		internal void save() {
			// File may have been loaded from a previous version
			string trueVersion = versionNumber;
			try {
				versionNumber = latestVersion();

				ConfigNode allData = new ConfigNode();
				ConfigNode.CreateConfigFromObject(this, allData);		// Only overload that works!

				// Create directories if necessary
				System.IO.FileInfo outFile = new System.IO.FileInfo(optionFile());
				System.IO.Directory.CreateDirectory(outFile.DirectoryName);
				allData.Save(outFile.FullName);
				Debug.Log("[CustomAsteroids]: settings saved");
			} finally {
				versionNumber = trueVersion;
			}
		}

		/// <summary>
		/// Factory method obtaining Custom Asteroids settings from a config file. Will not throw exceptions.
		/// </summary>
		/// <returns>A newly constructed Options object containing up-to-date settings from the Custom Asteroids config 
		/// file, or the default settings if no such file exists or the file is corrupted.</returns>
		internal static Options load() {
			try {
				// Start with the default options
				Options allOptions = new Options();

				// Load options
				Debug.Log("[CustomAsteroids]: loading settings...");

				ConfigNode optFile = ConfigNode.Load(optionFile());
				if (optFile != null) {
					try {
						ConfigNode.LoadObjectFromConfig(allOptions, optFile);
						// Backward-compatible with initial release
						if (!optFile.HasValue("VersionNumber")) {
							allOptions.versionNumber = "0.1.0";
						}
						// Backward-compatible with versions 1.1.0 and earlier
						if (!optFile.HasValue("Spawner") && optFile.HasValue("UseCustomSpawner")) {
							allOptions.spawner = optFile.GetValue("UseCustomSpawner").Equals("False") 
								? SpawnerType.Stock : SpawnerType.FixedRate;
						}
					} catch (ArgumentException e) {
						Debug.LogError("Could not load options; reverting to default.");
						Debug.LogException(e);
						ScreenMessages.PostScreenMessage(
							"[CustomAsteroids]: Could not load CustomAsteroids options. Cause: " + e.Message, 
							10.0f, ScreenMessageStyle.UPPER_CENTER);
						// Short-circuit things to prevent default from being saved
						return new Options();
					}
				} else {
					allOptions.versionNumber = "";
				}

				if (allOptions.versionNumber != latestVersion()) {
					// Config file is either missing or out of date, make a new one
					// Any information loaded from previous config file will be preserved
					try {
						allOptions.save();
						if (allOptions.versionNumber.Length == 0) {
							Debug.Log("[CustomAsteroids]: no config file found at " + optionFile() + "; creating new one");
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
			} catch {
				return new Options();
			}
		}

		/// <summary>
		/// Returns whether or not asteroids may be renamed by their population. Does not throw exceptions.
		/// </summary>
		/// <returns><c>true</c> if renaming allowed, <c>false</c> otherwise.</returns>
		internal bool getRenameOption() {
			return renameAsteroids;
		}

		/// <summary>
		/// Returns the spawner chosen in the settings. Does not throw exceptions.
		/// </summary>
		/// <returns>The spawner.</returns>
		internal SpawnerType getSpawner() {
			return spawner;
		}

		/// <summary>
		/// Returns whether or not asteroid spawning errors should appear in the game. Does not throw exceptions.
		/// </summary>
		/// <returns><c>true</c> if errors should be put on screen, <c>false</c> if logged only.</returns>
		internal bool getErrorReporting() {
			return errorsOnScreen;
		}

		/// <summary>
		/// Returns the time range in which untracked asteroids will disappear. The program state is unchanged in 
		/// the event of an exception.
		/// </summary>
		/// <returns>The minimum (<c>first</c>) and maximum (<c>second</c>) number of days an asteroid can go untracked.</returns>
		/// <exception cref="System.InvalidOperationException">Thrown if <c>first</c> is negative, <c>second</c> is nonpositive, 
		/// or <c>first &gt; second</c>.</exception> 
		internal Pair<float, float> getUntrackedTimes() {
			if (minUntrackedLifetime < 0.0f) {
				throw new InvalidOperationException("Minimum untracked time may not be negative (gave "
					+ minUntrackedLifetime + ")");
			}
			if (maxUntrackedLifetime <= 0.0f) {
				throw new InvalidOperationException("Maximum untracked time must be positive (gave "
					+ maxUntrackedLifetime + ")");
			}
			if (maxUntrackedLifetime < minUntrackedLifetime) {
				throw new InvalidOperationException("Maximum untracked time must be at least minimum time (gave "
					+ minUntrackedLifetime + " > " + maxUntrackedLifetime + ")");
			}
			return new Pair<float, float>(minUntrackedLifetime, maxUntrackedLifetime);
		}

		/// <summary>
		/// Identifies the Custom Asteroids config file. Does not throw exceptions.
		/// </summary>
		/// <returns>An absolute path to the config file.</returns>
		private static string optionFile() {
			return KSPUtil.ApplicationRootPath + "GameData/CustomAsteroids/PluginData/Custom Asteroids Settings.cfg";
		}

		/// <summary>
		/// Returns the mod's current version number. Does not throw exceptions.
		/// </summary>
		/// <returns>A version number in major.minor.patch form.</returns>
		private static string latestVersion() {
			return Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
		}
	}
}
