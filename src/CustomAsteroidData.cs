/** Extends asteroid properties beyond those permitted by stock
 * @file CustomAsteroidData.cs
 * @author %Starstrider42
 */

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
			// I need KSPField(isPersistant) for persistence when it's in a vessel, and 
			// 	Persistent for persistence in all other cases... bleh!
			[KSPField(isPersistant = true, guiActive = true, guiName = "Type")]
			[Persistent] public string composition = "Stony";

			// Default density from ModuleAsteroid, in tons/m^3
			[KSPField (isPersistant = true)]
			[Persistent] public float density = 0.03f;

			// Default fraction of science recovered by transmitting back to Kerbin, from ModuleAsteroid.
			[KSPField (isPersistant = true)]
			[Persistent] public float sampleExperimentXmitScalar = 0.3f;
			// Default sampling experiment from ModuleAsteroid.
			[KSPField (isPersistant = true)]
			[Persistent] public string sampleExperimentId = "asteroidSample";

			/** Returns the composition of any asteroid, whether or not it is loaded
			 * 
			 * @param[in] asteroid The asteroid whose composition is desired.
			 * 
			 * @return A string denoting the asteroid class or composition. In most cases, 
			 * 		the string will equal the `title` field of a loaded `ASTEROID_CLASS` node, but 
			 * 		the caller is responsible for handling values that do not match any node.
			 * 
			 * @pre @p asteroid is, in fact, an asteroid.
			 * 
			 * @exception NullReferenceException Thrown if @p asteroid is null.
			 * 
			 * @exceptsafe The game state must be unchanged in the event of an exception.
			 */
			public static string getAsteroidTypeName(Vessel asteroid) {
				return AsteroidDataRepository.getAsteroidData(asteroid).GetValue("composition");
			}

			/** Returns the density of any asteroid, whether or not it is loaded
			 * 
			 * @param[in] asteroid The asteroid whose density is desired.
			 * 
			 * @return The density in tons per cubic meter. In most cases, value will equal the 
			 * 		`density` field of a loaded `ASTEROID_CLASS` node, but the caller is responsible 
			 * 		for handling values that do not match any node.
			 * 
			 * @pre @p asteroid is, in fact, an asteroid.
			 * 
			 * @exception NullReferenceException Thrown if @p asteroid is null.
			 * 
			 * @exceptsafe The game state must be unchanged in the event of an exception.
			 */
			public static string getAsteroidDensity(Vessel asteroid) {
				return AsteroidDataRepository.getAsteroidData(asteroid).GetValue("density");
			}

			/** Returns the sample experiment transmittability of any asteroid, whether or not it is loaded
			 * 
			 * @param[in] asteroid The asteroid whose composition is desired.
			 * 
			 * @return The fraction of science data that can be recovered without taking the sample to a lab. 
			 * 		In most cases, value will equal the `sampleExperimentXmitScalar` field of a loaded `ASTEROID_CLASS` 
			 * 		node, but the caller is responsible for handling values that do not match any node.
			 * 
			 * @pre @p asteroid is, in fact, an asteroid.
			 * 
			 * @exception NullReferenceException Thrown if @p asteroid is null.
			 * 
			 * @exceptsafe The game state must be unchanged in the event of an exception.
			 */
			public static string getAsteroidXmitScalar(Vessel asteroid) {
				return AsteroidDataRepository.getAsteroidData(asteroid).GetValue("sampleExperimentXmitScalar");
			}

			/** Returns the name of the sample experiment of any asteroid, whether or not it is loaded
			 * 
			 * @param[in] asteroid The asteroid whose composition is desired.
			 * 
			 * @return A string indicating which experiment is run by sampling this asteroid. 
			 * 		In most cases, the string will equal the `sampleExperimentId` field of a loaded 
			 * 		`ASTEROID_CLASS` node, and will equal the `id` field of a loaded `EXPERIMENT_DEFINITION` 
			 * 		node, but the caller is responsible for handling values that do not match any node 
			 * 		of either type.
			 * 
			 * @pre @p asteroid is, in fact, an asteroid.
			 * 
			 * @exception NullReferenceException Thrown if @p asteroid is null.
			 * 
			 * @exceptsafe The game state must be unchanged in the event of an exception.
			 */
			public static string getAsteroidExperiment(Vessel asteroid) {
				return AsteroidDataRepository.getAsteroidData(asteroid).GetValue("sampleExperimentId");
			}

			/** Called when the part starts
			 * 
			 * @param[in] state The game sitiation in which the part is loaded
			 */
			public override void OnStart(PartModule.StartState state) {
				if (state != StartState.Editor) {
					List<ModuleAsteroid> potatoList = this.vessel.FindPartModulesImplementing<ModuleAsteroid>();
					foreach (ModuleAsteroid ma in potatoList) {
						#if DEBUG
						Debug.Log("CustomAsteroids: BEFORE");
						Debug.Log("CustomAsteroids: experimentId = " + ma.sampleExperimentId);
						Debug.Log("CustomAsteroids: xmit         = " + ma.sampleExperimentXmitScalar);
						Debug.Log("CustomAsteroids: density      = " + ma.density);
						Debug.Log("CustomAsteroids: mass         = " + ma.part.mass);
						#endif

						// Update asteroid info
						// Wait to make sure ModuleAsteroid is fully initialized first
						StartCoroutine("setAsteroid", ma);
					}
				}
			}

			/** Updates the asteroid properties
			 * 
			 * @param[in] asteroid The ModuleAsteroid to update.
			 * 
			 * @return Controls the delay before execution resumes
			 * 
			 * @see [Unity documentation](http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.StartCoroutine.html)
			 * 
			 * @post The asteroid has the specified density
			 */
			private System.Collections.IEnumerator setAsteroid(ModuleAsteroid asteroid) {
				// Wait one tick to ensure ModuleAsteroid has started first
				yield return 0;

				// Science properties
				asteroid.sampleExperimentId = this.sampleExperimentId;
				asteroid.sampleExperimentXmitScalar = this.sampleExperimentXmitScalar;

				// Update mass and density consistently
				float oldDensity = asteroid.density;
				asteroid.density = this.density;
				asteroid.part.mass *= (this.density / oldDensity);

				#if DEBUG
				yield return new WaitForSeconds(5);
				Debug.Log("CustomAsteroids: FINAL");
				Debug.Log("CustomAsteroids: experimentId = " + asteroid.sampleExperimentId);
				Debug.Log("CustomAsteroids: xmit         = " + asteroid.sampleExperimentXmitScalar);
				Debug.Log("CustomAsteroids: density      = " + asteroid.density);
				Debug.Log("CustomAsteroids: mass         = " + asteroid.part.mass);
				#endif
			}
		}
	}
}
