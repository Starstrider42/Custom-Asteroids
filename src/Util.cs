using System;
using System.Reflection;

namespace Starstrider42 {

	/** Simple implementation of an ordered pair.
	 * 
	 * Works around the lack of tuple support in .NET 3.5, and can 
	 * 	do things that KeyValuePair can't
	 * 
	 * @tparam T The type of the first pair element
	 * @tparam U The type of the second pair element
	 */
	internal class Pair<T, U> {
		/** Should have a default constructor */
		public Pair() {
		}

		/** Creates a new ordered pair
		 * 
		 * @param[in] first,second The values to store
		 * 
		 * @post The new object represents the pair (first, second).
		 * 
		 * @exceptsafe Does not throw exceptions.
		 */
		public Pair(T first, U second) {
			this.First = first;
			this.Second = second;
		}

		/** The first element of the pair */
		[Persistent] public T First { get; set; }
		/** The second element of the pair */
		[Persistent] public U Second { get; set; }
	}

	namespace CustomAsteroids {
		/** General-purpose functions that don't belong elsewhere
		 */
		public static class Util {

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
