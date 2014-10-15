using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Starstrider42 {

	namespace CustomAsteroids {
		/** Represents asteroid-specific information not included in the stock game
		 * 
		 * When in the same part as an instance of ModuleAsteroid, this class is responsible 
		 * for updating the following non-persistent fields:
		 * 		* ModuleAsteroid.density
		 * 		* ModuleAsteroid.sampleExperimentXmitScalar
		 * 		* ModuleAsteroid.sampleExperimentId
		 * and the following persistent field:
		 * 		* ModuleAsteroid.prefabBaseURL
		 */
		public class CustomAsteroidData : PartModule {
			[KSPField (isPersistant = true)]
			public string composition = "Stony";

			// Default density from ModuleAsteroid, in tons/m^3
			[Persistent] public float density = 0.03f;

			// Default fraction of science recovered by transmitting back to Kerbin, from ModuleAsteroid.
			[Persistent] public float sampleExperimentXmitScalar = 0.3f;
			// Default sampling experiment from ModuleAsteroid.
			[Persistent] public string sampleExperimentId = "asteroidSample";
		}

		/** Stores data on asteroids that have not yet been loaded
		 */
		public class AsteroidDataRepository : ScenarioModule {
			internal AsteroidDataRepository() {
				unloadedAsteroids = new SortedDictionary<Guid, CustomAsteroidData>();
			}

			/** Called when the module is either constructed or loaded as part of a save game
			 * 
			 * @param[in] node The ConfigNode representing this ScenarioModule
			 * 
			 * @post The module is initialized with any settings in @p node
			 */
			public override void OnLoad(ConfigNode node)
			{
				base.OnLoad(node);

				#if DEBUG
				Debug.Log("CustomAsteroids: full node = " + node);
				#endif
				ConfigNode thisNode = node.GetNode("KnownAsteroids");
				if (thisNode != null) {
					ConfigNode.LoadObjectFromConfig(this, thisNode);
					// Have to add the dictionary by hand
					ConfigNode dict = thisNode.GetNode("Repository");
					if (dict != null) {
						unloadedAsteroids.Clear();
						foreach (ConfigNode entry in dict.GetNodes("Record")) {
							Guid key = new Guid(entry.GetValue("Asteroid"));

							CustomAsteroidData value = new CustomAsteroidData();
							ConfigNode.LoadObjectFromConfig(value, entry.GetNode("Data"));

							unloadedAsteroids.Add(key, value);
						}
					}
				}
			}

			/** Called when the save game including the module is saved
			 * 
			 * @param[out] node The ConfigNode representing this ScenarioModule
			 * 
			 * @post @p node is initialized with the persistent contents of this object
			 */
			public override void OnSave(ConfigNode node)
			{
				base.OnSave(node);

				ConfigNode allData = new ConfigNode();
				ConfigNode.CreateConfigFromObject(this, allData);
				// Have to add the dictionary by hand
				ConfigNode dict = new ConfigNode("Repository");
				foreach (KeyValuePair<Guid, CustomAsteroidData> p in unloadedAsteroids) {
					ConfigNode entry = new ConfigNode("Record");
					entry.AddValue("Asteroid", p.Key);
					ConfigNode data = new ConfigNode();
						ConfigNode.CreateConfigFromObject(p.Value, data);
						data.name = "Data";
						entry.AddNode(data);
					dict.AddNode(entry);
				}
				allData.AddNode(dict);
				allData.name = "KnownAsteroids";
				node.AddNode(allData);

				#if DEBUG
				Debug.Log("CustomAsteroids: saved node = " + node);
				#endif
			}

			/** Wrapper function for looking up the scenario module
			 * 
			 * @return The AsteroidDataRepository associated with the loaded game, or null 
			 * 	if no such module exists.
			 * 
			 * @exceptsafe Must not throw exceptions
			 */
			internal static AsteroidDataRepository findModule() {
				try {
					AsteroidDataRepository module = 
						(AsteroidDataRepository) HighLogic.CurrentGame.scenarios.
						Find(sc => sc.moduleRef is AsteroidDataRepository).moduleRef;
					return module;
				} catch (Exception) {
					return null;
				}
			}

			/** Adds data for a particular asteroid to the registry
			 * 
			 * @param[in] asteroid The asteroid with which the data will be permanently associated.
			 * @param[in] data The data to store until the asteroid is loaded.
			 * 
			 * @pre @p asteroid has never been loaded
			 * @pre @p asteroid contains exactly one part that has a CustomAsteroidData module
			 * 
			 * @exception ArgumentException Thrown if @p asteroid is already registered.
			 * 
			 * @exceptsafe The AsteroidDataRepository must be unchanged in the event of an exception.
			 */
			internal void register(Vessel asteroid, CustomAsteroidData data) {
				unloadedAsteroids.Add(asteroid.id, data);
			}

			/** Stops storing data for a particular asteroid
			 * 
			 * @param[in] asteroid The asteroid to remove from the registry
			 * 
			 * @post The registry does not contain data for @p asteroid
			 * 
			 * @exceptsafe The AsteroidDataRepository must be unchanged in the event of an exception.
			 */
			internal void unregister(Vessel asteroid) {
				// Dictionary.Remove() does not throw in the case of a missing key
				unloadedAsteroids.Remove(asteroid.id);
			}

			/** Returns data associated with a particular asteroid
			 * 
			 * @param[in] asteroid The asteroid to look up
			 * 
			 * @pre @p asteroid is an asteroid rather than an artificial vessel (caller's responsibility)
			 * 
			 * @post Returns the CustomAsteroidData instance associated with the asteroid, or a default value 
			 * 	if no instance is stored
			 * 
			 * @exception NullReferenceException Thrown if @p asteroid is null.
			 * 
			 * @exceptsafe The game state must be unchanged in the event of an exception.
			 */
			public static CustomAsteroidData getAsteroidData(Vessel asteroid) {
				// Module in vessel takes precedence
				List<CustomAsteroidData> active = asteroid.FindPartModulesImplementing<CustomAsteroidData>();
				if (active != null && active.Count > 0) {
					return active.First();
				}

				// Anything in the repository?
				AsteroidDataRepository repo = findModule();
				if (repo != null) {
					try {
						return repo.unloadedAsteroids[asteroid.id];
					} catch (KeyNotFoundException) {}
				}

				// When all else fails, assume default asteroid type
				return new CustomAsteroidData();
			}

			/** The list of known asteroids
			 * 
			 * @invariant Every asteroid that has never been loaded has its Guid in @p unloadedAsteroids.
			 * @invariant Every Guid in @p unloadedAsteroids is the pid of an unloaded asteroid.
			 * 
			 * @warning DO NOT store loaded asteroids in this dictionary. Their pid's may change 
			 *  during the game, invalidating the dictionary lookup.
			 * 	@see [Anatid's KSP API documentation] (https://github.com/Anatid/XML-Documentation-for-the-KSP-API/blob/master/src/Vessel.cs#L63) 
			 *  for details
			 */
			private SortedDictionary<Guid, CustomAsteroidData> unloadedAsteroids;
		}

		/** Checks relationship between stock and custom spawners
		 */
		internal class SetupRepository : MonoBehaviour {
			/** Called on the frame when a script is enabled just before any of the Update methods is called the first time.
			 * 
			 * @see[Unity Documentation] (http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.Start.html)
			 * 
			 * @todo What exceptions are thrown by StartCoroutine?
			 */
			public void Start()
			{
				//StartCoroutine(editStockSpawner());
				StartCoroutine("confirmAsteroidRepository");
				GameEvents.onVesselDestroy.Add(obsoleteAsteroid);
			}

			/** This function is called when the object will be destroyed.
			 * 
			 * @see [Unity Documentation] (http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnDestroy.html)
			 * 
			 * @todo What exceptions are thrown by StopCoroutine?
			 */
			public void OnDestroy() {
				StopCoroutine("confirmAsteroidRepository");
				GameEvents.onVesselDestroy.Remove(obsoleteAsteroid);
			}

			/** Function called when an asteroid in the repository no longer exists
			 */
			private void obsoleteAsteroid(Vessel vessel) {
				AsteroidDataRepository repo = AsteroidDataRepository.findModule();
				if (repo != null) {
					repo.unregister(vessel);
					#if DEBUG
					Debug.Log("Unregistered obsolete asteroid " + vessel.vesselName);
					#endif
				} else {
					Debug.LogError("Could not unregister asteroid " + vessel.vesselName, vessel);
				}
			}

			/** Ensures the current game can store asteroids
			 * 
			 * @return Controls the delay before execution resumes
			 * 
			 * @see [Unity documentation](http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.StartCoroutine.html)
			 * 
			 * @post The currently loaded game has an AsteroidDataRepository scenario
			 * 
			 * @note The spawner must be loaded whether or not custom spawning is enabled, in case the 
			 * 		player changes the setting for a later game session
			 */
			internal System.Collections.IEnumerator confirmAsteroidRepository() {
				while (HighLogic.CurrentGame.scenarios[0].moduleRef == null) {
					yield return 0;
				}

				if (AsteroidDataRepository.findModule() == null) {
					Debug.Log("CustomAsteroids: Adding AsteroidDataRepository to game '" + HighLogic.CurrentGame.Title + "'");
					HighLogic.CurrentGame.AddProtoScenarioModule(typeof(AsteroidDataRepository), 
						GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT);
				}
			}
		}

		/** Workaround to let SetupSpawner be run in multiple specific scenes
		 * 
		 * Shamelessly stolen from Trigger Au, thanks for the idea!
		 * 
		 * Loaded on entering any Flight scene
		 */
		[KSPAddon(KSPAddon.Startup.Flight, false)]
		internal class SRFlight : SetupRepository {
		}
		/** Workaround to let SetupSpawner be run in multiple specific scenes
		 * 
		 * Shamelessly stolen from Trigger Au, thanks for the idea!
		 * 
		 * Loaded on entering any SpaceCentre scene
		 */
		[KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
		internal class SRSpaceCenter : SetupRepository {
		}
		/** Workaround to let SetupSpawner be run in multiple specific scenes
		 * 
		 * Shamelessly stolen from Trigger Au, thanks for the idea!
		 * 
		 * Loaded on entering any TrackingStation scene
		 */
		[KSPAddon(KSPAddon.Startup.TrackingStation, false)]
		internal class SRTrackingStation : SetupRepository {
		}

	}
}
