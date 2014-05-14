/** Identifies when KSP creates a new asteroid, and starts the orbit-changing module
 * @file SpawnCatcher.cs
 * @author %Starstrider42, based on a template by xEvilReeperx
 * @date Created April 9, 2014
 */

using System.Linq;
using UnityEngine;

namespace Starstrider42 {

	namespace CustomAsteroids {
		/** Workaround to let SpawnCatcher be run in multiple specific scenes
		 * 
		 * Shamelessly stolen from Trigger Au, thanks for the idea!
		 */
		[KSPAddon(KSPAddon.Startup.Flight, false)]
		public class SCFlight : SpawnCatcher {
		}
		/** Workaround to let SpawnCatcher be run in multiple specific scenes
		 * 
		 * Shamelessly stolen from Trigger Au, thanks for the idea!
		 */
		[KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
		public class SCSpaceCenter : SpawnCatcher {
		}
		/** Workaround to let SpawnCatcher be run in multiple specific scenes
		 * 
		 * Shamelessly stolen from Trigger Au, thanks for the idea!
		 */
		[KSPAddon(KSPAddon.Startup.TrackingStation, false)]
		public class SCTrackingStation : SpawnCatcher {
		}
				
		/** Class for identifying and manipulating new asteroids before they are seen by the player
		 * 
		 * @invariant At most one instance of this class exists
		 * @invariant If and only if an instance of this class edists, SpawnCatcher.CatchAsteroidSpawn() 
		 *		will be called whenever a new vessel is created
		 *
		 * @warning Assumes that Space Center, Tracking Station, and Flight are the only real-time scenes. 
		 * 		May be invalidated by the addition of a Mission Control or Observatory scene in KSP 0.24.
		 */
		public class SpawnCatcher : MonoBehaviour {
			/** Called on the frame when a script is enabled just before any of the Update methods is called the first time.
			 * 
			 * @see[Unity Documentation] (http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.Start.html)
			 * 
			 * @post SpawnCatcher.CatchAsteroidSpawn() will henceforth be called whenever a new vessel is created
			 * 
			 * @todo What exceptions are thrown by GameEvents.onVesselCreate.*?
			 */
			public void Start()
			{
				GameEvents.onVesselCreate.Add(catchAsteroidSpawn);

				//StartCoroutine(editStockSpawner());
				StartCoroutine("editStockSpawner");
			}

			/** This function is called when the object will be destroyed.
			 * 
			 * @see [Unity Documentation] (http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnDestroy.html)
			 * 
			 * @post SpawnCatcher.CatchAsteroidSpawn() will no longer be called whenever a new vessel is created
			 * 
			 * @todo What exceptions are thrown by GameEvents.onVesselCreate.*?
			 */
			public void OnDestroy() {
				// Keep things tidy, since I'm not sure when (or if) onVesselCreate gets automatically cleaned up
				GameEvents.onVesselCreate.Remove(catchAsteroidSpawn);
				StopCoroutine("editStockSpawner");
			}

			/** @todo Interim implementation to let me explore ScenarioDiscoverableObjects
			 * 
			 * To be superceded once manual asteroid spawning is implemented
			 */
			public System.Collections.IEnumerator editStockSpawner() {
				while (HighLogic.CurrentGame.scenarios[0].moduleRef == null) {
					yield return 0;
				}

				ScenarioDiscoverableObjects spawner = null;
				do {
					// Testing shows that loop condition is met fast enough that return 0 doesn't hurt performance
					yield return 0;
					// The spawner may be destroyed and re-created before the spawnInterval condition is met... 
					// 	Safer to do the lookup every time
					spawner = (ScenarioDiscoverableObjects)HighLogic.CurrentGame.scenarios.
						Find(scenario => scenario.moduleRef is ScenarioDiscoverableObjects).moduleRef;
					// Sometimes old scenario persists to when SpawnCatcher is reloaded... check for default value
				} while (spawner == null || spawner.spawnInterval != 15f);

				#if DEBUG
				Debug.Log("CustomAsteroids: editing spawner...");
				#endif

				spawner.minUntrackedLifetime = AsteroidManager.getUntrackedTimes().First;
				spawner.maxUntrackedLifetime = AsteroidManager.getUntrackedTimes().Second;

				if (AsteroidManager.getCustomSpawner()) {
					// Disable stock spawner
					spawner.spawnInterval = 1e10f;
					spawner.spawnGroupMinLimit = 0;
					spawner.spawnGroupMaxLimit = 0;
					Debug.Log("CustomAsteroids: spawner disabled");
				}
			}

			/** Selects newly created asteroids and forwards them to AsteroidManager for processing
			 * 
			 * @param[in] vessel A newly created ship object
			 * 
			 * @post if @p vessel is an asteroid, its properties are modified using AsteroidManager. Otherwise, 
			 * 		the function has no effect.
			 * @note if AsteroidManager cannot generate new data for the asteroid, the asteroid is destroyed. 
			 * 		This policy is intended to minimize side effects while alerting the player to the 
			 * 		presence of a bug.
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			public void catchAsteroidSpawn(Vessel vessel) {
				// Ignore asteroids "created" by undocking
				if (vessel.vesselType == VesselType.SpaceObject && vessel.loaded == false) {
					// Verify that each asteroid is caught exactly once
					Debug.Log("CustomAsteroids: caught spawn of " + vessel.GetName());

					try {
						AsteroidManager.editAsteroid(vessel);
					} catch (System.InvalidOperationException e) {
						Debug.LogException(e);
						// Destroy the asteroid as a fallback option
						vessel.Die();
					}
				}
			}
		}
	}
}
