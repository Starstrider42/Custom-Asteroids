/** Identifies when KSP creates a new asteroid, and starts the orbit-changing module
 * @file SpawnCatcher.cs
 * @author Starstrider42, based on a template by xEvilReeperx
 * @date Created April 9, 2014
 */

using UnityEngine;

namespace Starstrider42 {

	namespace CustomAsteroids {
		/** Class for identifying and manipulating new asteroids before they are seen by the player
		 * 
		 * @invariant At most one instance of this class exists
		 * @invariant If and only if an instance of this class edists, SpawnCatcher.CatchAsteroidSpawn() 
		 *		will be called whenever a new vessel is created
		 */
		[KSPAddon(KSPAddon.Startup.EveryScene, false)]
		public class SpawnCatcher : MonoBehaviour 
		{
			/** Called on the frame when a script is enabled just before any of the Update methods is called the first time.
			 * 
			 * @see[Unity Documentation] (http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.Start.html)
			 * 
			 * @post SpawnCatcher.CatchAsteroidSpawn() will henceforth be called whenever a new vessel is created
			 * 
			 * @warning Unknown exception spec
			 */
			public void Start()
			{
				GameEvents.onVesselCreate.Add(CatchAsteroidSpawn);
			}

			/** This function is called when the object will be destroyed.
			 * 
			 * @see [Unity Documentation] (http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnDestroy.html)
			 * 
			 * @post SpawnCatcher.CatchAsteroidSpawn() will no longer be called whenever a new vessel is created
			 * 
			 * @warning Unknown exception spec
			 */
			public void OnDestroy() {
				// Keep things tidy, since I'm not sure when (or if) onVesselCreate gets automatically cleaned up
				GameEvents.onVesselCreate.Remove(CatchAsteroidSpawn);
			}

			/** Selects newly created asteroids and forwards them to OrbitManager for processing
			 * 
			 * @param[in] vessel A newly created ship object
			 * 
			 * @post if @p vessel is an asteroid, its orbit is modified using OrbitManager. Otherwise, 
			 * 		the function has no effect
			 * 
			 * @warning Unknown exception spec
			 */
			public void CatchAsteroidSpawn(Vessel vessel) {
				if (vessel.vesselType == VesselType.SpaceObject || vessel.vesselType == VesselType.Unknown) {
					// Verify that each asteroid is caught exactly once
					Debug.Log("CustomAsteroids: caught spawn of " + vessel.GetName());

					// track it by default, else its orbit won't be visible in mapview
					//vessel.DiscoveryInfo.SetLastObservedTime(Planetarium.GetUniversalTime());
					//vessel.DiscoveryInfo.SetLevel(DiscoveryLevels.StateVectors | DiscoveryLevels.Name | DiscoveryLevels.Presence);

					vessel.orbitDriver.orbit = OrbitManager.makeOrbit();
				}
			}
		}
	}
}
