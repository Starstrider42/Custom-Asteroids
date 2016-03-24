namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// <para>Represents asteroid-specific information not included in the stock game.</para>
	/// </summary>
	public class CustomAsteroidData : PartModule {
		/// <summary>The name of the composition class to use.</summary>
		/// <remarks>Must be public to initialise it from part config.</remarks>
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Type")]
		public string composition = "Stony";
	}
}
