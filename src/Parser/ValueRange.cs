using System;
using System.Text.RegularExpressions;

namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// Represents the set of values an orbital element may assume.
	/// </summary>
	/* 
	 * invariant: min is numerically equivalent to rawMin
	 * invariant: max is numerically equivalent to rawMax
	 * invariant: avg is numerically equivalent to rawAvg
	 * invariant: stdDev is numerically equivalent to rawStdDev
	 * 
	 * @todo Find a way to make values private!
	 */
	class ValueRange : IPersistenceLoad {
		/// <summary>Parse format for planet names.</summary>
		protected const string PLANET_FORMAT = "(?<planet>.+)";
		/// <summary>Parse format for planet properties.</summary>
		protected const string PROP_FORMAT = "(?<prop>rad|soi|sma|per|apo|ecc|inc|(a|l)pe|lan|mn(a|l)0"
		                                     + "|p(rot|sol|orb)|v(esc|orb|min|max))";
		/// <summary>Parse format for planets, with properties.</summary>
		protected const string PLANET_PROP = PLANET_FORMAT + "\\s*\\.\\s*" + PROP_FORMAT;

		// Unfortunately, planet name can have pretty much any character
		/// <summary>Defines the syntax for a Ratio declaration.</summary>
		private static readonly Regex ratioDecl = new Regex("Ratio\\(\\s*" + PLANET_PROP + "\\s*,"
			                                          + "\\s*(?<ratio>[-+.e\\d]+)\\s*\\)", 
			                                          RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
		/// <summary>Defines the syntax for an Offset declaration.</summary>
		private static readonly Regex sumDecl = new Regex("Offset\\(\\s*" + PLANET_PROP + "\\s*,"
			                                        + "\\s*(?<incr>[-+.e\\d]+)\\s*\\)", 
			                                        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

		// For some reason, ConfigNode can't load a SizeRange unless SizeRange has access to these
		//	members -- even though ConfigNodes seem to completely ignore permissions in all other cases

		/// <summary>The probability distribution from which the value is drawn.</summary>
		[Persistent] protected Distribution dist;

		/// <summary>Abstract string representation of <c>min</c>.</summary>
		[Persistent(name = "min")] protected string rawMin;
		/// <summary>The minimum allowed value (not always used).</summary>
		protected double min;

		/// <summary>Abstract string representation of <c>max</c>.</summary>
		[Persistent(name = "max")] protected string rawMax;
		/// <summary>The maximum allowed value (not always used).</summary>
		protected double max;

		/// <summary>Abstract string representation of <c>avg</c>.</summary>
		[Persistent(name = "avg")] protected string rawAvg;
		/// <summary>The average value (not always used).</summary>
		protected double avg;

		/// <summary>Abstract string representation of <c>stdDev</c>.</summary>
		[Persistent(name = "stddev")] protected string rawStdDev;
		/// <summary>The standard deviation of the values (not always used).</summary>
		protected double stdDev;

		/// <summary>
		/// Assigns situation-specific default values to the ValueRange. The given values will be used by draw() 
		/// unless they are specifically overridden by a ConfigNode. Does not throw exceptions.
		/// </summary>
		/// <param name="dist">The distribution from which the value will be drawn.</param>
		/// <param name="min">The minimum value allowed for distributions. May be unused.</param>
		/// <param name="max">The maximum value allowed for distributions. May be unused.</param>
		/// <param name="avg">The mean value returned. May be unused.</param>
		/// <param name="stdDev">The standard deviation of values returned. May be unused.</param>
		internal ValueRange(Distribution dist, double min = 0.0, double max = 1.0, 
			double avg = 0.0, double stdDev = 0.0) {
			this.dist = dist;
			this.rawMin = min.ToString();
			this.min = min;
			this.rawMax = max.ToString();
			this.max = max;
			this.rawAvg = avg.ToString();
			this.avg = avg;
			this.rawStdDev = stdDev.ToString();
			this.stdDev = stdDev;
		}

		/// <summary>
		/// Generates a random number consistent with the distribution. The program state shall be unchanged in the 
		/// event of an exception.
		/// </summary>
		/// <returns>The desired random variate. The distribution depends on this object's internal data.</returns>
		/// 
		/// <exception cref="System.InvalidOperationException">Thrown if the parameters are inappropriate 
		/// 	for the distribution, or if the distribution is invalid.</exception>
		internal double draw() {
			switch (dist) {
			case Distribution.Uniform: 
				return RandomDist.drawUniform(min, max);
			case Distribution.LogUniform: 
				return RandomDist.drawLogUniform(min, max);
			case Distribution.Gaussian: 
			case Distribution.Normal: 
				return RandomDist.drawNormal(avg, stdDev);
			case Distribution.LogNormal:
				if (avg <= 0.0) {
					throw new InvalidOperationException("Lognormal distribution must have positive mean (gave "
						+ avg + ").");
				}
				if (stdDev <= 0.0) {
					throw new InvalidOperationException("Lognormal distribution must have positive standard deviation "
						+ "(gave " + stdDev + ").");
				}
				double quad = Math.Sqrt(avg * avg + stdDev * stdDev);
				double mu = Math.Log(avg * avg / quad);
				double sigma = Math.Sqrt(2 * Math.Log(quad / avg));
				return RandomDist.drawLognormal(mu, sigma);
			case Distribution.Rayleigh: 
				return RandomDist.drawRayleigh(avg * Math.Sqrt(2.0 / Math.PI));
			case Distribution.Exponential:
				return RandomDist.drawExponential(avg);
			case Distribution.Gamma:
				if (avg <= 0.0) {
					throw new InvalidOperationException("Gamma distribution must have positive mean (gave "
						+ avg + ").");
				}
				if (stdDev <= 0.0) {
					throw new InvalidOperationException("Gamma distribution must have positive standard deviation "
						+ "(gave " + stdDev + ").");
				}
				double k = (avg / stdDev);
				k = k * k;
				double theta = stdDev * stdDev / avg;
				return RandomDist.drawGamma(k, theta);
			case Distribution.Beta:
				if (avg <= min || avg >= max) {
					throw new InvalidOperationException(
						String.Format("Beta distribution must have mean between min and max (gave {0} < {1} < {2}).", 
							min, avg, max));
				}
				if (stdDev <= 0.0) {
					throw new InvalidOperationException("Beta distribution must have positive standard deviation "
						+ "(gave " + stdDev + ").");
				}
				double scaledMean = (avg - min) / (max - min);
				double scaledStdDev = stdDev / (max - min);
				double scaledVar = scaledStdDev * scaledStdDev;
				double factor = (scaledMean - scaledMean * scaledMean - scaledVar) / scaledVar;
				double alpha = scaledMean * factor;
				double beta = (1.0 - scaledMean) * factor;
				return min + (max - min) * RandomDist.drawBeta(alpha, beta);
			case Distribution.Isotropic: 
				return RandomDist.drawIsotropic();
			default: 
				throw new InvalidOperationException("Invalid distribution specified, code " + dist);
			}
		}

		/// <summary>
		/// Callback used by ConfigNode.LoadObjectFromConfig().
		/// </summary>
		public void PersistenceLoad() {
			parseAll();
		}

		/// <summary>
		/// Ensures that any abstract entries in the config file are properly interpreted. Warning: Class 
		/// invariants should not be assumed to hold true prior to calling <c>parseAll()</c>.
		/// </summary>
		/// 
		/// <remarks><c>this.rawMin</c>, <c>this.rawMax</c>, <c>this.rawAvg</c>, and <c>this.rawStdDev</c> must 
		/// contain a representation of the desired object value prior to method invocation.</remarks>
		/// 
		/// <exception cref="TypeInitializationException">Thrown if the ConfigNode could not be interpreted 
		/// 	as a set of floating-point values. The program will be in a consistent state in the event of 
		/// 	an exception.</exception> 
		protected virtual void parseAll() {
			try {
				min = parseOrbitalElement(rawMin);
				max = parseOrbitalElement(rawMax);
				avg = parseOrbitalElement(rawAvg);
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
		/// Converts an arbitrary string representation of an orbital element to a specific value. The string must 
		/// have one of the following formats:
		/// <list type="bullet">
		/// 	<item><description>a string representation of a floating-point number</description></item>
		/// 	<item><description>a string of the format "Ratio(&lt;Planet&gt;.&lt;stat&gt;, &lt;value&gt;)", where 
		/// 		&lt;Planet&gt; is the name of a loaded celestial body, &lt;stat&gt; is one of 
		/// 		(rad, soi, sma, per, apo, ecc, inc, ape, lpe, lan, mna0, mnl0), 
		/// 		and &lt;value&gt; is a string representation of a floating-point number</description></item>
		/// 	<item><description>a string of the format "Offset(&lt;Planet&gt;.&lt;stat&gt;, &lt;value&gt;)", where 
		/// 		&lt;Planet&gt;, &lt;stat&gt;, and &lt;value&gt; are as above.</description></item>
		/// </list>
		/// </summary>
		/// 
		/// <param name="rawValue">A string representing the value.</param>
		/// 
		/// <returns>The value represented by <c>rawValue</c>.</returns>
		/// 
		/// <exception cref="ArgumentException">Thrown if <c>rawValue</c> could not be interpreted as a 
		/// floating-point value. The program state shall be unchanged in the event of an exception.</exception> 
		protected static double parseOrbitalElement(string rawValue) {
			double retVal;

			// Try a Ratio declaration
			if (ratioDecl.Match(rawValue).Groups[0].Success) {
				GroupCollection parsed = ratioDecl.Match(rawValue).Groups;
				double ratio;
				if (!Double.TryParse(parsed["ratio"].ToString(), out ratio)) {
					throw new ArgumentException("Cannot parse '" + parsed["ratio"] + "' as a floating point number");
				}
				retVal = getPlanetProperty(parsed["planet"].ToString(), parsed["prop"].ToString()) * ratio;
			} else if (sumDecl.Match(rawValue).Groups[0].Success) {
				GroupCollection parsed = sumDecl.Match(rawValue).Groups;
				double delta;
				if (!Double.TryParse(parsed["incr"].ToString(), out delta)) {
					throw new ArgumentException("Cannot parse '" + parsed["incr"] + "' as a floating point number");
				}
				retVal = getPlanetProperty(parsed["planet"].ToString(), parsed["prop"].ToString()) + delta;
			} else if (!Double.TryParse(rawValue, out retVal)) {
				throw new ArgumentException("Cannot parse '" + rawValue + "' as a floating point number", "rawValue");
			}
			return retVal;
		}

		/// <summary>
		/// Returns the desired property of a known celestial body.
		/// </summary>
		/// 
		/// <param name="planet">The exact, case-sensitive name of the celestial body. Assumes all loaded 
		/// 	celestial bodies have unique names.</param>
		/// <param name="property">The short name of the property to recover. Must be one 
		/// 	of ("rad", "soi", "sma", "per", "apo", "ecc", "inc", "ape", "lan", "mna0", "mnl0", 
		/// 	"prot", "psun", "porb", "vesc", "vorb", "vmin", or "vmax"). 
		/// 	The only properties supported for Sun are "rad", "soi", "prot", and "vesc".</param>
		/// <returns>The value of <c>property</c> appropriate for <c>planet</c>. Distances are given 
		/// 	in meters, angles are given in degrees.</returns>
		/// 
		/// <exception cref="ArgumentException">Thrown if no planet named <c>name</c> exists, or if 
		/// 	<c>property</c> does not have one of the allowed values. The program state shapp remain 
		/// 	unchanged in the event of an exception.</exception> 
		protected static double getPlanetProperty(string planet, string property) {
			CelestialBody body = AsteroidManager.getPlanetByName(planet);

			switch (property.ToLower()) {
			case "rad":
				return body.Radius;
			case "soi":
				return body.sphereOfInfluence;
			case "prot":
				return body.rotates ? body.rotationPeriod : Double.PositiveInfinity;
			case "psol":
				if (body.solarRotationPeriod) {
					return body.solarDayLength;
				} else {
					throw new ArgumentException("[CustomAsteroids]: celestial body '" + planet
						+ "' does not have a solar day", "property");
				}
			case "vesc":
				return Math.Sqrt(2 * body.gravParameter / body.Radius);
			default:
				if (body.GetOrbitDriver() == null) {
					throw new ArgumentException("[CustomAsteroids]: celestial body '" + planet
						+ "' does not have an orbit", "planet");
				}
				Orbit orbit = body.GetOrbit();

				switch (property.ToLower()) {
				case "sma": 
					return orbit.semiMajorAxis;
				case "per": 
					return orbit.PeR;
				case "apo": 
					return orbit.ApR;
				case "ecc": 
					return orbit.eccentricity;
				case "inc": 
					return orbit.inclination;
				case "ape": 
					return orbit.argumentOfPeriapsis;
				case "lpe": 
					// Ignore inclination: http://en.wikipedia.org/wiki/Longitude_of_periapsis
					return orbit.LAN + orbit.argumentOfPeriapsis;
				case "lan": 
					return orbit.LAN;
				case "mna0":
					return meanAnomalyAtUT(orbit, 0.0) * 180.0 / Math.PI;
				case "mnl0":
					return anomalyToLong(meanAnomalyAtUT(orbit, 0.0) * 180.0 / Math.PI, 
						orbit.inclination, orbit.argumentOfPeriapsis, orbit.LAN);
				case "porb":
					return orbit.period;
				case "vorb":
					// Need circumference of an ellipse; closed form does not exist
					double sum = orbit.semiMajorAxis + orbit.semiMinorAxis;
					double diff = orbit.semiMajorAxis - orbit.semiMinorAxis;
					double h = diff * diff / (sum * sum);
					double correction = 1.0;
					for (int n = 1; n < 10; n++) {
						double coeff = Util.doubleFactorial(2 * n - 1) / (Math.Pow(2, n) * Util.factorial(n)) 
							/ (2 * n - 1);
						correction += coeff * coeff * Math.Pow(h, n);
					}
					return Math.PI * sum * correction / orbit.period;
				case "vmin":
					return orbit.getOrbitalSpeedAtDistance(orbit.ApR);
				case "vmax":
					return orbit.getOrbitalSpeedAtDistance(orbit.PeR);
				default:
					throw new ArgumentException("[CustomAsteroids]: celestial bodies do not have a " + property
						+ " value", "property");
				}
			}
		}

		/// <summary>
		/// Returns the mean anomaly of the orbit at the given time.
		/// </summary>
		/// 
		/// <param name="orbit">The orbit whose anomaly is desired.</param>
		/// <param name="ut">The time at which to measure the mean anomaly.</param>
		/// <returns>The mean anomaly at the specified epoch.</returns>
		private static double meanAnomalyAtUT(Orbit orbit, double ut) {
			double fracOrbit = (ut - orbit.epoch) / orbit.period;
			return UtilMath.Clamp(orbit.meanAnomalyAtEpoch + 2.0 * Math.PI * fracOrbit, 0.0, 2.0 * Math.PI);
		}

		/// <summary>
		/// Converts an orbital anomaly to an orbital longitude. Does not throw exceptions.
		/// </summary>
		/// 
		/// <param name="anom">The angle between the periapsis point and a position in the planet's orbital 
		/// 	plane. May be mean, eccentric, or true anomaly.</param>
		/// <param name="i">The inclination of the planet's orbital plane.</param>
		/// <param name="aPe">The argument of periapsis of the planet's orbit.</param>
		/// <param name="lAn">The longitude of ascending node of this planet's orbital plane.</param>
		/// 
		/// <returns>The angle between the reference direction (coordinate x-axis) and the projection 
		/// of a position onto the x-y plane. Will be mean, eccentric, or true longitude, corresponding 
		/// to the type of anomaly provided.</returns>
		private static double anomalyToLong(double anom, double i, double aPe, double lAn) {
			// Why doesn't KSP.Orbit have a function for this?
			// Cos[l-Ω] ==        Cos[θ+ω]/Sqrt[Cos[θ+ω]^2 + Cos[i]^2 Sin[θ+ω]^2]
			// Sin[l-Ω] == Cos[i] Sin[θ+ω]/Sqrt[Cos[θ+ω]^2 + Cos[i]^2 Sin[θ+ω]^2]
			double iRad = i * Math.PI / 180.0;
			double aPeRad = aPe * Math.PI / 180.0;
			double lAnRad = lAn * Math.PI / 180.0;
			double anRad = anom * Math.PI / 180.0;

			double sincos = Math.Cos(iRad) * Math.Sin(anRad + aPeRad);
			double cosOnly = Math.Cos(anRad + aPeRad);
			double cos = Math.Cos(anRad + aPeRad) / Math.Sqrt(cosOnly * cosOnly + sincos * sincos);
			double sin = Math.Cos(iRad) * Math.Sin(anRad + aPeRad) / Math.Sqrt(cosOnly * cosOnly + sincos * sincos);
			return 180.0 / Math.PI * (Math.Atan2(sin, cos) + lAnRad);
		}

		/// <summary>Defines the type of probability distribution from which the value is drawn.</summary>
		internal enum Distribution {
			Uniform,
			LogUniform,
			Gaussian,
			Normal,
			LogNormal,
			Rayleigh,
			Exponential,
			Gamma,
			Beta,
			Isotropic
		}
	}
}