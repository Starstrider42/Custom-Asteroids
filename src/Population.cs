using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// Represents a set of asteroids with similar stable orbits.
	/// </summary>
	/// 
	/// <remarks>To avoid breaking the persistence code, Population may not have subclasses.</remarks>
	sealed class Population {
		/// <summary>A unique name for the population.</summary>
		[Persistent] private string name;
		/// <summary>The name of asteroids belonging to this population.</summary>
		[Persistent] private string title;
		/// <summary>The name of the celestial object orbited by the asteroids.</summary>
		[Persistent] private string centralBody;
		/// <summary>The rate, in asteroids per Earth day, at which asteroids are discovered.</summary>
		[Persistent] private double spawnRate;
		/// <summary>The size (range) of orbits in this population.</summary>
		[Persistent] private  SizeRange orbitSize;
		/// <summary>The eccentricity (range) of orbits in this population.</summary>
		[Persistent] private ValueRange eccentricity;
		/// <summary>The inclination (range) of orbits in this population.</summary>
		[Persistent] private ValueRange inclination;
		/// <summary>The argument/longitude of periapsis (range) of orbits in this population.</summary>
		[Persistent] private  PeriRange periapsis;
		/// <summary>The longitude of ascending node (range) of orbits in this population.</summary>
		[Persistent] private ValueRange ascNode;
		/// <summary>The range of positions along the orbit for asteroids in this population.</summary>
		[Persistent] private PhaseRange orbitPhase;

		/// <summary>
		/// Creates a dummy population. The object is initialized to a state in which it will not be expected to 
		/// generate orbits. Any orbits that <em>are</em> generated will be located inside the Sun, causing the game 
		/// to immediately delete the object with the orbit.
		/// <para>Does not throw exceptions.</para>
		/// </summary>
		internal Population() {
			this.name = "invalid";
			this.title = "Ast.";
			this.centralBody = "Sun";
			this.spawnRate = 0.0;			// Safeguard: don't make asteroids until the values are set

			this.orbitSize = new  SizeRange(ValueRange.Distribution.LogUniform, SizeRange.SizeType.SemimajorAxis);
			this.eccentricity = new ValueRange(ValueRange.Distribution.Rayleigh, min: 0.0, max: 1.0);
			this.inclination = new ValueRange(ValueRange.Distribution.Rayleigh);
			this.periapsis = new  PeriRange(ValueRange.Distribution.Uniform, PeriRange.PeriType.Argument, 
				min: 0.0, max: 360.0);
			this.ascNode = new ValueRange(ValueRange.Distribution.Uniform, min: 0.0, max: 360.0);
			this.orbitPhase = new PhaseRange(ValueRange.Distribution.Uniform, min: 0.0, max: 360.0, 
				type: PhaseRange.PhaseType.MeanAnomaly, epoch: PhaseRange.EpochType.GameStart);
		}

		/// <summary>
		/// Generates a random orbit consistent with the population properties. The program will be in a consistent 
		/// state in the event of an exception.
		/// </summary>
		/// <returns>The orbit of a randomly selected member of the population.</returns>
		/// 
		/// <exception cref="System.InvalidOperationException">Thrown if population's parameter values cannot produce 
		/// valid orbits.</exception>
		/// 
		/// @todo Break up this function.
		internal Orbit drawOrbit() {
			// Would like to only calculate this once, but I don't know for sure that this object will 
			//		be initialized after FlightGlobals
			try {
				CelestialBody orbitee = getPlanetByName(centralBody);

				Debug.Log("[CustomAsteroids]: drawing orbit from " + name);

				// Properties with only one reasonable parametrization
				double e = eccentricity.draw();
				if (e < 0.0) {
					throw new InvalidOperationException("[CustomAsteroids]: cannot have negative eccentricity (generated "
						+ e + ")");
				}
				// Sign of inclination is redundant with 180-degree shift in longitude of ascending node
				// So it's ok to just have positive inclinations
				double i = inclination.draw();
				double lAn = ascNode.draw();		// longitude of ascending node

				// Position of periapsis
				double aPe;
				double peri = periapsis.draw();		// argument of periapsis
				switch (periapsis.getParam()) {
				case PeriRange.PeriType.Argument:
					aPe = peri;
					break;
				case PeriRange.PeriType.Longitude:
					aPe = peri - lAn;
					break;
				default:
					throw new InvalidOperationException("[CustomAsteroids]: cannot describe periapsis position using type "
						+ periapsis.getParam());
				}

				// Semimajor axis
				double a;
				double size = orbitSize.draw();
				switch (orbitSize.getParam()) {
				case SizeRange.SizeType.SemimajorAxis:
					a = size;
					break;
				case SizeRange.SizeType.Periapsis:
					a = size / (1.0 - e);
					break;
				case SizeRange.SizeType.Apoapsis:
					if (e > 1.0) {
						throw new InvalidOperationException("[CustomAsteroids]: cannot constrain apoapsis on "
							+ "unbound orbits (eccentricity " + e + ")");
					}
					a = size / (1.0 + e);
					break;
				default:
					throw new InvalidOperationException("[CustomAsteroids]: cannot describe orbit size using type "
						+ orbitSize.getParam());
				}

				// Mean anomaly at given epoch
				double mEp, epoch;
				double phase = orbitPhase.draw();
				switch (orbitPhase.getParam()) {
				case PhaseRange.PhaseType.MeanAnomaly:
					// Mean anomaly is the ONLY orbital angle that needs to be given in radians
					mEp = Math.PI / 180.0 * phase;
					break;
				case PhaseRange.PhaseType.MeanLongitude:
					mEp = Math.PI / 180.0 * longToAnomaly(phase, i, aPe, lAn);
					break;
				default:
					throw new InvalidOperationException("[CustomAsteroids]: cannot describe orbit position using type "
						+ orbitSize.getParam());
				}
				switch (orbitPhase.getEpoch()) {
				case PhaseRange.EpochType.GameStart:
					epoch = 0.0;
					break;
				case PhaseRange.EpochType.Now:
					epoch = Planetarium.GetUniversalTime();
					break;
				default:
					throw new InvalidOperationException("[CustomAsteroids]: cannot describe orbit position using type "
						+ orbitSize.getParam());
				}

				// Fix accidentally hyperbolic orbits
				if (a * (1.0 - e) < 0.0) {
					a = -a;
				}

				Debug.Log("[CustomAsteroids]: new orbit at " + a + " m, e = " + e + ", i = " + i
					+ ", aPe = " + aPe + ", lAn = " + lAn + ", mEp = " + mEp + " at epoch " + epoch);

				// Does Orbit(...) throw exceptions?
				Orbit newOrbit = new Orbit(i, e, a, lAn, aPe, mEp, epoch, orbitee);
				newOrbit.UpdateFromUT(Planetarium.GetUniversalTime());

				return newOrbit;
			} catch (ArgumentException e) {
				throw new InvalidOperationException("[CustomAsteroids]: could not create orbit", e);
			}
		}

		/// <summary>
		/// Returns the rate at which asteroids are discovered in the population. Does not throw exceptions.
		/// </summary>
		/// <returns>The number of asteroids discovered per Earth day.</returns>
		internal double getSpawnRate() {
			return spawnRate;
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current object.
		/// </summary>
		/// <returns>A simple string identifying the object.</returns>
		/// 
		/// <seealso cref="Object.ToString()"/> 
		public override string ToString() {
			return getName();
		}

		/// <summary>
		/// Returns the name of the population. Does not throw exceptions.
		/// </summary>
		/// <returns>A human-readable string identifying the population.</returns>
		internal string getName() {
			return name;
		}

		/// <summary>
		/// Returns the name of asteroids within the population. Does not throw exceptions.
		/// </summary>
		/// <returns>A human-readable string that can be used as an asteroid prefix.</returns>
		internal string getAsteroidName() {
			return title;
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
			double lAnRad = lAn * Math.PI / 180.0;
			double anRad = anom * Math.PI / 180.0;

			double sincos = Math.Cos(iRad) * Math.Sin(anRad + aPe);
			double cosOnly = Math.Cos(anRad + aPe);
			double cos = Math.Cos(anRad + aPe) / Math.Sqrt(cosOnly * cosOnly + sincos * sincos);
			double sin = Math.Cos(iRad) * Math.Sin(anRad + aPe) / Math.Sqrt(cosOnly * cosOnly + sincos * sincos);
			return 180.0 / Math.PI * (Math.Atan2(sin, cos) + lAnRad);
		}

		/// <summary>
		/// Converts an orbital longitude to an anomaly. Does not throw exceptions.
		/// </summary>
		/// 
		/// <param name="longitude">The angle between the reference direction (coordinate x-axis) and the projection 
		/// 	of a position onto the x-y plane, in degrees. May be mean, eccentric, or true longitude.</param>
		/// <param name="i">The inclination of the planet's orbital plane.</param>
		/// <param name="aPe">The argument of periapsis of the planet's orbit.</param>
		/// <param name="lAn">The longitude of ascending node of this planet's orbital plane.</param>
		/// 
		/// <returns>The angle between the periapsis point and a position in the planet's orbital plane, in degrees. 
		/// 	Will be mean, eccentric, or true anomaly, corresponding to the type of longitude provided.</returns>
		private static double longToAnomaly(double longitude, double i, double aPe, double lAn) {
			// Why doesn't KSP.Orbit have a function for this?
			// Cos[θ+ω] == Cos[i] Cos[l-Ω]/Sqrt[1 - Sin[i]^2 Cos[l-Ω]^2]
			// Sin[θ+ω] ==        Sin[l-Ω]/Sqrt[1 - Sin[i]^2 Cos[l-Ω]^2]
			double iRad = i * Math.PI / 180.0;
			double aPeRad = aPe * Math.PI / 180.0;
			double lAnRad = lAn * Math.PI / 180.0;
			double lRad = longitude * Math.PI / 180.0;

			double sincos = Math.Sin(iRad) * Math.Cos(lRad - lAnRad);
			double cos = Math.Cos(iRad) * Math.Cos(lRad - lAnRad) / Math.Sqrt(1 - sincos * sincos);
			double sin = Math.Sin(lRad - lAnRad) / Math.Sqrt(1 - sincos * sincos);
			return 180.0 / Math.PI * (Math.Atan2(sin, cos) - aPeRad);
		}

		/// <summary>
		/// Searches KSP for a celestial body. Assumes all loaded celestial bodies have unique names.
		/// </summary>
		/// 
		/// <param name="name">The exact, case-sensitive name of the celestial body to recover.</param>
		/// <returns>The celestial body named <c>name</c>.</returns>
		/// 
		/// <exception cref="ArgumentException">Thrown if no celestial body named <c>name</c> exists. The program state 
		/// will be unchanged in the event of an exception.</exception> 
		private static CelestialBody getPlanetByName(string name) {
			// Would like to only calculate this once, but I don't know for sure that this object will 
			//		be initialized after FlightGlobals
			CelestialBody theBody = FlightGlobals.Bodies.Find(body => body.name == name);
			if (theBody == null) {
				throw new ArgumentException("[CustomAsteroids]: could not find celestial body named " + name, 
					"name");
			}
			return theBody;
		}


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
		private class ValueRange : IPersistenceLoad {
			/// <summary>Parse format for planet names..</summary>
			protected const string PLANET_FORMAT = "(?<planet>.+)";
			/// <summary>Parse format for planet properties..</summary>
			protected const string PROP_FORMAT = "(?<prop>rad|soi|sma|per|apo|ecc|inc|(a|l)pe|lan|mn(a|l)0)";
			/// <summary>Parse format for planets, with properties..</summary>
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
			// Why can't compiler recognize implicit implementation?
			void IPersistenceLoad.PersistenceLoad() {
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
					throw new TypeInitializationException("Starstrider42.CustomAsteroids.Population.ValueRange", e);
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
			/// 	of ("rad", "soi", "sma", "per", "apo", "ecc", "inc", "ape", "lan", "mna0", or "mnl0"). 
			/// 	The only properties supported for Sun are "rad" and "soi".</param>
			/// <returns>The value of <c>property</c> appropriate for <c>planet</c>. Distances are given 
			/// 	in meters, angles are given in degrees.</returns>
			/// 
			/// <exception cref="ArgumentException">Thrown if no planet named <c>name</c> exists, or if 
			/// 	<c>property</c> does not have one of the allowed values. The program state shapp remain 
			/// 	unchanged in the event of an exception.</exception> 
			protected static double getPlanetProperty(string planet, string property) {
				CelestialBody body = Population.getPlanetByName(planet);

				switch (property.ToLower()) {
				case "rad":
					return body.Radius;
				case "soi":
					return body.sphereOfInfluence;
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
						return orbit.meanAnomalyAtEpoch * 180.0 / Math.PI;
					case "mnl0":
						return Population.anomalyToLong(orbit.meanAnomalyAtEpoch * 180.0 / Math.PI, 
							orbit.inclination, orbit.argumentOfPeriapsis, orbit.LAN);
					default:
						throw new ArgumentException("[CustomAsteroids]: celestial bodies do not have a " + property
							+ " value", "property");
					}
				}
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

		/// <summary>
		/// Specialization of ValueRange for orbital size parameter.
		/// </summary>
		/// 
		/// TODO: I don't think that SizeRange is a subtype of ValueRange in the Liskov sense... check!
		private class SizeRange : ValueRange {
			/// <summary>Defines the syntax for a Resonance declaration.</summary>
			/// <remarks>Unfortunately, planet name can have pretty much any character.</remarks>
			private static readonly Regex mmrDecl = new Regex(
				"Resonance\\(\\s*" + PLANET_FORMAT
				+ "\\s*,\\s*(?<m>\\d+)\\s*:\\s*(?<n>\\d+)\\s*\\)", 
				RegexOptions.IgnoreCase);

			/// <summary>The type of parameter describing the orbit.</summary>
			[Persistent] private SizeType type;

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
			internal SizeRange(Distribution dist, SizeType type = SizeType.SemimajorAxis, 
				double min = 0.0, double max = 1.0, double avg = 0.0, double stddev = 0.0)
				: base(dist, min, max, avg, stddev) {
				this.type = type;
			}

			/// <summary>
			/// Returns the parametrization used by this ValueRange. Does not throw exceptions.
			/// </summary>
			/// <returns>The orbit size parameter represented by this object.</returns>
			internal SizeType getParam() {
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
					throw new TypeInitializationException("Starstrider42.CustomAsteroids.Population.ValueRange", e);
				}
			}

			/// <summary>
			/// Converts an arbitrary string representation of an orbit size to a specific value. The string must have 
			/// one of the following formats:
			/// <list type="bullet">
			/// 	<item><description>a string representation of a floating-point number</description></item>
			/// 	<item><description>a string of the format "Ratio(&lt;Planet&gt;.&lt;stat&gt;, &lt;value&gt;)", where 
			/// 		&lt;Planet&gt; is the name of a loaded celestial body, &lt;stat&gt; is one of 
			/// 		(rad, soi, sma, per, apo, ecc, inc, ape, lpe, lan, mna0, mnl0), 
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
						throw new ArgumentException("Cannot parse '" + parsed["m"] + "' as an integer");
					}
					if (!Int32.TryParse(parsed["n"].ToString(), out n)) {
						throw new ArgumentException("Cannot parse '" + parsed["n"] + "' as an integer");
					}
					if (m <= 0 || n <= 0) {
						throw new ArgumentException("Mean-motion resonance must have positive integers (gave "
							+ m + ":" + n + ")");
					}

					return getPlanetProperty(parsed["planet"].ToString(), "sma")
					* Math.Pow((double) n / (double) m, 2.0 / 3.0);

					// Try the remaining options
				}

				return parseOrbitalElement(rawValue);
			}

			/// <summary>Defines the parametrization of orbit size that is used.</summary>
			internal enum SizeType {
				SemimajorAxis,
				Periapsis,
				Apoapsis
			}
		}

		/// <summary>
		/// Specialization of ValueRange for position of periapsis.
		/// </summary>
		/// 
		/// TODO: I don't think that PeriRange is a subtype of ValueRange in the Liskov sense... check!
		private class PeriRange : ValueRange {
			/// <summary>The type of parameter describing the orbit.</summary>
			[Persistent] private PeriType type;

			/// <summary>
			/// Assigns situation-specific default values to the PeriRange. The given values will be used by draw() 
			/// unless they are specifically overridden by a ConfigNode. Does not throw exceptions.
			/// </summary>
			/// <param name="dist">The distribution from which the value will be drawn.</param>
			/// <param name="type">The description of periapsis position that is used.</param>
			/// <param name="min">The minimum value allowed for distributions. May be unused.</param>
			/// <param name="max">The maximum value allowed for distributions. May be unused.</param>
			/// <param name="avg">The mean value returned. May be unused.</param>
			/// <param name="stddev">The standard deviation of values returned. May be unused.</param>
			internal PeriRange(Distribution dist, PeriType type = PeriType.Argument, 
				double min = 0.0, double max = 1.0, double avg = 0.0, double stddev = 0.0)
				: base(dist, min, max, avg, stddev) {
				this.type = type;
			}

			/// <summary>
			/// Returns the parametrization used by this ValueRange. Does not throw exceptions.
			/// </summary>
			/// <returns>The periapsis position parameter represented by this object.</returns>
			internal PeriType getParam() {
				return type;
			}

			/// <summary>Defines the parametrization of orbit size that is used.</summary>
			internal enum PeriType {
				Argument,
				Longitude
			}
		}

		/// <summary>
		/// Specialization of ValueRange for orbital phase parameter.
		/// </summary>
		/// 
		/// TODO: I don't think that PhaseRange is a subtype of ValueRange in the Liskov sense... check!
		private class PhaseRange : ValueRange {
			/// <summary>The type of parameter describing the orbit.</summary>
			[Persistent] private PhaseType type;
			/// <summary>The time at which the parameter should be calculated.</summary>
			[Persistent] private EpochType epoch;

			/// <summary>
			/// Assigns situation-specific default values to the ValueRange. The given values will be used by draw() 
			/// unless they are specifically overridden by a ConfigNode. Does not throw exceptions.
			/// </summary>
			/// <param name="dist">The distribution from which the value will be drawn.</param>
			/// <param name="type">The description of orbit position that is used.</param>
			/// <param name="epoch">The time at which the orbit position should be measured.</param>
			/// <param name="min">The minimum value allowed for distributions. May be unused.</param>
			/// <param name="max">The maximum value allowed for distributions. May be unused.</param>
			/// <param name="avg">The mean value returned. May be unused.</param>
			/// <param name="stddev">The standard deviation of values returned. May be unused.</param>
			internal PhaseRange(Distribution dist, 
				PhaseType type = PhaseType.MeanAnomaly, EpochType epoch = EpochType.GameStart, 
				double min = 0.0, double max = 1.0, double avg = 0.0, double stddev = 0.0)
				: base(dist, min, max, avg, stddev) {
				this.type = type;
				this.epoch = epoch;
			}

			/// <summary>
			/// Returns the parametrization used by this ValueRange. Does not throw exceptions.
			/// </summary>
			/// <returns>The orbit position parameter represented by this object.</returns>
			internal PhaseType getParam() {
				return type;
			}

			/// <summary>
			/// Returns the epoch at which the phase is evaluated. Does not throw exceptions.
			/// </summary>
			/// <returns>The epoch at which the orbital position is specified.</returns>
			internal EpochType getEpoch() {
				return epoch;
			}

			/// <summary>Defines the parametrization of orbit size that is used.</summary>
			internal enum PhaseType {
				MeanLongitude,
				MeanAnomaly
			}

			/// <summary>Defines the time at which the phase is measured.</summary>
			internal enum EpochType {
				GameStart,
				Now
			}
		}
	}
}
