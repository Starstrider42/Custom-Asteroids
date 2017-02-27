using System;
using System.Text.RegularExpressions;

namespace Starstrider42.CustomAsteroids {

	/// <summary>
	/// Specialization of ValueRange for orbital size parameter.
	/// </summary>
	/// 
	/// TODO: I don't think that SizeRange is a subtype of ValueRange in the Liskov sense... check!
	internal class SizeRange : ValueRange {
		/// <summary>Defines the syntax for a Resonance declaration.</summary>
		/// <remarks>Unfortunately, planet name can have pretty much any character.</remarks>
		private static readonly Regex mmrDecl = new Regex(
			"Resonance\\(\\s*" + PLANET_FORMAT
			+ "\\s*,\\s*(?<m>\\d+)\\s*:\\s*(?<n>\\d+)\\s*\\)", 
			RegexOptions.IgnoreCase);

		/// <summary>The type of parameter describing the orbit.</summary>
		[Persistent] private Type type;

		/// <summary>
		/// Assigns situation-specific default values to the ValueRange. The given values will be used by draw() 
		/// unless they are specifically overridden by a ConfigNode. Does not throw exceptions.
		/// </summary>
		/// <param name="dist">The distribution from which the value will be drawn.</param>
		/// <param name="type">The description of orbit size that is used.</param>
		/// <param name="min">The minimum value allowed for distributions. May be unused.</param>
		/// <param name="max">The maximum value allowed for distributions. May be unused.</param>
		/// <param name="avg">The mean value returned. May be unused.</param>
		/// <param name="stddev">The standard deviation of values returned. May be unused.</param>
		internal SizeRange(Distribution dist, Type type = Type.SemimajorAxis, 
			double min = 0.0, double max = 1.0, double avg = 0.0, double stddev = 0.0)
			: base(dist, min, max, avg, stddev) {
			this.type = type;
		}

		/// <summary>
		/// Returns the parametrization used by this ValueRange. Does not throw exceptions.
		/// </summary>
		/// <returns>The orbit size parameter represented by this object.</returns>
		internal Type getParam() {
			return type;
		}

		/// <summary>
		/// Ensures that any abstract entries in the config file are properly interpreted. Warning: Class invariants 
		/// should not be assumed to hold true prior to calling <c>parseAll()</c>.
		/// </summary>
		/// 
		/// <remarks><c>this.rawMin</c>, <c>this.rawMax</c>, <c>this.rawAvg</c>, and <c>this.rawStdDev</c> must 
		/// contain a representation of the desired object value prior to method invocation.</remarks>
		/// 
		/// <exception cref="TypeInitializationException">Thrown if the ConfigNode could not be interpreted 
		/// 	as a set of floating-point values. The program will be in a consistent state in the event of 
		/// 	an exception.</exception>
		protected override void parseAll() {
			try {
				min = parseOrbitSize(rawMin);
				max = parseOrbitSize(rawMax);
				avg = parseOrbitSize(rawAvg);
				stdDev = parseOrbitalElement(rawStdDev);
			} catch (ArgumentException e) {
				// Enforce basic exception guarantee, albeit clumsily
				// Double.ToString() does not throw
				rawMin = min.ToString();
				rawMax = max.ToString();
				rawAvg = avg.ToString();
				rawStdDev = stdDev.ToString();
				throw new TypeInitializationException("Starstrider42.CustomAsteroids.ValueRange", e);
			}
		}

		/// <summary>
		/// Converts an arbitrary string representation of an orbit size to a specific value. The string must have 
		/// one of the following formats:
		/// <list type="bullet">
		/// 	<item><description>a string representation of a floating-point number</description></item>
		/// 	<item><description>a string of the format "Ratio(&lt;Planet&gt;.&lt;stat&gt;, &lt;value&gt;)", where 
		/// 		&lt;Planet&gt; is the name of a loaded celestial body, &lt;stat&gt; is one of 
		/// 		(rad, soi, sma, per, apo, ecc, inc, ape, lpe, lan, mna0, mnl0, prot, psun, porb, 
		/// 		vesc, vorb, vmin, vmax), 
		/// 		and &lt;value&gt; is a string representation of a floating-point number</description></item>
		/// 	<item><description>a string of the format "Offset(&lt;Planet&gt;.&lt;stat&gt;, &lt;value&gt;)", where 
		/// 		&lt;Planet&gt;, &lt;stat&gt;, and &lt;value&gt; are as above.</description></item>
		/// 	<item><description>a string of the format "Resonance(&lt;Planet&gt;, &lt;m&gt;:&lt;n&gt;)", where 
		/// 		&lt;Planet&gt; is the name of a loaded celestial body, and &lt;m&gt; and &lt;n&gt; are string 
		/// 		representations of positive integers. In keeping with standard astronomical convention, 
		/// 		m &gt; n means an orbit inside that of &lt;Planet&gt;, while m &lt; n means an exterior 
		/// 		orbit</description></item>
		/// </list>
		/// </summary>
		/// 
		/// <param name="rawValue">A string representing the value.</param>
		/// 
		/// <returns>The value represented by <c>rawValue</c>.</returns>
		/// 
		/// <exception cref="ArgumentException">Thrown if <c>rawValue</c> could not be interpreted as a 
		/// floating-point value. The program state shall be unchanged in the event of an exception.</exception> 
		protected static double parseOrbitSize(string rawValue) {
			// Try a Ratio declaration
			GroupCollection parsed = mmrDecl.Match(rawValue).Groups;
			if (parsed[0].Success) {
				int m, n;
				if (!Int32.TryParse(parsed["m"].ToString(), out m)) {
					throw new ArgumentException($"In {rawValue}, cannot parse '{parsed["m"]}' as an integer");
				}
				if (!Int32.TryParse(parsed["n"].ToString(), out n)) {
					throw new ArgumentException($"In {rawValue}, cannot parse '{parsed["n"]}' as an integer");
				}
				if (m <= 0 || n <= 0) {
					throw new ArgumentException(
						$"Mean-motion resonance must have positive integers (gave {m}:{n} in {rawValue})");
				}

				return getPlanetProperty(parsed["planet"].ToString(), "sma")
					* Math.Pow((double) n / (double) m, 2.0 / 3.0);
			}

			return parseOrbitalElement(rawValue);
		}

		/// <summary>Defines the parametrization of orbit size that is used.</summary>
		internal enum Type {
			SemimajorAxis,
			Periapsis,
			Apoapsis
		}
	}
	
}