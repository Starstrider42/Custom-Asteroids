using System;

namespace Starstrider42 {

	/// <summary>
	/// Simple implementation of an ordered pair. Works around the lack of tuple support in .NET 3.5.
	/// </summary>
	/// 
	/// <typeparam name="T">The type of the first pair element.</typeparam>
	/// <typeparam name="U">The type of the second pair element.</typeparam>
	internal class Pair<T, U> {
		/// <summary>The first element of the pair.</summary>
		public T first { get; set; }

		/// <summary>The second element of the pair.</summary>
		public U second { get; set; }

		/// <summary>Creates an ordered pair whose elements are null.</summary>
		public Pair() {
		}

		/// <summary>
		/// Creates a new ordered pair. The new object will represent the pair (first, second).
		/// </summary>
		/// <param name="first">The first element of the pair.</param>
		/// <param name="second">The second element of the pair.</param>
		public Pair(T first, U second) {
			this.first = first;
			this.second = second;
		}
	}

	namespace CustomAsteroids {
		/// <summary>
		/// Contains static methods for random number distributions.
		/// </summary>
		internal static class RandomDist {
			/// <summary>Caches the next normal random variate to return from <c>drawNormal()</c>.</summary>
			private static double nextNormal;
			/// <summary><c>nextNormal</c> is valid if and only if <c>isNextNormal</c> is true.</summary>
			private static bool isNextNormal;

			/// <summary>
			/// Prepares all random number generators in the class.
			/// </summary>
			static RandomDist() {
				isNextNormal = false;
				nextNormal = 0.0;
			}

			/// <summary>
			/// Randomly selects from discrete options with unequal weights. The program state shall be unchanged in 
			/// the event of an exception.
			/// </summary>
			/// 
			/// <typeparam name="T">The type of object to be selected.</typeparam>
			/// <param name="weightedChoices">A list of (<typeparamref name="T"/>, double) tuples. The first value of 
			/// 	each tuple represents one of the choices, the second the weight for that choice. The odds of 
			/// 	selecting two choices equals the ratio of the weights between them. All weights must be nonnegative, 
			/// 	and at least one must be positive.</param>
			/// <returns>The selected object.</returns>
			/// 
			/// <exception cref="System.ArgumentException">Thrown if <c>weightedChoices</c> is empty.</exception>
			/// <exception cref="System.ArgumentOutOfRangeException">Thrown if any weight is negative, or if no weight 
			/// 	is positive.</exception> 
			internal static T weightedSample<T>(System.Collections.Generic.IList<Pair<T,double>> weightedChoices) {
				if (weightedChoices.Count == 0) {
					throw new ArgumentException("weightedSample(): Cannot sample from an empty set", "weightedChoices");
				}
				double norm = 0.0;
				foreach (Pair<T,double> choice in weightedChoices) {
					if (choice.second < 0) {
						throw new ArgumentOutOfRangeException("weightedChoices",
							"weightedSample(): The weight of any sample may not be negative (gave "
							+ choice.second + " for " + choice.first + ")");
					}
					norm += choice.second;
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
					level += choice.second;
					if (level >= threshold) {
						return choice.first;
					}
				}

				// Should only get here because of rounding error when threshold = norm
				return weightedChoices[weightedChoices.Count - 1].first;
			}

			/// <summary>
			/// Returns <c>+1</c> or <c>-1</c> with equal probability. Does not throw exceptions.
			/// </summary>
			/// <returns>The value <c>+1</c> or <c>-1</c>.</returns>
			internal static double drawSign() {
				return (UnityEngine.Random.value < 0.5 ? -1.0 : +1.0);
			}

			/// <summary>
			///  Returns a value between 0 and 360. Does not throw exceptions.
			/// </summary>
			/// <returns>A uniform random variate over [0, 360).</returns>
			internal static double drawAngle() {
				return drawUniform(0.0, 360.0);
			}

