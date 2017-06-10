using System;
using System.Reflection;
using KSP.Localization;
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

				var parentNode = new ConfigNode();
				var allData = parentNode.AddNode("CustomAsteroidSettings");
				ConfigNode.CreateConfigFromObject(this, allData);		// Only overload that works!

				// Create directories if necessary
				var outFile = new System.IO.FileInfo(optionFile());
				System.IO.Directory.CreateDirectory(outFile.DirectoryName);
				parentNode.Save(outFile.FullName);
				Debug.Log("[CustomAsteroids]: " + Localizer.Format ("#autoLOC_CustomAsteroids_LogOptionsSave"));
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
				Debug.Log("[CustomAsteroids]: " + Localizer.Format ("#autoLOC_CustomAsteroids_LogOptionsLoad1"));

				var allOptions = loadNewStyleOptions();
				if (allOptions == null) {
					allOptions = loadOldStyleOptions ();
				}

				if (allOptions.versionNumber != latestVersion()) {
					// Config file is either missing or out of date, make a new one
					// Any information loaded from previous config file will be preserved
					updateOptionFile(allOptions);
				}

				Debug.Log("[CustomAsteroids]: " + Localizer.Format ("#autoLOC_CustomAsteroids_LogOptionsLoad2"));

				return allOptions;
			} catch (ArgumentException e) {
				Debug.LogError ("[CustomAsteroids]: " + Localizer.Format ("#autoLOC_CustomAsteroids_LogOptionsNoLoad"));
				Debug.LogException (e);
				ScreenMessages.PostScreenMessage (
					"[CustomAsteroids]: " + Localizer.Format (
						"#autoLOC_CustomAsteroids_ErrorBasic", "#autoLOC_CustomAsteroids_LogOptionsNoLoad", e.Message),
					10.0f, ScreenMessageStyle.UPPER_CENTER);
				return new Options();
			} catch {
				return new Options();
			}
		}

		/// <summary>
		/// Reads a modern (MM-compatible) options file.
		/// </summary>
		/// <returns>The options stored in the game databse, or <c>null</c> if none were found.</returns>
		/// <exception cref="ArgumentException">Thrown if there is a syntax error in
		/// 	one of the options.</exception>
		private static Options loadNewStyleOptions() {
			UrlDir.UrlConfig[] configList = GameDatabase.Instance.GetConfigs("CustomAsteroidSettings");
			if (configList.Length > 0) {
				// Start with the default options
				var options = new Options ();

				foreach (UrlDir.UrlConfig settings in configList) {
					ConfigNode.LoadObjectFromConfig (options, settings.config);
				}
				return options;
			} else {
				return null;
			}
		}

		/// <summary>
		/// Reads a pre-MM options file.
		/// </summary>
		/// <returns>The options in <c>oldOptionFile()</c>, or the default for any unspecified
		///	 option. The <c>versionNumber</c> field shall contain the version number
		///	 of the options file, or "" if no such file exists.</returns>
		/// <exception cref="ArgumentException">Thrown if there is a syntax error in
		/// 	the options file.</exception>
		private static Options loadOldStyleOptions() {
			// Start with the default options
			var options = new Options();

			ConfigNode optFile = ConfigNode.Load(oldOptionFile());
			if (optFile != null) {
				ConfigNode.LoadObjectFromConfig(options, optFile);
				// Backward-compatible with initial release
				if (!optFile.HasValue ("VersionNumber")) {
					options.versionNumber = "0.1.0";
				}
				// Backward-compatible with versions 1.1.0 and earlier
				if (!optFile.HasValue ("Spawner") && optFile.HasValue ("UseCustomSpawner")) {
					options.spawner = optFile.GetValue ("UseCustomSpawner").Equals ("False")
						? SpawnerType.Stock : SpawnerType.FixedRate;
				}
			} else {
				options.versionNumber = "";
			}
			return options;
		}

		/// <summary>
		/// Replaces a missing or out-of-date preferences file. Failures to write the file to disk are logged and ignored.
		/// </summary>
		/// <param name="oldData">The data to store in the new file.</param>
		private static void updateOptionFile(Options oldData) {
			try {
				oldData.save();
				if (oldData.versionNumber.Length == 0) {
					Debug.Log ("[CustomAsteroids]: "
							   + Localizer.Format ("#autoLOC_CustomAsteroids_LogNoOptions", optionFile ()));
				} else {
					Debug.Log ("[CustomAsteroids]: "
							   + Localizer.Format ("#autoLOC_CustomAsteroids_LogOldOptions",
												   oldData.versionNumber,
												   latestVersion ()));
				}
			} catch (Exception e) {
				Debug.LogError ("[CustomAsteroids]: " + Localizer.Format ("#autoLOC_CustomAsteroids_LogOptionsNoSave"));
				Debug.LogException (e);
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
				throw new InvalidOperationException(
					Localizer.Format ("#autoLOC_CustomAsteroids_ErrorOptionsBadMin", minUntrackedLifetime));
			}
			if (maxUntrackedLifetime <= 0.0f) {
				throw new InvalidOperationException(
					Localizer.Format ("#autoLOC_CustomAsteroids_ErrorOptionsBadMax", maxUntrackedLifetime));
			}
			if (maxUntrackedLifetime < minUntrackedLifetime) {
				throw new InvalidOperationException (
					Localizer.Format ("#autoLOC_CustomAsteroids_ErrorOptionsBadRange",
								  minUntrackedLifetime, maxUntrackedLifetime));
			}
			return new Pair<float, float>(minUntrackedLifetime, maxUntrackedLifetime);
		}

		/// <summary>
		/// Identifies the version 1.4 and earlier Custom Asteroids config file. Does not throw exceptions.
		/// </summary>
		/// <returns>An absolute path to the config file.</returns>
		private static string oldOptionFile() {
			return KSPUtil.ApplicationRootPath + "GameData/CustomAsteroids/PluginData/Custom Asteroids Settings.cfg";
		}

		/// <summary>
		/// Identifies the MM-compatible Custom Asteroids config file. Does not throw exceptions.
		/// </summary>
		/// <returns>An absolute path to the config file.</returns>
		private static string optionFile ()
		{
			return KSPUtil.ApplicationRootPath + "GameData/CustomAsteroids/Custom Asteroids Settings.cfg";
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
