using System.Collections.Generic;
using UnityEngine;

namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// Manages asteroid spawning behaviour, including the choice of spawner.
	/// </summary>
	/// 
	/// @deprecated Remove this class when implementing 3rd party spawner support for version 2.0.0.
	[KSPScenario(
		ScenarioCreationOptions.AddToAllGames,
		GameScenes.SPACECENTER,
		GameScenes.TRACKSTATION,
		GameScenes.FLIGHT)]
	[System.Obsolete("Spawner should no longer be a ScenarioModule; "
		+ "this class will be replaced with a dedicated management class in 2.0.0.")]
	public class CustomAsteroidSpawner : ScenarioModule {
		/// <summary>Handles game-wide asteroid initialization.</summary>
		static CustomAsteroidSpawner() {
			// Ensure each game has different asteroids
			Random.seed = System.Guid.NewGuid().GetHashCode();
		}

		/// <summary>Handles asteroid spawning behaviour.</summary>
		private readonly AbstractSpawner spawner;

		/// <summary>Unity trick to get start/stop behaviour without a method name.</summary>
		private IEnumerator<WaitForSeconds> driverRoutine;

		/// <summary>Initializes the scenario prior to loading persistent data. Custom Asteroids options 
		/// must have already been loaded.</summary>
		internal CustomAsteroidSpawner() {
			this.driverRoutine = null;

			switch (AsteroidManager.getOptions().getSpawner()) {
			case SpawnerType.FixedRate:
				this.spawner = new FixedRateSpawner();
				break;
			case SpawnerType.Stock:
				this.spawner = new StockalikeSpawner();
				break;
			default:
				throw new System.InvalidOperationException("Unknown spawner type: "
					+ AsteroidManager.getOptions().getSpawner());
			}
		}

		/// <summary>
		/// Called on the frame when a script is first loaded, before any are enabled.
		/// </summary>
		/// 
		/// @see[Unity Documentation](http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.Awake.html)
		public override sealed void OnAwake() {
			base.OnAwake();

			// Wait until after KSP is done looping through the scenarios
			Invoke("disableStock", 0.0f);
		}

		private void disableStock() {
			// Stock spawner only needs to be unloaded when first loading the game
			// It will stay unloaded through future scene changes
			if (HighLogic.CurrentGame.RemoveProtoScenarioModule(typeof(ScenarioDiscoverableObjects))) {
				// RemoveProtoScenarioModule doesn't remove the actual Scenario
				foreach (ScenarioDiscoverableObjects scen in 
					Resources.FindObjectsOfTypeAll(typeof(ScenarioDiscoverableObjects))) {
					scen.StopAllCoroutines();
					Destroy(scen);
				}
				Debug.Log("[CustomAsteroids]: stock spawner has been shut down.");
			} else {
				#if DEBUG
				Debug.Log("[CustomAsteroids]: stock spawner not found, doing nothing.");
				#endif
			}
		}

		/// <summary>
		/// Called on the frame when a script is enabled just before any of the Update methods is called the first time.
		/// </summary>
		/// 
		/// @see[Unity Documentation](http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.Start.html)
		internal void Start() {
			Debug.Log("[CustomAsteroids]: Booting asteroid driver...");
			driverRoutine = driver();
			StartCoroutine(driverRoutine);
		}

		/// <summary>
		/// This function is called when the object will be destroyed.
		/// </summary>
		/// 
		/// @see [Unity Documentation](http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnDestroy.html)
		internal void OnDestroy() {
			if (driverRoutine != null) {
				Debug.Log("[CustomAsteroids]: Shutting down asteroid driver...");
				StopCoroutine(driverRoutine);
			}
		}

		/// <summary>Controls scheduling of asteroid discovery and loss. Actual asteroid code is 
		/// delegated to <c>spawner</c>.</summary>
		private IEnumerator<WaitForSeconds> driver() {
			Debug.Log("[CustomAsteroids]: Asteroid driver started.");
			// Loop will be terminated by StopCoroutine
			while (true) {
				float waitSeconds = spawner.asteroidCheck();
				#if DEBUG
				Debug.Log("[CustomAsteroids]: Next check in " + waitSeconds + " s.");
				#endif
				yield return new WaitForSeconds(waitSeconds);
			}
		}

		/// <summary>
		/// Update is called every frame, if the MonoBehaviour is enabled. Does not throw exceptions.
		/// </summary>
		/// 
		/// @see [Unity Documentation](http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.Update.html)
		/// @deprecated This method is kept public for backward-compatibility, but no longer does anything.
		[System.Obsolete("Improved spawn code does not work every tick; Update() no longer does anything.")]
		public void Update() {
			// This method is obsolete, but can't be removed before 2.0.0
		}

		/// <summary>
		/// Called when the module is either constructed or loaded as part of a save game. After this method returns, 
		/// the module will be initialized with any settings in <c>node</c>.
		/// </summary>
		/// <param name="node">The ConfigNode representing this ScenarioModule.</param>
		public override void OnLoad(ConfigNode node) {
			base.OnLoad(node);

			#if DEBUG
			Debug.Log("[CustomAsteroids]: full node = " + node);
			#endif
			ConfigNode thisNode = node.GetNode("SpawnState");
			if (thisNode != null) {
				ConfigNode.LoadObjectFromConfig(this, thisNode);
			}
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
			allData.name = "SpawnState";
			node.AddNode(allData);
		}
	}
}
