/** Identifies when KSP creates a new asteroid, and starts the orbit-changing module
 * @file SpawnCatcher.cs
 * @author %Starstrider42, based on a template by xEvilReeperx
 * @date Created April 9, 2014
 */

using System.Linq;
using UnityEngine;

namespace Starstrider42 {

	namespace CustomAsteroids {
		/** Class for identifying and manipulating new asteroids before they are seen by the player
		 * 
		 * @invariant At most one instance of this class exists
		 * @invariant If and only if an instance of this class edists, SpawnCatcher.CatchAsteroidSpawn() 
		 *		will be called whenever a new vessel is created
		 *
		 * @warning Assumes that Space Center, Tracking Station, and Flight are the only real-time scenes. 
		 * 		May be invalidated by the addition of a Mission Control or Observatory scene in KSP 0.24.
		 */
		internal class SpawnCatcher : MonoBehaviour {
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
			internal void catchAsteroidSpawn(Vessel vessel) {
				// Ignore asteroids "created" by undocking
				if (vessel.vesselType == VesselType.SpaceObject && vessel.loaded == false) {
					// Verify that each asteroid is caught exactly once
					Debug.Log("CustomAsteroids: caught spawn of " + vessel.GetName());

					try {
						AsteroidManager.editAsteroid(vessel);
					} catch (System.InvalidOperationException e) {
						if (e.InnerException != null) {
							Util.ErrorToPlayer("Could not place {0}. Cause: \"{1}\"\nRoot Cause: \"{2}\".", 
								vessel.GetName(), e.Message, e.GetBaseException().Message);
						} else {
							Util.ErrorToPlayer("Could not place {0}. Cause: \"{1}\".", 
								vessel.GetName(), e.Message);
						}
						Debug.LogException(e);
						// Better no asteroid than a corrupted one
						vessel.Die();
					}
				}
			}
		}

		/** Workaround to let SpawnCatcher be run in multiple specific scenes
		 * 
		 * Shamelessly stolen from Trigger Au, thanks for the idea!
		 * 
		 * Loaded on entering any Flight scene
		 */
		[KSPAddon(KSPAddon.Startup.Flight, false)]
		internal class SCFlight : SpawnCatcher {
		}
		/** Workaround to let SpawnCatcher be run in multiple specific scenes
		 * 
		 * Shamelessly stolen from Trigger Au, thanks for the idea!
		 * 
		 * Loaded on entering any SpaceCentre scene
		 */
		[KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
		internal class SCSpaceCenter : SpawnCatcher {
		}
		/** Workaround to let SpawnCatcher be run in multiple specific scenes
		 * 
		 * Shamelessly stolen from Trigger Au, thanks for the idea!
		 * 
		 * Loaded on entering any TrackingStation scene
		 */
		[KSPAddon(KSPAddon.Startup.TrackingStation, false)]
		internal class SCTrackingStation : SpawnCatcher {
		}
	}
}
