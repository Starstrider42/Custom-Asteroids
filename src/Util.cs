using System;

namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// General-purpose functions that don't belong elsewhere.
	/// </summary>
	public static class Util {
		/// <summary>
		/// Prints an error message visible to the player. Does not throw exceptions.
		/// </summary>
		/// <param name="format">The error message, or a composite format string for the message.</param>
		/// <param name="param">Any parameters to place in the composite format string (optional).</param>
		/// 
		/// <remarks>Based on code from RemoteTech.</remarks>
		public static void errorToPlayer(string format, params object[] param) {
			if (AsteroidManager.getOptions().getErrorReporting()) {
				ScreenMessages.PostScreenMessage(String.Format("[CustomAsteroids]: " + format, param), 
					5.0f, ScreenMessageStyle.UPPER_RIGHT);
			}
		}
	}
}