			/// <summary>
			/// Draws a value from a uniform distribution.
			/// </summary>
			/// 
			/// <param name="a">The minimum value to return.</param>
			/// <param name="b">The maximum value to return.</param>
			/// <returns>A uniform random variate in the interval <c>[a, b]</c>. The return value has the same units 
			/// as <c>a</c> and <c>b</c>.</returns>
			/// 
			/// <exception cref="System.ArgumentOutOfRangeException">Thrown if <c>a &gt; b</c>. The program state shall 
			/// be unchanged in the event of an exception.</exception> 
			internal static double drawUniform(double a, double b) {
				if (b < a) {
					throw new ArgumentOutOfRangeException("a",
						"drawLogUniform(): In a uniform distribution, the first parameter must be no more than the second (gave a = "
						+ a + ", b = " + b + ")");
				}

				// IMPORTANT: don't let anything throw beyond this point

				return UnityEngine.Random.Range((float) a, (float) b);
			}

			/// <summary>
			/// Draws a value from a log-uniform distribution.
			/// </summary>
			/// 
			/// <param name="a">The minimum value to return.</param>
			/// <param name="b">The maximum value to return.</param>
			/// <returns>A log-uniform random variate in the interval <c>[a, b]</c>. The return value has the 
			/// same units as <c>a</c> and <c>b</c>.</returns>
			/// 
			/// <exception cref="System.ArgumentOutOfRangeException">Thrown if <c>a &gt; b</c>, or if either 
			/// <c>a</c> or <c>b</c> is nonpositive. The program state shall be unchanged in the event of an 
			/// exception.</exception> 
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

				return Math.Exp(UnityEngine.Random.Range((float) Math.Log(a), (float) Math.Log(b)));
			}

			/// <summary>
			/// Draws a value from an exponential distribution.
			/// </summary>
			/// 
			/// <param name="mean">The mean of the distribution. Must not be negative.</param>
			/// <returns>An exponential random variate. The return value has the same units as <c>mean</c>.</returns>
			/// 
			/// <exception cref="System.ArgumentOutOfRangeException">Thrown if <c>mean &lt; 0</c>. The program state 
			/// shall be unchanged in the event of an exception.</exception>
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
			/// <exception cref="System.ArgumentOutOfRangeException">Thrown if <c>sigma &lt; 0</c>. The program state 
			/// shall be unchanged in the event of an exception.</exception>
			internal static double drawRayleigh(double sigma) {
				if (sigma < 0.0) {
					throw new ArgumentOutOfRangeException("sigma",
						"drawRayleigh(): A Rayleigh distribution cannot have a negative sigma (gave " + sigma + ")");
				}
				// IMPORTANT: don't let anything throw beyond this point
				return Math.Sqrt(-2.0 * sigma * sigma * Math.Log(UnityEngine.Random.value));
			}

			/// <summary>
			/// Draws a value from a normal distribution.
			/// </summary>
			/// 
			/// <param name="mean">The mean of the distribution.</param>
			/// <param name="stddev">The standard deviation of the distribution. Must not be negative.</param>
			/// <returns>A normal random variate. The return value has the same units as <c>mean</c> 
			/// and <c>stddev</c>.</returns>
			/// 
			/// <exception cref="System.ArgumentOutOfRangeException">Thrown if <c>stddev &lt; 0</c>. The program state 
			/// shall be unchanged in the event of an exception.</exception> 
			internal static double drawNormal(double mean, double stddev) {
				if (stddev < 0.0) {
					throw new ArgumentOutOfRangeException("stddev",
						"drawNormal(): A normal distribution cannot have a negative width (gave " + stddev + ")");
				}

				// IMPORTANT: don't let anything throw beyond this point

				if (isNextNormal) {
					isNextNormal = false;
					return mean + stddev * nextNormal;
				} else {
					// Box-Muller transform
					double u = UnityEngine.Random.value;
					double v = UnityEngine.Random.value;

					u = Math.Sqrt(-2.0 * Math.Log(u));

					nextNormal = u * Math.Cos(2 * Math.PI * v);
					isNextNormal = true;

					return mean + stddev * (u * Math.Sin(2 * Math.PI * v));
				}
			}

