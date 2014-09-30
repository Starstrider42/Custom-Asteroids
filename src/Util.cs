using System;

namespace Starstrider42 {

	namespace CustomAsteroids {
		public class Util {

			/** Prints an error message visible to the player
			 * @param[in] format The error message, or a composite format string for the message.
			 * @param[in] param Any parameters to place in the composite format string (optional).
			 * 
			 * @exceptsafe Does not throw exceptions
			 * 
			 * @note Based on code from RemoteTech
			 */
			public static void ErrorToPlayer(string format, params object[] param) {
				if (AsteroidManager.getOptions().getErrorReporting()) {
					ScreenMessages.PostScreenMessage(new ScreenMessage(
						String.Format("CustomAsteroids: " + format, param), 5.0f, ScreenMessageStyle.UPPER_RIGHT));
				}
			}
		}
	}
}
