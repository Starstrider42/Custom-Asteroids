using System.Collections.Generic;
using UnityEngine;

namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// <para>Represents asteroid-specific information not included in the stock game.</para>
	/// 
	/// <para>When in the same part as an instance of ModuleAsteroid, this class is responsible 
	/// for updating the following non-persistent fields:
	/// <list type="bullet">
	/// <item><description>ModuleAsteroid.density</description></item>
	/// <item><description>ModuleAsteroid.sampleExperimentXmitScalar</description></item>
	/// <item><description>ModuleAsteroid.sampleExperimentId</description></item>
	/// </list>
	/// and the following persistent field:
	/// <list type="bullet">
	/// <item><description>ModuleAsteroid.prefabBaseURL</description></item>
	/// </list>
	/// </para>
	/// </summary>
	public class CustomAsteroidData : PartModule {
		// I need KSPField(isPersistant) for persistence when it's in a vessel, and
		// 	Persistent for persistence in all other cases... bleh!
		/// <summary>The name of the composition class to use.</summary>
		[KSPField(isPersistant = true, guiActive = true, guiName = "Type")]
		[Persistent] internal string composition = "Stony";

		/// <summary>Default density, in tons/m^3 (ModuleAsteroid override).</summary>
		[KSPField(isPersistant = true)]
		[Persistent] internal float density = 0.03f;
		/// <summary>
		/// Default fraction of science recovered by transmitting back to Kerbin (ModuleAsteroid override).
		/// </summary>
		[KSPField(isPersistant = true)]
		[Persistent] internal float sampleExperimentXmitScalar = 0.3f;
		/// <summary>Default sampling experiment (ModuleAsteroid override).</summary>
		[KSPField(isPersistant = true)]
		[Persistent] internal string sampleExperimentId = "asteroidSample";

		/// <summary>Returns a ConfigNode representing the unique fields of this object. It does not store events 
		/// and other forms of PartModule state, and so is not recommended for loaded modules.</summary>
		/// <remarks>This method is a general-use alternative to <see cref="PartModule.Save()"/>, which only works 
		/// if the module has been fully initialized by KSP's part handling code.</remarks>
		/// 
		/// <returns>A config node that may be used to initialize CustomAsteroidData modules as part of 
		/// a persistance file.</returns>
		internal ConfigNode toProtoConfigNode() {
			var returnNode = new ConfigNode("MODULE");
			returnNode.AddValue("name", typeof(CustomAsteroidData).Name);
			ConfigNode.CreateConfigFromObject(this, returnNode);
			#if DEBUG
			Debug.Log("CustomAsteroidData = " + returnNode);
			#endif
			return returnNode;
		}

		/// <summary>
		/// Returns the composition of any asteroid, whether or not it is loaded.
		/// </summary>
		/// 
		/// <param name="asteroid">The asteroid whose composition is desired.</param>
		/// <returns>A string denoting the asteroid class or composition. In most cases, 
		/// 	the string will equal the <c>title</c> field of a loaded <c>ASTEROID_CLASS</c> node, but 
		/// 	the caller is responsible for handling values that do not match any node.</returns>
		/// 
		/// <exception cref="System.NullReferenceException">If <c>asteroid</c> is null. The game state shall 
		/// be unchanged in the event of an exception.</exception>
		public static string getAsteroidTypeName(Vessel asteroid) {
			return getData(asteroid).composition;
		}

		/// <summary>
		/// Returns the density of any asteroid, whether or not it is loaded.
		/// </summary>
		/// 
		/// <param name="asteroid">The asteroid whose density is desired.</param>
		/// <returns>The density in tons per cubic meter. In most cases, value will equal the 
		/// 	<c>density</c> field of a loaded <c>ASTEROID_CLASS</c> node, but the caller is responsible 
		/// 	for handling values that do not match any node.</returns>
		/// 
		/// <exception cref="System.NullReferenceException">If <c>asteroid</c> is null. The game state shall 
		/// be unchanged in the event of an exception.</exception>
		public static float getAsteroidDensity(Vessel asteroid) {
			return getData(asteroid).density;
		}

		/// <summary>
		/// Returns the sample experiment transmittability of any asteroid, whether or not it is loaded.
		/// </summary>
		/// 
		/// <param name="asteroid">The asteroid whose science efficnecy is desired.</param>
		/// <returns>The fraction of science data that can be recovered without taking the sample to a lab. 
		/// 	In most cases, value will equal the <c>sampleExperimentXmitScalar</c> field of a loaded 
		/// 	<c>ASTEROID_CLASS</c> node, but the caller is responsible for handling values that do not match 
		/// 	any node.</returns>
		/// 
		/// <exception cref="System.NullReferenceException">If <c>asteroid</c> is null. The game state shall 
		/// be unchanged in the event of an exception.</exception>
		public static float getAsteroidXmitScalar(Vessel asteroid) {
			return getData(asteroid).sampleExperimentXmitScalar;
		}

		/// <summary>
		/// Returns the name of the sample experiment of any asteroid, whether or not it is loaded.
		/// </summary>
		///
		/// <param name="asteroid">The asteroid whose sampling experiment is desired.</param>
		/// <returns>A string indicating which experiment is run by sampling this asteroid. 
		/// 	In most cases, the string will equal the <c>sampleExperimentId</c> field of a loaded 
		/// 	<c>ASTEROID_CLASS</c> node, and will equal the <c>id</c> field of a loaded <c>EXPERIMENT_DEFINITION</c> 
		/// 	node, but the caller is responsible for handling values that do not match any node of 
		/// 	either type.</returns>
		public static string getAsteroidExperiment(Vessel asteroid) {
			return getData(asteroid).sampleExperimentId;
		}

		/// <summary>
		/// Finds the CustomAsteroidData module of any asteroid, whether or not it is loaded. Shall not throw exceptions.
		/// </summary>
		///
		/// <param name="asteroid">The asteroid whose data is desired.</param>
		/// <returns>A CustomAsteroidData module for the vessel. If no such module exists, will return a default 
		/// module.</returns>
		private static CustomAsteroidData getData(Vessel asteroid) {
			// Active module in loaded vessel takes precedence
			List<CustomAsteroidData> active = asteroid.FindPartModulesImplementing<CustomAsteroidData>();
			if (active != null && active.Count > 0) {
				return active[0];
			}

			// Unloaded asteroid?
			foreach (ProtoPartSnapshot part in asteroid.protoVessel.protoPartSnapshots) {
				foreach (ProtoPartModuleSnapshot module in part.modules) {
					if (module.moduleName.Equals(typeof(CustomAsteroidData).Name)) {
						ConfigNode data = module.moduleValues;
						var returnValue = new CustomAsteroidData();
						ConfigNode.LoadObjectFromConfig(returnValue, data);
						return returnValue;
					}
				}
			}

			// When all else fails, assume default asteroid type
			return new CustomAsteroidData();
		}

		/// <summary>
		/// Called when the flight starts. OnStart will be called before OnUpdate or OnFixedUpdate are ever called.
		/// </summary>
		/// 
		/// <param name="state">The game situation in which the part is loaded.</param>
		public override void OnStart(PartModule.StartState state) {
			if (state != StartState.Editor) {
				List<ModuleAsteroid> potatoList = vessel.FindPartModulesImplementing<ModuleAsteroid>();
				foreach (ModuleAsteroid ma in potatoList) {
					// Update asteroid info
					// Wait to make sure ModuleAsteroid is fully initialized first
					StartCoroutine("setAsteroid", ma);
				}
			}
		}

		/// <summary>
		/// Updates the asteroid properties.
		/// </summary>
		/// 
		/// <param name="asteroid">The ModuleAsteroid to update.</param>
		/// <returns>Controls the delay before execution resumes. See 
		/// 	[Unity documentation](http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.StartCoroutine.html)</returns>
		private System.Collections.IEnumerator setAsteroid(ModuleAsteroid asteroid) {
			// Ensure ModuleAsteroid has started first, or our values will be overwritten
			while (!asteroid.isActiveAndEnabled) {
				#if DEBUG
				Debug.Log("Waiting 1 tick...");
				#endif
				yield return 0;
			}

			// Science properties
			asteroid.sampleExperimentId = sampleExperimentId;
			asteroid.sampleExperimentXmitScalar = sampleExperimentXmitScalar;

			// Update mass and density consistently
			float oldDensity = asteroid.density;
			asteroid.density = density;
			#if DEBUG
			Debug.Log("Initial vessel mass: " + asteroid.part.mass);
			#endif
			asteroid.part.mass *= (density / oldDensity);
			#if DEBUG
			Debug.Log("Final vessel mass: " + asteroid.part.mass);
			#endif
		}
	}
}
