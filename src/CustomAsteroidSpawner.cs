/** Allows manual control of asteroid detections
 * @file CustomAsteroidSpawner.cs
 * @author %Starstrider42
 * @date Created May 14, 2014
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Starstrider42 {

	namespace CustomAsteroids {
		/** Manages asteroid spawning behaviour, including the choice of spawner.
		 * 
		 * @todo Remove this class when implementing 3rd party spawner support for version 2.0.0
		 */
		// This class is, for better or worse, part of the Custom Asteroids API, so it can't 
		// be properly refactored or even renamed before version 2.0.0.
		[KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT)]
		[System.Obsolete("Spawner should no longer be a ScenarioModule; this class will be replaced with a dedicated management class in 2.0.0.")]
		public class CustomAsteroidSpawner : ScenarioModule {
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
					throw new System.InvalidOperationException("Unknown spawner type: " + AsteroidManager.getOptions().getSpawner());
				}
			}

			/** Called on the frame when a script is first loaded, before any are enabled.
			 * 
			 * @see[Unity Documentation] (http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.Awake.html)
			 */
			public override sealed void OnAwake() {
				base.OnAwake();

				// Stock spawner only needs to be unloaded when first loading the game
				// It will stay unloaded through future scene changes
				if (HighLogic.CurrentGame.RemoveProtoScenarioModule(typeof(ScenarioDiscoverableObjects))) {
					// RemoveProtoScenarioModule doesn't remove the actual Scenario
					foreach(ScenarioDiscoverableObjects scen in 
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

			/** Called on the frame when a script is enabled just before any of the Update methods is called the first time.
			 * 
			 * @see[Unity Documentation] (http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.Start.html)
			 * 
			 * @todo What exceptions are thrown by StartCoroutine?
			 */
			internal void Start() {
				#if DEBUG
				Debug.Log("[CustomAsteroids]: Booting asteroid driver...");
				#endif
				driverRoutine = driver();
				StartCoroutine(driverRoutine);
			}

			/** This function is called when the object will be destroyed.
			 * 
			 * @see [Unity Documentation] (http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnDestroy.html)
			 * 
			 * @todo What exceptions are thrown by StopCoroutine?
			 */
			internal void OnDestroy() {
				if (driverRoutine != null) {
					#if DEBUG
					Debug.Log("[CustomAsteroids]: Shutting down asteroid driver...");
					#endif
					StopCoroutine(driverRoutine);
				}
			}

			private IEnumerator<WaitForSeconds> driver()
			{
				#if DEBUG
				Debug.Log("[CustomAsteroids]: Asteroid driver started.");
				#endif
				// Loop will be terminated by StopCoroutine
				while (true)
				{
					float waitSeconds = spawner.asteroidCheck();
					#if DEBUG
					Debug.Log("[CustomAsteroids]: Next check in " + waitSeconds + " s.");
					#endif
					yield return new WaitForSeconds(waitSeconds);
				}
			}

			/** Update is called every frame, if the MonoBehaviour is enabled.
			 * 
			 * @see [Unity Documentation] (http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.Update.html)
			 * 
			 * Tests whether it's time to create an asteroid
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			[System.Obsolete("Improved spawn code does not work every tick; Update() no longer does anything.")]
			public void Update() {
				// This method is obsolete, but can't be removed before 2.0.0
			}

			/** Called when the module is either constructed or loaded as part of a save game
			 * 
			 * @param[in] node The ConfigNode representing this ScenarioModule
			 * 
			 * @pre @p node is assumed to have the following format:
			 * @code{.cfg}
			 * SpawnState
			 * {
			 * 	NextAsteroidUT = 12345.6789
			 * 	Enabled = True
			 * }
			 * @endcode
			 * 
			 * @post The module is initialized with any settings in @p node
			 */
			public override void OnLoad(ConfigNode node)
			{
				base.OnLoad(node);

				#if DEBUG
				Debug.Log("[CustomAsteroids]: full node = " + node);
				#endif
				ConfigNode thisNode = node.GetNode("SpawnState");
				if (thisNode != null) {
					ConfigNode.LoadObjectFromConfig(this, thisNode);
				}
			}

			/** Called when the save game including the module is saved
			 * 
			 * @param[out] node The ConfigNode representing this ScenarioModule
			 * 
			 * @post @p node is initialized with the persistent contents of this object
			 * @post @p node has the following format:
			 * @code{.cfg}
			 * SpawnState
			 * {
			 * 	NextAsteroidUT = 12345.6789
			 * 	Enabled = True
			 * }
			 * @endcode
			 */
			public override void OnSave(ConfigNode node)
			{
				base.OnSave(node);

				ConfigNode allData = new ConfigNode();
				ConfigNode.CreateConfigFromObject(this, allData);
				allData.name = "SpawnState";
				node.AddNode(allData);
			}

			/** Handles asteroid spawning behaviour. */
			private readonly AbstractSpawner spawner;

			/** Unity trick to get start/stop behaviour without a method name. */
			private IEnumerator<WaitForSeconds> driverRoutine;
		}
	}
}
