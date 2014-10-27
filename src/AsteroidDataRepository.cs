/** Stores asteroid properties for asteroids that have not been initialized as vessels
 * @file AsteroidDataRepository.cs
 * @author %Starstrider42
 * @date Created October 26, 2014
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Starstrider42 {

	namespace CustomAsteroids {

		/** Stores data on asteroids that have not yet been loaded
		 */
		public class AsteroidDataRepository : ScenarioModule {
			internal AsteroidDataRepository() {
				loaded = false;
				unloadedAsteroids = new SortedDictionary<Guid, ConfigNode>();
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
				var nodeForm = new ConfigNode();
				ConfigNode.CreateConfigFromObject(data, nodeForm);
				register(asteroid, nodeForm);
			}

			/** Adds data for a particular asteroid to the registry
			 * 
			 * @param[in] asteroid The asteroid with which the data will be permanently associated.
			 * @param[in] node The data to store until the asteroid is loaded.
			 * 
			 * @pre @p asteroid has never been loaded
			 * @pre @p asteroid contains exactly one part that has a CustomAsteroidData module
			 * 
			 * @exception ArgumentException Thrown if @p asteroid is already registered.
			 * 
			 * @exceptsafe The AsteroidDataRepository must be unchanged in the event of an exception.
			 */
			internal void register(Vessel asteroid, ConfigNode node) {
				unloadedAsteroids.Add(asteroid.id, node);
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
			 * @post Returns a ConfigNode representing the CustomAsteroidData instance associated with the 
			 * 	asteroid, or a default value if no instance is stored
			 * 
			 * @exception NullReferenceException Thrown if @p asteroid is null.
			 * 
			 * @exceptsafe The game state must be unchanged in the event of an exception.
			 */
			internal static ConfigNode getAsteroidData(Vessel asteroid) {
				// Active module in loaded vessel takes precedence
				List<CustomAsteroidData> active = asteroid.FindPartModulesImplementing<CustomAsteroidData>();
				if (active != null && active.Count > 0) {
					var nodeForm = new ConfigNode();
					ConfigNode.CreateConfigFromObject(active.First(), nodeForm);
					return nodeForm;
				}

				// Unloaded but initialized asteroid?
				foreach (ProtoPartSnapshot part in asteroid.protoVessel.protoPartSnapshots) {
					foreach (ProtoPartModuleSnapshot module in part.modules) {
						if (module.moduleName == "CustomAsteroidData") {
							var nodeForm = new ConfigNode();
							module.Save(nodeForm);
							return nodeForm;
						}
					}
				}

				// Uninitialized asteroid?
				AsteroidDataRepository repo = findModule();
				if (repo != null) {
					try {
						ConfigNode archiveData = repo.unloadedAsteroids[asteroid.id];
						return archiveData;
					} catch (KeyNotFoundException) {}
				}

				// When all else fails, assume default asteroid type
				var node = new ConfigNode();
				ConfigNode.CreateConfigFromObject(new CustomAsteroidData(), node);
				return node;
			}

			/** Called on the frame when a script is enabled just before any of the Update methods is called the first time.
			 * 
			 * @see[Unity Documentation] (http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.Start.html)
			 * 
			 * @todo What exceptions are thrown by StartCoroutine?
			 */
			public void Start()
			{
				GameEvents.onVesselDestroy.Add(unregister);
				// GameEvents.onVesselGoOffRails doesn't work for some reason
				GameEvents.onPartUnpack.Add(checkAsteroid);
			}

			/** This function is called when the object will be destroyed.
			 * 
			 * @see [Unity Documentation] (http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnDestroy.html)
			 * 
			 * @todo What exceptions are thrown by StopCoroutine?
			 */
			public void OnDestroy() {
				GameEvents.onVesselDestroy.Remove(unregister);
				GameEvents.onPartUnpack.Remove(checkAsteroid);
			}

			/** Function called when an asteroid is loaded
			 */
			private void checkAsteroid(Part potato) {
				if (unloadedAsteroids.ContainsKey(potato.vessel.id) 
						&& potato.FindModuleImplementing<CustomAsteroidData>() != null) {
					#if DEBUG
					Debug.Log("CustomAsteroids: Transferring registration of asteroid " + potato.vessel.vesselName);
					#endif
					ConfigNode newData = unloadedAsteroids[potato.vessel.id];

					List<CustomAsteroidData> oldData = potato.FindModulesImplementing<CustomAsteroidData>();

					foreach (CustomAsteroidData oldModule in oldData) {
						oldModule.Load(newData);
					}
					unregister(potato.vessel);
				} // else we're good
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

							unloadedAsteroids.Add(key, entry.GetNode("Data").CreateCopy());
						}
					}
				}

				loaded = true;
			}

			/** If true, then object is up-to-date. If false, object should be treated as uninitialized.
			 */
			internal bool isLoaded() {
				return loaded;
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
				foreach (KeyValuePair<Guid, ConfigNode> p in unloadedAsteroids) {
					ConfigNode entry = new ConfigNode("Record");
					entry.AddValue("Asteroid", p.Key);
					ConfigNode record = p.Value.CreateCopy();
					record.name = "Data";
					entry.AddNode(record);
					dict.AddNode(entry);
				}
				allData.AddNode(dict);
				allData.name = "KnownAsteroids";
				node.AddNode(allData);

				#if DEBUG
				Debug.Log("CustomAsteroids: saved node = " + node);
				#endif
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
			private SortedDictionary<Guid, ConfigNode> unloadedAsteroids;

			/** Flag to indicate that AsteroidDataRepository is up-to-date
			 */
			private bool loaded;
		}


		/** Workaround to let module adder be run in multiple specific scenes
		 * 
		 * Shamelessly stolen from Trigger Au, thanks for the idea!
		 * 
		 * Loaded on entering any Flight scene
		 */
		[KSPAddon(KSPAddon.Startup.Flight, false)]
		internal class ADRFlight : AddScenario<AsteroidDataRepository> {
		}
		/** Workaround to let module adder be run in multiple specific scenes
		 * 
		 * Shamelessly stolen from Trigger Au, thanks for the idea!
		 * 
		 * Loaded on entering any SpaceCentre scene
		 */
		[KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
		internal class ADRSpaceCenter : AddScenario<AsteroidDataRepository> {
		}
		/** Workaround to let module adder be run in multiple specific scenes
		 * 
		 * Shamelessly stolen from Trigger Au, thanks for the idea!
		 * 
		 * Loaded on entering any TrackingStation scene
		 */
		[KSPAddon(KSPAddon.Startup.TrackingStation, false)]
		internal class ADRTrackingStation : AddScenario<AsteroidDataRepository> {
		}

	}
}
