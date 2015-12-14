using System;

namespace Starstrider42 {

	/** Simple implementation of an ordered pair.
	 * 
	 * Works around the lack of tuple support in .NET 3.5
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
		public T First { get; set; }
		/** The second element of the pair */
		public U Second { get; set; }
	}

	namespace CustomAsteroids {
		/** Contains static methods for random number distributions
		 */
		internal static class RandomDist {
			/// <summary>Caches the next normal random variate to return from <c>drawNormal()</c>.</summary>
			private static double nextNormal;
			/// <summary><c>nextNormal</c> is valid if and only if <c>isNextNormal</c> is true.</summary>
			private static bool isNextNormal;

			static RandomDist() {
				isNextNormal = false;
				nextNormal = 0.0;
			}

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
					throw new ArgumentException("weightedSample(): Cannot sample from an empty set", "weightedChoices");
				}
				double norm = 0.0;
				foreach (Pair<T,double> choice in weightedChoices) {
					if (choice.Second < 0) {
						throw new ArgumentOutOfRangeException("weightedChoices",
							"weightedSample(): The weight of any sample may not be negative (gave " 
								+ choice.Second + " for " + choice.First + ")");
					}
					norm += choice.Second;
				}
				if (norm <= 0.0) {
					throw new ArgumentOutOfRangeException("weightedChoices",
						"weightedSample(): the weights of the samples may not all be zero");
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
						"drawLogUniform(): In a uniform distribution, the first parameter must be no more than the second (gave a = " 
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
						"drawLogUniform(): In a log-uniform distribution, the first parameter must be no more than the second (gave a = " 
						+ a + ", b = " + b + ")");
				}
				if (a <= 0) {
					throw new ArgumentOutOfRangeException("a",
						"drawLogUniform(): In a log-uniform distribution, all parameters must be positive (gave a = " 
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

			/** Draws a value from an exponential distribution
			 * 
			 * @param[in] mean The mean of the distribution.
			 * 
			 * @return An exponential random variate. The return value has the same units as @p mean
			 * 
			 * @pre @p mean &ge; 0
			 * 
			 * @exception System.ArgumentOutOfRangeException Thrown if @p mean < 0
			 * 
			 * @exceptsafe This method is atomic
			 */
			internal static double drawExponential(double mean) {
				if (mean < 0.0) {
					throw new ArgumentOutOfRangeException("mean",
						"drawExponential(): An exponential distribution cannot have a negative mean (gave " + mean + ")");
				}
				// IMPORTANT: don't let anything throw beyond this point
				return -mean * Math.Log(UnityEngine.Random.value);
			}

			/// <summary>
			/// Draws a value from a Rayleigh distribution.
			/// </summary>
			/// <param name="sigma">The standard parameter of the distribution. Must not be negative.</param>
			/// <returns>A Rayleigh random variate. The return value has the same units as <c>sigma</c>.</returns>
			/// 
			/// <exception cref="System.ArgumentOutOfRangeException">Thrown if <c>sigma &lt; 0</c>.</exception>
			internal static double drawRayleigh(double sigma) {
				if (sigma < 0.0) {
					throw new ArgumentOutOfRangeException("sigma",
						"drawRayleigh(): A Rayleigh distribution cannot have a negative sigma (gave " + sigma + ")");
				}
				// IMPORTANT: don't let anything throw beyond this point
				return Math.Sqrt(-2.0*sigma*sigma*Math.Log(UnityEngine.Random.value));
			}

			/** Draws a value from a normal distribution
			 * 
			 * @param[in] mean The mean of the distribution.
			 * @param[in] stddev The standard deviation of the distribution.
			 * 
			 * @return A normal random variate. The return value has the same units as @p mean and @p stddev
			 * 
			 * @pre @p stddev &ge; 0
			 * 
			 * @exception System.ArgumentOutOfRangeException Thrown if @p stddev < 0
			 * 
			 * @exceptsafe This method is atomic
			 */
			internal static double drawNormal(double mean, double stddev) {
				if (stddev < 0.0) {
					throw new ArgumentOutOfRangeException("stddev",
						"drawNormal(): A normal distribution cannot have a negative width (gave " + stddev + ")");
				}

				if (isNextNormal) {
					isNextNormal = false;
					return mean + stddev*nextNormal;
				} else {
					// Box-Muller transform
					double u = UnityEngine.Random.value;
					double v = UnityEngine.Random.value;

					u = Math.Sqrt(-2.0 * Math.Log(u));

					nextNormal = u * Math.Cos(2 * Math.PI * v);
					isNextNormal = true;

					return mean + stddev*(u * Math.Sin(2 * Math.PI * v));
				}
			}

			/// <summary>
			/// Draws a value from a log-normal distribution. Parameters are the mean and standard 
			/// deviation of the natural log of the value.
			/// </summary>
			/// 
			/// <param name="mu">The standard position parameter of this distribution.</param>
			/// <param name="sigma">The standard width parameter of this distribution. MUST NOT be negative.</param>
			/// <returns>A lognormal random variate. The return value has the same units as <c>e<sup>mu</sup></c> 
			/// or <c>e<sup>sigma</sup></c>.</returns>
			/// 
			/// <exception cref="System.ArgumentOutOfRangeException">Thrown if <c>sigma &lt; 0</c>.</exception> 
			internal static double drawLognormal(double mu, double sigma) {
				if (sigma < 0.0) {
					throw new ArgumentOutOfRangeException("sigma",
						"drawLognormal(): A lognormal distribution cannot have a negative width (gave " + sigma + ")");
				}

				return Math.Exp(drawNormal(mu, sigma));
			}

			/// <summary>
			/// Draws a value from a Gamma distribution. The distribution is given using the shape-scale 
			/// parametrization (called α-θ by Wolfram, and k-θ by Wikipedia).
			/// </summary>
			/// <remarks>The method arguments use Wikipedia's notation.</remarks>
			/// 
			/// <param name="k">The shape parameter for the distribution. MUST be positive.</param>
			/// <param name="theta">The scale parameter for the distribution. MUST be positive.</param>
			/// <returns>A gamma random variate. The return value has the same units as <c>theta</c>.</returns>
			/// 
			/// <exception cref="System.ArgumentOutOfRangeException">Thrown if <c>k &le; 0</c> 
			/// or <c>theta &le; 0</c>.</exception> 
			internal static double drawGamma(double k, double theta) {
				if (k <= 0.0) {
					throw new ArgumentOutOfRangeException("k",
						"drawGamma(): A gamma distribution cannot have a negative shape parameter (gave k = " + k + ")");
				}
				if (theta <= 0.0) {
					throw new ArgumentOutOfRangeException("theta",
						"drawGamma(): A gamma distribution cannot have a negative scale parameter (gave theta = " + theta + ")");
				}

				// Marsaglia's method works for a broad range of parameters
				if (k >= 1) {
					double d = k - 1.0 / 3.0;
					double c = 1.0 / Math.Sqrt(9.0*d);

					double x = 0, v = 0;
					do {
						x = drawNormal(0, 1);
						double factor = 1.0 + c*x;
						v = factor * factor * factor;
					} while (v <= 0 || Math.Log(drawUniform(0,1)) >= x*x/2 + d - d*v + d*Math.Log(v));

					return theta * d * v;
				} else {
					return drawGamma(k + 1, theta) * Math.Pow(drawUniform(0, 1), 1.0/k);
				}
			}

			/// <summary>
			/// Draws a value from a Beta distribution. The distribution is given using the parametrization 
			/// where both parameters must be positive (preferred by both Wolfram and Wikipedia).
			/// </summary>
			/// 
			/// <param name="alpha">The &alpha; parameter for the distribution. MUST be positive.</param>
			/// <param name="beta">The &beta; parameter for the distribution. MUST be positive.</param>
			/// <returns>A beta random variate. The return value will be in [0, 1].</returns>
			/// 
			/// <exception cref="System.ArgumentOutOfRangeException">Thrown if <c>alpha &le; 0</c> 
			/// or <c>beta &le; 0</c>.</exception>
			internal static double drawBeta(double alpha, double beta) {
				if (alpha <= 0.0) {
					throw new ArgumentOutOfRangeException("alpha",
						"drawBeta(): A beta distribution cannot have a negative shape parameter (gave alpha = " + alpha + ")");
				}
				if (beta <= 0.0) {
					throw new ArgumentOutOfRangeException("beta",
						"drawBeta(): A beta distribution cannot have a negative shape parameter (gave beta = " + beta + ")");
				}

				// Transform from two independent Gamma variates
				double x = drawGamma(alpha, 1.0);
				double y = drawGamma(beta, 1.0);
				return x / (x + y);
			}

			/** Draws the inclination of a randomly oriented plane
			 * 
			 * @return An angle between 0&deg; and 180&deg;, weighted by the sine of the angle. 
			 * 
			 * @note This function is intended to be used with inclinations. Drawing an inclination from this 
			 * 		distribution and drawing a longitude of ascending node uniformly from [0&deg;, 360&deg;] 
			 * 		will ensure that the orbital normal faces any direction with equal probability
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			internal static double drawIsotropic() {
				return 180.0/Math.PI * Math.Acos(1 - 2 * UnityEngine.Random.value);
			}
		}
	}
}
