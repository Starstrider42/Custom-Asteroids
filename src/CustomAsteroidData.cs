using System.Collections.Generic;

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
		[Persistent] public string composition = "Stony";

		/// <summary>Default density from ModuleAsteroid, in tons/m^3</summary>
		[KSPField(isPersistant = true)]
		[Persistent] public float density = 0.03f;

		/// <summary>
		/// Default fraction of science recovered by transmitting back to Kerbin, from ModuleAsteroid.
		/// </summary>
		[KSPField(isPersistant = true)]
		[Persistent] public float sampleExperimentXmitScalar = 0.3f;
		/// <summary>Default sampling experiment from ModuleAsteroid.</summary>
		[KSPField(isPersistant = true)]
		[Persistent] public string sampleExperimentId = "asteroidSample";

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
			return AsteroidDataRepository.getAsteroidData(asteroid).GetValue("composition");
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
		public static string getAsteroidDensity(Vessel asteroid) {
			return AsteroidDataRepository.getAsteroidData(asteroid).GetValue("density");
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
		public static string getAsteroidXmitScalar(Vessel asteroid) {
			return AsteroidDataRepository.getAsteroidData(asteroid).GetValue("sampleExperimentXmitScalar");
		}

		/// <summary>
		/// Returns the name of the sample experiment of any asteroid, whether or not it is loaded.
		/// </summary>
		///
		/// <param name="asteroid">Asteroid.</param>
		/// <returns>A string indicating which experiment is run by sampling this asteroid. 
		/// 	In most cases, the string will equal the <c>sampleExperimentId</c> field of a loaded 
		/// 	<c>ASTEROID_CLASS</c> node, and will equal the <c>id</c> field of a loaded <c>EXPERIMENT_DEFINITION</c> 
		/// 	node, but the caller is responsible for handling values that do not match any node of 
		/// 	either type.</returns>
		public static string getAsteroidExperiment(Vessel asteroid) {
			return AsteroidDataRepository.getAsteroidData(asteroid).GetValue("sampleExperimentId");
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
			// Wait one tick to ensure ModuleAsteroid has started first
			yield return 0;

			// Science properties
			asteroid.sampleExperimentId = sampleExperimentId;
			asteroid.sampleExperimentXmitScalar = sampleExperimentXmitScalar;

			// Update mass and density consistently
			float oldDensity = asteroid.density;
			asteroid.density = density;
			asteroid.part.mass *= (density / oldDensity);
		}
	}
}
