using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// Stores data on asteroids that have not yet been loaded.
	/// </summary>
	[KSPScenario(
		ScenarioCreationOptions.AddToAllGames,
		GameScenes.SPACECENTER,
		GameScenes.TRACKSTATION,
		GameScenes.FLIGHT)]
	public class AsteroidDataRepository : ScenarioModule {
		/// <summary>
		/// <para>The list of known asteroids. Every asteroid that has never been loaded has its Guid in this object, 
		/// and every Guid in this object is the pid of an unloaded asteroid.</para>
		/// 
		/// <para>WARNING: DO NOT store loaded asteroids in this dictionary. Their pid's may change
		/// during the game, invalidating the dictionary lookup. See 
		/// [Anatid's KSP API documentation](https://github.com/Anatid/XML-Documentation-for-the-KSP-API/blob/master/src/Vessel.cs#L63) 
		/// for details.</para>
		/// </summary>
		private readonly SortedDictionary<Guid, ConfigNode> unloadedAsteroids;

		/// <summary>Flag to indicate that AsteroidDataRepository is up-to-date.</summary>
		private bool loaded;

		/// <summary>
		/// Initializes the scenario prior to loading persistent data.
		/// </summary>
		internal AsteroidDataRepository() {
			loaded = false;
			unloadedAsteroids = new SortedDictionary<Guid, ConfigNode>();
		}

		/// <summary>
		/// Wrapper function for looking up the scenario module. Does not throw exceptions.
		/// </summary>
		/// <returns>The AsteroidDataRepository associated with the loaded game, or null 
		/// if no such module exists.</returns>
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

		/// <summary>
		/// Adds data for a particular asteroid to the registry.
		/// </summary>
		/// 
		/// <param name="asteroid">The asteroid with which the data will be permanently associated. Must 
		/// never have been loaded, and must contain exactly one part with a <c>CustomAsteroidData</c> module.</param>
		/// <param name="data">The data to store until the asteroid is loaded.</param>
		/// 
		/// <exception cref="System.ArgumentException">Thrown if <c>asteroid</c> is already registered. This object 
		/// shall not be changed in the event of an exception.</exception>
		internal void register(ProtoVessel asteroid, CustomAsteroidData data) {
			var nodeForm = new ConfigNode();
			ConfigNode.CreateConfigFromObject(data, nodeForm);
			register(asteroid, nodeForm);
		}

		/// <summary>
		/// Adds data for a particular asteroid to the registry.
		/// </summary>
		/// 
		/// <param name="asteroid">The asteroid with which the data will be permanently associated. Must 
		/// never have been loaded, and must contain exactly one part with a <c>CustomAsteroidData</c> module.</param>
		/// <param name="node">The data to store until the asteroid is loaded.</param>
		/// 
		/// <exception cref="System.ArgumentException">Thrown if <c>asteroid</c> is already registered. This object 
		/// shall not be changed in the event of an exception.</exception>
		internal void register(ProtoVessel asteroid, ConfigNode node) {
			unloadedAsteroids.Add(asteroid.vesselID, node);
		}

		/// <summary>
		/// Stops storing data for a particular asteroid. After calling this method, the registry will not contain data 
		/// for <c>asteroid</c>.
		/// </summary>
		/// <param name="asteroid">The asteroid to remove from the registry.</param>
		internal void unregister(Vessel asteroid) {
			// Dictionary.Remove() does not throw in the case of a missing key
			unloadedAsteroids.Remove(asteroid.id);
		}

		/// <summary>
		/// Returns data associated with a particular asteroid.
		/// </summary>
		/// 
		/// <param name="asteroid">The asteroid to look up. The caller is responsible for ensuring that this vessel 
		/// is, in fact, an asteroid.</param>
		/// <returns>A ConfigNode representing the CustomAsteroidData instance associated with the 
		/// 	asteroid, or a default value if no instance is stored.</returns>
		/// 
		/// <exception cref="System.NullReferenceException">Thrown if <c>asteroid</c> is null. The game state 
		/// shall be unchanged in the event of an exception.</exception>
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
				} catch (KeyNotFoundException) {
					// If the asteroid doesn't exist, keep going to the default
				}
			}

			// When all else fails, assume default asteroid type
			var node = new ConfigNode();
			ConfigNode.CreateConfigFromObject(new CustomAsteroidData(), node);
			return node;
		}

		/// <summary>
		/// Called on the frame when a script is enabled just before any of the Update methods is called the first time.
		/// See the [Unity Documentation](http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.Start.html) 
		/// for details.
		/// </summary>
		public void Start() {
			GameEvents.onVesselDestroy.Add(unregister);
			// GameEvents.onVesselGoOffRails doesn't work for some reason
			GameEvents.onPartUnpack.Add(checkAsteroid);
		}

		/// <summary>
		/// This function is called when the object will be destroyed. See the 
		/// [Unity Documentation](http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnDestroy.html) 
		/// fpr details.
		/// </summary>
		public void OnDestroy() {
			GameEvents.onVesselDestroy.Remove(unregister);
			GameEvents.onPartUnpack.Remove(checkAsteroid);
		}

		/// <summary>
		/// Function called when an asteroid is loaded.
		/// </summary>
		/// 
		/// <param name="potato">A vessel part. The method does nothing unless the part is being loaded for the 
		/// first time, and contains a <c>CustomAsteroidData</c> module.</param>
		private void checkAsteroid(Part potato) {
			if (unloadedAsteroids.ContainsKey(potato.vessel.id)
			    && potato.FindModuleImplementing<CustomAsteroidData>() != null) {
				#if DEBUG
				Debug.Log("[CustomAsteroids]: Transferring registration of asteroid " + potato.vessel.vesselName);
				#endif
				ConfigNode newData = unloadedAsteroids[potato.vessel.id];

				List<CustomAsteroidData> oldData = potato.FindModulesImplementing<CustomAsteroidData>();

				foreach (CustomAsteroidData oldModule in oldData) {
					oldModule.Load(newData);
				}
				unregister(potato.vessel);
			} // else we're good
		}

		/// <summary>
		/// Called when the module is either constructed or loaded as part of a save game. After this method returns, 
		/// the module will be initialized with any settings in <c>node</c>.
		/// </summary>
		/// 
		/// <param name="node">The ConfigNode representing this ScenarioModule.</param>
		public override void OnLoad(ConfigNode node) {
			base.OnLoad(node);

			#if DEBUG
			Debug.Log("[CustomAsteroids]: full node = " + node);
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

		/// <summary>
		/// Tests whether this object is up-to-date.
		/// </summary>
		/// 
		/// <returns><c>true</c>, if this object has been initialized, <c>false</c> otherwise.</returns>
		internal bool isLoaded() {
			return loaded;
		}

		/// <summary>
		/// Called when the save game including the module is saved. <c>node</c> is initialized with the persistent contents 
		/// of this object.
		/// </summary>
		/// <param name="node">The ConfigNode representing this ScenarioModule.</param>
		public override void OnSave(ConfigNode node) {
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
			Debug.Log("[CustomAsteroids]: saved node = " + node);
			#endif
		}
	}
}
