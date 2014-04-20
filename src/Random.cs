/** Implements random number generators appropriate for minor planet models
 * @file Random.cs
 * @author Starstrider42
 * @date Created April 9, 2014
 */

using System;

namespace Starstrider42 {

	namespace CustomAsteroids {
		// Can't use Tuples in .NET 3.5, and can't use .NET 4.0 with Unity/KSP
		internal class Pair<T, U> {
			public Pair() {
			}

			public Pair(T first, U second) {
				this.First = first;
				this.Second = second;
			}

			public T First { get; set; }
			public U Second { get; set; }
		}

		/** Contains static methods for random number distributions
		 * 
		 * @todo Move methods into Population.ValueRange, as appropriate
		 */
		internal static class RandomDist {
			/** Randomly selects from discrete options with unequal weights
			 * 
			 * @tparam T The type of object to be selected
			 * 
			 * @param[in] weightedChoices A list of (@p T, double) tuples. The first value of each tuple represents 
			 * 		one of the choices, the second the weight for that choice. The odds of selecting two choices 
			 * 		equals the ratio of the weights between them.
			 * 
			 * @return The selected object.
			 * 
			 * @pre @p weightedChoices contains at least one element
			 * @pre For each element @p x of @p weightedChoices, @p x.Second is nonnegative.
			 * @pre There exists an element @p x of @p weightedChoices where @p x.Second is positive.
			 * 
			 * @post The return value is in weightedChoices
			 * 
			 * @exception System.ArgumentException Thrown if @p weightedChoices is empty
			 * @exception System.ArgumentOutOfRangeException Thrown if any weight is negative, or if no weight is positive
			 * 
			 * @exceptsafe The method is atomic.
			 */
			internal static T weightedSample<T>(System.Collections.Generic.IList<Pair<T,double>> weightedChoices) {
				if (weightedChoices.Count == 0) {
					throw new ArgumentException("RandomDist.weightedSample(): Cannot sample from an empty set", "weightedChoices");
				}
				double norm = 0.0;
				foreach (Pair<T,double> choice in weightedChoices) {
					if (choice.Second < 0) {
						throw new ArgumentOutOfRangeException("weightedChoices",
							"RandomDist.weightedSample(): The weight of any sample may not be negative (gave " 
								+ choice.Second + " for " + choice.First + ")");
					}
					norm += choice.Second;
				}
				if (norm <= 0.0) {
					throw new ArgumentOutOfRangeException("weightedChoices",
						"RandomDist.weightedSample(): the weights of the samples may not all be zero");
				}

				// important: no exceptions beyond this point

				// assert: r is in [0, norm]
				double threshold = norm * UnityEngine.Random.value;

				// If you stack up all the weights, at what level do you hit threshold?
				double level = 0.0;
				foreach (Pair<T,double> choice in weightedChoices) {
					level += choice.Second;
					if (level >= threshold) {
						return choice.First;
					}
				}

				// Should only get here because of rounding error when threshold = norm
				return weightedChoices[weightedChoices.Count-1].First;
			}

			/** Returns +1 or -1 with equal probability
			 * 
			 * @return The value +1 or -1.
			 * 
			 * @exceptsafe Does not throw exceptions.
			 */
			internal static double drawSign() {
				return (UnityEngine.Random.value < 0.5 ? -1.0 : +1.0);
			}

			/** Returns a value between 0 and 360
			 * 
			 * @return A uniform random variate over [0, 360]
			 * 
			 * @exceptsafe Does not throw exceptions.
			 */
			internal static double drawAngle() {
				return drawUniform(0.0, 360.0);
			}

			/** Draws a value from a uniform distribution
			 * 
			 * @param[in] a,b The endpoints of the range containing the random variate.
			 * 
			 * @return A uniform random variate in the interval [@p a, @p b]. The return value has 
			 * 	the same units as @p a and @p b.
			 * 
			 * @pre @p a &le; @p b
			 * 
			 * @exception System.ArgumentOutOfRangeException Thrown if @p a > @p b
			 * 
			 * @exceptsafe This method is atomic
			 */
			internal static double drawUniform(double a, double b) {
				if (b < a) {
					throw new ArgumentOutOfRangeException("a",
						"RandomDist.drawLogUniform(): In a uniform distribution, the first parameter must be no more than the second (gave a = " 
						+ a + ", b = " + b + ")");
				}
				// IMPORTANT: don't let anything throw beyond this point

				/* Why the HELL does UnityEngine.Random use single precision?
				 * Alas, using System.Random (a linear congruential generator, of all things) 
				 *	might be an even greater evil. I'd rather take the chance that the Unity developers 
				 *	chose a sensible implementation; why else would they have their own class? */
				return UnityEngine.Random.Range((float) a,(float) b);
			}

			/** Draws a value from a log-uniform distribution
			 * 
			 * @param[in] a,b The endpoints of the range containing the random variate.
			 * 
			 * @return A log-uniform random variate in the interval [@p a, @p b]. The return value has 
			 * 	the same units as @p a and @p b.
			 * 
			 * @pre 0 < @p a &le; @p b
			 * 
			 * @exception System.ArgumentOutOfRangeException Thrown if @p a > @p b, or if 
			 *	either @p a or @p b is nonpositive
			 * 
			 * @exceptsafe This method is atomic
			 */
			internal static double drawLogUniform(double a, double b) {
				if (b < a) {
					throw new ArgumentOutOfRangeException("a",
						"RandomDist.drawLogUniform(): In a log-uniform distribution, the first parameter must be no more than the second (gave a = " 
						+ a + ", b = " + b + ")");
				}
				if (a <= 0) {
					throw new ArgumentOutOfRangeException("a",
						"RandomDist.drawLogUniform(): In a log-uniform distribution, all parameters must be positive (gave a = " 
						+ a + ", b = " + b + ")");
				}
				// IMPORTANT: don't let anything throw beyond this point
				// Note: System.Math.Log() does not throw even on invalid input

				/* Why the HELL does UnityEngine.Random use single precision?
				 * Alas, using System.Random (a linear congruential generator, of all things) 
				 *	might be an even greater evil. I'd rather take the chance that the Unity developers 
				 *	chose a sensible implementation; why else would they have their own class? */
				return Math.Exp(UnityEngine.Random.Range((float)Math.Log(a),(float)Math.Log(b)));
			}

			/** Draws a value from a Rayleigh distribution
			 * 
			 * @param[in] mean The mean of the distribution. This is not the standard 
			 * 	parametrization of the Rayleigh distribution, but it is easier to pick 
			 * 	values for
			 * 
			 * @return A Rayleigh random variate. The return value has the same units as @p mean
			 * 
			 * @pre @p mean &ge; 0
			 * 
			 * @exception System.ArgumentOutOfRangeException Thrown if @p mean < 0
			 * 
			 * @exceptsafe This method is atomic
			 */
			internal static double drawRayleigh(double mean) {
				if (mean < 0.0) {
					throw new ArgumentOutOfRangeException("mean",
						"RandomDist.drawRayleigh(): A Rayleigh distribution cannot have a negative mean (gave " + mean + ")");
				}
				double sigmaSquared = mean * mean * 2.0 / Math.PI;
				// IMPORTANT: don't let anything throw beyond this point
				return Math.Sqrt(-2.0*sigmaSquared*Math.Log(UnityEngine.Random.value));
			}
		}
	}
}