			/// <summary>
			/// Draws a value from a log-normal distribution. Parameters are the mean and standard deviation of the 
			/// natural log of the value.
			/// </summary>
			/// 
			/// <param name="mu">The standard position parameter of this distribution.</param>
			/// <param name="sigma">The standard width parameter of this distribution. Must not be negative.</param>
			/// <returns>A lognormal random variate. The return value has the same units as <c>e<sup>mu</sup></c> 
			/// or <c>e<sup>sigma</sup></c>.</returns>
			/// 
			/// <exception cref="System.ArgumentOutOfRangeException">Thrown if <c>sigma &lt; 0</c>. The program state 
			/// shall be unchanged in the event of an exception.</exception> 
			internal static double drawLognormal(double mu, double sigma) {
				if (sigma < 0.0) {
					throw new ArgumentOutOfRangeException("sigma",
						"drawLognormal(): A lognormal distribution cannot have a negative width (gave " + sigma + ")");
				}

				// IMPORTANT: don't let anything throw beyond this point

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
			/// or <c>theta &le; 0</c>. The program state 
			/// shall be unchanged in the event of an exception.</exception> 
			internal static double drawGamma(double k, double theta) {
				if (k <= 0.0) {
					throw new ArgumentOutOfRangeException("k",
						"drawGamma(): A gamma distribution cannot have a negative shape parameter (gave k = "
						+ k + ")");
				}
				if (theta <= 0.0) {
					throw new ArgumentOutOfRangeException("theta",
						"drawGamma(): A gamma distribution cannot have a negative scale parameter (gave theta = "
						+ theta + ")");
				}

				// IMPORTANT: don't let anything throw beyond this point

				// Marsaglia's method works for a broad range of parameters
				if (k >= 1) {
					double d = k - 1.0 / 3.0;
					double c = 1.0 / Math.Sqrt(9.0 * d);

					double x, v;
					do {
						x = drawNormal(0, 1);
						double factor = 1.0 + c * x;
						v = factor * factor * factor;
					} while (v <= 0 || Math.Log(drawUniform(0, 1)) >= x * x / 2 + d - d * v + d * Math.Log(v));

					return theta * d * v;
				} else {
					return drawGamma(k + 1, theta) * Math.Pow(drawUniform(0, 1), 1.0 / k);
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
			/// or <c>beta &le; 0</c>. The program state 
			/// shall be unchanged in the event of an exception.</exception>
			internal static double drawBeta(double alpha, double beta) {
				if (alpha <= 0.0) {
					throw new ArgumentOutOfRangeException("alpha",
						"drawBeta(): A beta distribution cannot have a negative shape parameter (gave alpha = "
						+ alpha + ")");
				}
				if (beta <= 0.0) {
					throw new ArgumentOutOfRangeException("beta",
						"drawBeta(): A beta distribution cannot have a negative shape parameter (gave beta = "
						+ beta + ")");
				}

				// IMPORTANT: don't let anything throw beyond this point

				// Transform from two independent Gamma variates
				double x = drawGamma(alpha, 1.0);
				double y = drawGamma(beta, 1.0);
				return x / (x + y);
			}

			/// <summary>
			/// Draws the inclination of a randomly oriented plane. Does not throw exceptions. This function is 
			/// intended to be used with inclinations. Drawing an inclination from this distribution and drawing a 
			/// longitude of ascending node uniformly from <c>[0&deg;, 360&deg;)</c> will ensure that the orbital 
			/// normal faces any direction with equal probability.
			/// </summary>
			/// <returns>An angle between 0&deg; and 180&deg;, weighted by the sine of the angle.</returns>
			internal static double drawIsotropic() {
				return 180.0 / Math.PI * Math.Acos(1 - 2 * UnityEngine.Random.value);
			}
		}
	}
}
