using System;
using KSP.Localization;

namespace Starstrider42.CustomAsteroids
{
    /// <summary>
    /// General-purpose functions that don't belong elsewhere.
    /// </summary>
    public static class Util
    {
        /// <summary>
        /// Prints an error message visible to the player. Does not throw exceptions.
        /// </summary>
        /// <param name="format">The error message, or a composite format string for the
        /// message.</param>
        /// <param name="param">Any parameters to place in the composite format string
        /// (optional).</param>
        ///
        /// <remarks>Based on code from RemoteTech.</remarks>
        /// @deprecated This method does not support localization; use errorToPlayerLoc instead.
        [Obsolete ("This method does not support localization; use errorToPlayerLoc instead.")]
        public static void errorToPlayer (string format, params object [] param)
        {
            if (AsteroidManager.getOptions ().getErrorReporting ()) {
                ScreenMessages.PostScreenMessage (
                    string.Format ("[CustomAsteroids]: " + format, param),
                    5.0f, ScreenMessageStyle.UPPER_RIGHT);
            }
        }

        /// <summary>
        /// Prints an error message visible to the player. Does not throw exceptions.
        /// </summary>
        /// <param name="format">The error message, a localization tag, or a Lingoona format string
        /// for the message.</param>
        /// <param name="param">Any parameters to place in the format string (optional).</param>
        public static void errorToPlayerLoc (string format, params object [] param)
        {
            if (AsteroidManager.getOptions ().getErrorReporting ()) {
                ScreenMessages.PostScreenMessage (
                    "[CustomAsteroids]: " + Localizer.Format (format, param),
                    5.0f, ScreenMessageStyle.UPPER_RIGHT);
            }
        }

        /// <summary>
        /// Prints an exception visible to the player. Does not throw exceptions.
        /// </summary>
        /// <param name="e">The exception to report.</param>
        /// <param name="summary">Top-level description of the problem, to be given before any
        /// exception messages.</param>
        public static void errorToPlayer (Exception e, string summary)
        {
            if (e.InnerException != null) {
                errorToPlayerLoc ("#autoLOC_CustomAsteroids_ErrorChained",
                    summary, e.Message, e.GetBaseException ().Message);
            } else {
                errorToPlayerLoc ("#autoLOC_CustomAsteroids_ErrorBasic",
                    summary, e.Message);
            }
        }

        /// <summary>
        /// Computes the factorial of <c>n</c>.
        /// </summary>
        /// <returns><c>n!</c></returns>
        ///
        /// <param name="n">The number whose factorial is desired.</param>
        ///
        /// <exception cref="ArgumentException">Thrown if <c>n</c> is negative. The program
        /// state shall be unchanged in the event of an exception.</exception>
        internal static double Factorial (int n)
        {
            if (n < 0) {
                throw new ArgumentException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorFactorial", n), nameof (n));
            }
            if (n == 0) {
                return 1;
            }
            return n * Factorial (n - 1);
        }

        /// <summary>
        /// Computes the double factorial of <c>n</c>.
        /// </summary>
        /// <returns><c>n!!</c></returns>
        ///
        /// <param name="n">The number whose double factorial is desired.</param>
        ///
        /// <exception cref="ArgumentException">Thrown if <c>n</c> is negative. The program
        /// state shall be unchanged in the event of an exception.</exception>
        internal static double DoubleFactorial (int n)
        {
            if (n < 0) {
                throw new ArgumentException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_Error2Factorial", n), nameof (n));
            }
            if (n <= 1) {    // Cover both base cases at once
                return 1;
            }
            return n * DoubleFactorial (n - 2);
        }
    }
}
