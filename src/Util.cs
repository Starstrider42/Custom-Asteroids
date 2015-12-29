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

		/// <summary>
		/// Computes the factorial of <c>n</c>.
		/// </summary>
		/// <returns><c>n!</c></returns>
		/// 
		/// <param name="n">The number whose factorial is desired.</param>
		/// 
		/// <exception cref="System.ArgumentException">Thrown if <c>n</c> is negative. The program state shall be 
		/// unchanged in the event of an exception.</exception>
		internal static double factorial(int n) {
			if (n < 0) {
				throw new ArgumentException("Negative numbers do not have factorials (gave " + n + ")", "n");
			} else if (n == 0) {
				return 1;
			} else {
				return n * factorial(n - 1);
			}
		}

		/// <summary>
		/// Computes the double factorial of <c>n</c>.
		/// </summary>
		/// <returns><c>n!!</c></returns>
		/// 
		/// <param name="n">The number whose double factorial is desired.</param>
		/// 
		/// <exception cref="System.ArgumentException">Thrown if <c>n</c> is negative. The program state shall be 
		/// unchanged in the event of an exception.</exception>
		internal static double doubleFactorial(int n) {
			if (n < 0) {
				throw new ArgumentException("Negative numbers do not have double factorials (gave " + n + ")", "n");
			} else if (n <= 1) {	// Cover both base cases at once
				return 1;
			} else {
				return n * doubleFactorial(n - 2);
			}
		}
	}
}
