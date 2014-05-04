/** Code for generating asteroid orbits
 * @file Population.cs
 * @author %Starstrider42
 * @date Created April 9, 2014
 *
 * @todo Refactor this file
 */

using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Starstrider42 {

	namespace CustomAsteroids {
		/** Represents a set of asteroids with similar orbits
		 * 
		 * @warning Population objects are typically initialized using an external traversal, rather than a 
		 * 		constructor. Therefore, traditional validity guarantees cannot be enforced. Instead, the 
		 * 		Population class makes heavier than usual use of defensive programming.
		 * 
		 * @note To avoid breaking the persistence code, Population may not have subclasses
		 */
		internal sealed class Population
		{
			/** Creates a dummy population
			 * 
			 * @post The object is initialized to a state in which it will not be expected to generate orbits. 
			 * 		Any orbits that *are* generated will be located inside the Sun, causing the game to immediately 
			 * 		delete the object with the orbit.
			 * 
			 * @exceptsafe Does not throw exceptions.
			 * 
			 * @note Required by interface of ConfigNode.LoadObjectFromConfig()
			 */
			internal Population() {
				this.name         = "INVALID";
				this.centralBody  = "Sun";
				this.spawnRate    = 0.0;			// Safeguard: don't make asteroids until the values are set

				this.orbitSize    = new  SizeRange(ValueRange.Distribution.LogUniform, SizeRange.SizeType.SemimajorAxis);
				this.eccentricity = new ValueRange(ValueRange.Distribution.Rayleigh, min: 0.0, max: 1.0);
				this.inclination  = new ValueRange(ValueRange.Distribution.Rayleigh);
				this.periapsis    = new ValueRange(ValueRange.Distribution.Uniform, min: 0.0, max: 360.0);
				this.ascNode      = new ValueRange(ValueRange.Distribution.Uniform, min: 0.0, max: 360.0);
				this.orbitPhase   = new PhaseRange(ValueRange.Distribution.Uniform, min: 0.0, max: 360.0, 
					type: PhaseRange.PhaseType.MeanAnomaly, epoch: PhaseRange.EpochType.GameStart);
			}

			/** Generates a random orbit consistent with the population properties
			 * 
			 * @return The orbit of a randomly selected member of the population
			 * 
			 * @exception System.InvalidOperationException Thrown if population's parameter values cannot produce 
			 * 		valid orbits.
			 * 
			 * @exceptsafe The program is in a consistent state in the event of an exception
			 * 
			 * @todo Break up this function
			 */
			internal Orbit drawOrbit() {
				// Would like to only calculate this once, but I don't know for sure that this object will 
				//		be initialized after FlightGlobals
				try {
					CelestialBody orbitee  = getPlanetByName(this.centralBody);

					Debug.Log("CustomAsteroids: drawing orbit from " + name);

					// Properties with only one reasonable parametrization
					double e = eccentricity.draw();
					if (e < 0.0) {
						throw new InvalidOperationException("CustomAsteroids: cannot have negative eccentricity (generated " 
							+ e + ")");
					}
					// Sign of inclination is redundant with 180-degree shift in longitude of ascending node
					// So it's ok to just have positive inclinations
					double i = inclination.draw();

					double aPe = periapsis.draw();		// argument of periapsis
					double lAn = ascNode.draw();		// longitude of ascending node

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
							throw new InvalidOperationException("CustomAsteroids: cannot constrain apoapsis on unbound orbits (eccentricity " 
							+ e + ")");
						}
						a = size / (1.0 + e);
						break;
					default:
						throw new InvalidOperationException("CustomAsteroids: cannot describe orbit size using type " 
							+ orbitSize.getParam());
					}

					// Mean anomaly at given epoch
					double mEp, epoch;
					double phase = orbitPhase.draw();
					switch (orbitPhase.getParam()) {
					case PhaseRange.PhaseType.MeanAnomaly:
						// Mean anomaly is the ONLY orbital angle that needs to be given in radians
						mEp = Math.PI/180.0 * phase;
						break;
					case PhaseRange.PhaseType.MeanLongitude:
						mEp = Math.PI/180.0 * longToAnomaly(phase, i, aPe, lAn);
						break;
					default:
						throw new InvalidOperationException("CustomAsteroids: cannot describe orbit position using type " 
							+ orbitSize.getParam());
					}
					switch (orbitPhase.getEpoch()) {
					case PhaseRange.EpochType.GameStart:
						epoch = getStartUt();
						break;
					case PhaseRange.EpochType.Now:
						epoch = Planetarium.GetUniversalTime();
						break;
					default:
						throw new InvalidOperationException("CustomAsteroids: cannot describe orbit position using type " 
							+ orbitSize.getParam());
					}

					// Fix accidentally hyperbolic orbits
					if (a * (1.0-e) < 0.0) {
						a = -a;
					}

					Debug.Log("CustomAsteroids: new orbit at " + a + " m, e = " + e + ", i = " + i 
						+ ", aPe = " + aPe + ", lAn = " + lAn + ", mEp = " + mEp + " at epoch " + epoch);

					// Does Orbit(...) throw exceptions?
					Orbit newOrbit = new Orbit(i, e, a, lAn, aPe, mEp, epoch, orbitee);
					newOrbit.UpdateFromUT(Planetarium.GetUniversalTime());

					return newOrbit;
				} catch (ArgumentException e) {
					throw new InvalidOperationException("CustomAsteroids: could not create orbit", e);
				}
			}

			/** Returns the rate at which asteroids are discovered in the population
			 * 
			 * @return The rate relative to the rates of all other populations.
			 * 
			 * @exceptsafe Does not throw exceptions.
			 */
			internal double getSpawnRate() {
				return spawnRate;
			}

			/** Returns a string that represents the current object.
			 *
			 * @return A simple string identifying the object
			 * 
			 * @see [Object.ToString()](http://msdn.microsoft.com/en-us/library/system.object.tostring%28v=vs.90%29.aspx)
			 */
			public override string ToString() {
				return getName();
			}

			/** Returns the name of the population
			 * 
			 * @return A human-readable string identifying the population. May not be unique.
			 * 
			 * @exceptsafe Does not throw exceptions.
			 */
			public string getName() {
				return name;
			}

			/** Converts an anomaly to an orbital longitude
			 * 
			 * @param[in] anom The angle between the periapsis point and a position in the planet's orbital 
			 * 		plane. May be mean, eccentric, or true anomaly.
			 * @param[in] i The inclination of the planet's orbital plane
			 * @param[in] aPe The argument of periapsis of the planet's orbit
			 * @param[in] lAn The longitude of ascending node of this planet's orbital plane
			 * 
			 * @return The angle between the reference direction (coordinate x-axis) and the projection 
			 * 		of a position onto the x-y plane. Will be mean, eccentric, or true longitude, corresponding 
			 * 		to the type of longitude provided.
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			private static double anomalyToLong(double anom, double i, double aPe, double lAn) {
				// Why doesn't KSP.Orbit have a function for this?
				// Cos[l-Ω] ==        Cos[θ+ω]/Sqrt[Cos[θ+ω]^2 + Cos[i]^2 Sin[θ+ω]^2]
				// Sin[l-Ω] == Cos[i] Sin[θ+ω]/Sqrt[Cos[θ+ω]^2 + Cos[i]^2 Sin[θ+ω]^2]
				double   iRad =    i * Math.PI/180.0;
				double aPeRad =  aPe * Math.PI/180.0;
				double lAnRad =  lAn * Math.PI/180.0;
				double  anRad = anom * Math.PI/180.0;

				double sincos  = Math.Cos(iRad) * Math.Sin(anRad + aPe);
				double cosOnly = Math.Cos(anRad + aPe);
				double cos     =                  Math.Cos(anRad + aPe)/Math.Sqrt(cosOnly*cosOnly + sincos*sincos);
				double sin     = Math.Cos(iRad) * Math.Sin(anRad + aPe)/Math.Sqrt(cosOnly*cosOnly + sincos*sincos);
				return 180.0/Math.PI * (Math.Atan2(sin, cos) + lAnRad);
			}

			/** Converts an orbital longitude to an anomaly
			 * 
			 * @param[in] longitude The angle between the reference direction (coordinate x-axis) and the projection 
			 * 		of a position onto the x-y plane, in degrees. May be mean, eccentric, or true longitude.
			 * @param[in] i The inclination of the planet's orbital plane
			 * @param[in] aPe The argument of periapsis of the planet's orbit
			 * @param[in] lAn The longitude of ascending node of this planet's orbital plane
			 * 
			 * @return The angle between the periapsis point and a position in the planet's orbital plane, in degrees. 
			 * 		Will be mean, eccentric, or true anomaly, corresponding to the type of longitude provided.
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			private static double longToAnomaly(double longitude, double i, double aPe, double lAn) {
				// Why doesn't KSP.Orbit have a function for this?
				// Cos[θ+ω] == Cos[i] Cos[l-Ω]/Sqrt[1 - Sin[i]^2 Cos[l-Ω]^2]
				// Sin[θ+ω] ==        Sin[l-Ω]/Sqrt[1 - Sin[i]^2 Cos[l-Ω]^2]
				double   iRad =         i * Math.PI/180.0;
				double aPeRad =       aPe * Math.PI/180.0;
				double lAnRad =       lAn * Math.PI/180.0;
				double   lRad = longitude * Math.PI/180.0;

				double sincos = Math.Sin(iRad) * Math.Cos(lRad - lAnRad);
				double cos    = Math.Cos(iRad) * Math.Cos(lRad - lAnRad)/Math.Sqrt(1 - sincos*sincos);
				double sin    =                  Math.Sin(lRad - lAnRad)/Math.Sqrt(1 - sincos*sincos);
				return 180.0/Math.PI * (Math.Atan2(sin, cos) - aPeRad);
			}

			/** Searches for a celestial body
			 * 
			 * @param[in] name The exact, case-sensitive name of the celestial body to recover
			 * 
			 * @return The celestial body named @p name
			 * 
			 * @pre All loaded celestial bodies have unique names
			 * 
			 * @exception ArgumentException Thrown if no planet named @p name exists
			 * 
			 * @exceptsafe This method is atomic
			 */
			private static CelestialBody getPlanetByName(string name) {
				// Would like to only calculate this once, but I don't know for sure that this object will 
				//		be initialized after FlightGlobals
				CelestialBody theBody = FlightGlobals.Bodies.Find(body => body.name == name);
				if (theBody == null) {
					throw new ArgumentException("CustomAsteroids: could not find celestial body named " + name, 
						"name");
				}
				return theBody;
			}

			/** Returns the time at the start of the game
			 * 
			 * @return If playing stock KSP, returns 0 UT. If playing Real Solar System, returns 
			 * 		`RealSolarSystem.cfg/REALSOLARSYSTEM.Epoch`
			 * 
			 * @exceptsafe Does not throw exceptions
			 */
			private static double getStartUt() {
				double epoch = 0.0;

				// Even if the RSS config file exists, ignore it if the mod itself is inactive
				if (AssemblyLoader.loadedAssemblies.Any (assemb => assemb.assembly.GetName().Name == "RealSolarSystem")) {
					UrlDir.UrlConfig[] configList = GameDatabase.Instance.GetConfigs("REALSOLARSYSTEM");
					if (configList.Length >= 1) {
						string epochSearch = configList[0].config.GetValue("Epoch");
						if (!Double.TryParse(epochSearch, out epoch)) {
							epoch = 0.0;
						}
					}
				}

				return epoch;
			}

			////////////////////////////////////////////////////////
			// Population properties

			/** The name of asteroids belonging to this population */
			[Persistent] private string name;
			/** The name of the celestial object orbited by the asteroids */
			[Persistent] private string centralBody;
			/** The rate, in asteroids per day, at which asteroids are discovered */
			[Persistent] private double spawnRate;
			/** The size (range) of orbits in this population */
			[Persistent] private  SizeRange orbitSize;
			/** The eccentricity (range) of orbits in this population */
			[Persistent] private ValueRange eccentricity;
			/** The inclination (range) of orbits in this population */
			[Persistent] private ValueRange inclination;
			/** The argument/longitude of periapsis (range) of orbits in this population */
			[Persistent] private ValueRange periapsis;
			/** The longitude of ascending node (range) of orbits in this population */
			[Persistent] private ValueRange ascNode;
			/** The range of positions along the orbit for asteroids in this population */
			[Persistent] private PhaseRange orbitPhase;


			/** Represents the set of values an orbital element may assume
			 * 
			 * The same consistency caveats as for Population apply here.
			 */
			private class ValueRange : IPersistenceLoad {
				/** Assigns situation-specific default values to the ValueRange
				 * 
				 * @param[in] dist The distribution from which the value will be drawn
				 * @param[in] min,max The minimum and maximum values allowed for distributions. May be unused.
				 * @param[in] avg The mean value returned. May be unused.
				 * @param[in] stdDev The standard deviation of values returned. May be unused.
				 *
				 * @post The given values will be used by draw() unless they are specifically overridden by a ConfigNode.
				 * 
				 * @exceptsafe Does not throw exceptions
				 */
				internal ValueRange(Distribution dist, double min = 0.0, double max = 1.0, 
					double avg = 0.0, double stdDev = 0.0) {
					this.dist      = dist;
					this.rawMin    = min.ToString();
					this.min       = min;
					this.rawMax    = max.ToString();
					this.max       = max;
					this.rawAvg    = avg.ToString();
					this.avg       = avg;
					this.rawStdDev = stdDev.ToString();
					this.stdDev    = stdDev;
				}

				/** Generates a random number consistent with the distribution
				 *
				 * @return The desired random variate. The distribution depends on this object's internal data.
				 * 
				 * @exception System.InvalidOperationException Thrown if the parameters are inappropriate 
				 * 		for the distribution, or if the distribution is invalid.
				 * 
				 * @exceptsafe This method is atomic
				 */
				internal double draw() {
					switch (dist) {
					case Distribution.Uniform: 
						return RandomDist.drawUniform(min, max);
					case Distribution.LogUniform: 
						return RandomDist.drawLogUniform(min, max);
					case Distribution.Rayleigh: 
						return RandomDist.drawRayleigh(avg);
					default: 
						throw new InvalidOperationException("Invalid distribution specified, code " + dist);
					}
				}

				/** Callback used by ConfigNode.LoadObjectFromConfig()
				 */
				void IPersistenceLoad.PersistenceLoad() {
					parseAll();
				}

				/** Ensures that any abstract entries in the config file are properly interpreted
				 * 
				 * @pre @p this.rawMin, @p this.rawMax, @p this.rawAvg, and @p this.rawStdDev contain a 
				 * 	representation of the desired object value
				 * 
				 * @warning Class invariant should not be assumed to hold true prior to calling PersistenceLoad()
				 * 
				 * @exception TypeInitializationException Thrown if the ConfigNode could not be interpreted 
				 * 		as a set of floating-point values
				 * 
				 * @exceptsafe The program is in a consistent state in the event of an exception
				 */
				protected virtual void parseAll() {
					try {
						min    = parseOrbitalElement(rawMin   );
						max    = parseOrbitalElement(rawMax   );
						avg    = parseOrbitalElement(rawAvg   );
						stdDev = parseOrbitalElement(rawStdDev);
					} catch (ArgumentException e) {
						// Enforce basic exception guarantee, albeit clumsily
						// Double.ToString() does not throw
						rawMin    = min.ToString();
						rawMax    = max.ToString();
						rawAvg    = avg.ToString();
						rawStdDev = stdDev.ToString();
						throw new TypeInitializationException("Starstrider42.CustomAsteroids.Population.ValueRange", e);
					}
				}

				/** Converts an arbitrary string representation of an orbital element to a specific value
				 * 
				 * @param[in] rawValue A string representing the value.
				 * 
				 * @return The value represented by @p rawValue.
				 * 
				 * @pre rawValue has one of the following formats:
				 * 		- a string representation of a floating-point number
				 * 		- a string of the format "Ratio(<Planet>.<stat>, <value>)", where <Planet> is the 
				 * 			name of a loaded celestial body, <stat> is one of (sma, per, apo, ecc, inc, ape, lpe, lan, mna0, mnl0), 
				 * 			and <value> is a string representation of a floating-point number
				 * 		- a string of the format "Offset(<Planet>.<stat>, <value>)", where <Planet>, <stat>, 
				 * 			and <value> are as above.
				 * 
				 * @exception ArgumentException Thrown if @p rawValue could not be interpreted as a floating-point value
				 * 
				 * @exceptsafe This method is atomic.
				 */
				protected static double parseOrbitalElement(string rawValue) {
					double retVal;

					// Try a Ratio declaration
					if (ratioDecl.Match(rawValue).Groups[0].Success) {
						GroupCollection parsed = ratioDecl.Match(rawValue).Groups;
						double ratio;
						if (!Double.TryParse(parsed["ratio"].ToString(), out ratio)) {
							throw new ArgumentException ("Cannot parse '" + parsed["ratio"] + "' as a floating point number");
						}
						retVal = getPlanetProperty(parsed["planet"].ToString(), parsed["prop"].ToString()) * ratio;
					
					// Try an Offset declaration
					} else if (sumDecl.Match(rawValue).Groups[0].Success) {
						GroupCollection parsed = sumDecl.Match(rawValue).Groups;
						double delta;
						if (!Double.TryParse(parsed["incr"].ToString(), out delta)) {
							throw new ArgumentException ("Cannot parse '" + parsed["incr"] + "' as a floating point number");
						}
						retVal = getPlanetProperty(parsed["planet"].ToString(), parsed["prop"].ToString()) + delta;
					
					// Finally, try a floating-point literal
					} else if (!Double.TryParse(rawValue, out retVal)) {
						throw new ArgumentException ("Cannot parse '" + rawValue + "' as a floating point number", "rawValue");
					}
					return retVal;
				}

				/** Returns the desired property of a known celestial body
				 * 
				 * @param[in] planet The exact, case-sensitive name of the celestial body
				 * @param[in] property The short name of the property to recover. Must be one 
				 * 		of ("sma", "per", "apo", "ecc", "inc", "ape", "lan", "mna0", or "mnl0").
				 * 
				 * @return The value of @p property appropriate for @p planet. Distances are given 
				 * 		in meters, angles are given in degrees.
				 * 
				 * @pre All loaded celestial bodies have unique names
				 * 
				 * @exception ArgumentException Thrown if no planet named @p name exists, or if 
				 * 		@p property does not have one of the allowed values
				 * 
				 * @exceptsafe This method is atomic
				 */
				protected static double getPlanetProperty(string planet, string property) {
					Orbit orbit = Population.getPlanetByName(planet).GetOrbit();

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
						return orbit.meanAnomalyAtEpoch * 180.0/Math.PI;
					case "mnl0":
						return Population.anomalyToLong(orbit.meanAnomalyAtEpoch * 180.0/Math.PI, 
							orbit.inclination, orbit.argumentOfPeriapsis, orbit.LAN);
					default:
						throw new ArgumentException("CustomAsteroids: celestial bodies do not have a " + property + " value", 
							"property");
					}
				}

				/** Defines the type of probability distribution from which the value is drawn
				 */
				internal enum Distribution {Uniform, LogUniform, Rayleigh};

				// Unfortunately, planet name can have pretty much any character
				private static Regex ratioDecl = new Regex("Ratio\\(\\s*(?<planet>.+)\\s*\\.\\s*" 
					+ "(?<prop>sma|per|apo|ecc|inc|ape|lpe|lan|(mna|mnl)0)\\s*," 
					+ "\\s*(?<ratio>[-+.e\\d]+)\\s*\\)", 
					RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
				private static Regex sumDecl = new Regex("Offset\\(\\s*(?<planet>.+)\\s*\\.\\s*" 
					+ "(?<prop>sma|per|apo|ecc|inc|ape|lpe|lan|(mna|mnl)0)\\s*," 
					+ "\\s*(?<incr>[-+.e\\d]+)\\s*\\)", 
					RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

				// For some reason, ConfigNode can't load a SizeRange unless SizeRange has access to these 
				//	members -- even though ConfigNodes seem to completely ignore permissions in all other cases

				/** The probability distribution from which the value is drawn */
				[Persistent] protected Distribution dist;

				/** @class ValueRange
				 * @invariant @p min is numerically equivalent to @p rawMin
				 * @invariant @p max is numerically equivalent to @p rawMax
				 * @invariant @p avg is numerically equivalent to @p rawAvg
				 * @invariant @p stdDev is numerically equivalent to @p rawStdDev
				 * 
				 * @todo Find a way to make values private!
				 */
				/** Abstract string representation of @p min */
				[Persistent(name="min")] protected string rawMin;
				/** The minimum allowed value (not always used) */
				protected double min;

				/** Abstract string representation of @p max */
				[Persistent(name="max")] protected string rawMax;
				/** The maximum allowed value (not always used) */
				protected double max;

				/** Abstract string representation of @p avg */
				[Persistent(name="avg")] protected string rawAvg;
				/** The average value (not always used) */
				protected double avg;

				/** Abstract string representation of @p stdDev */
				[Persistent(name="stddev")] protected string rawStdDev;
				/** The standard deviation of the values (not always used) */
				protected double stdDev;
			}

			/** Specialization of ValueRange for orbital size parameter.
			 * 
			 * @todo I don't think that SizeRange is a subtype of ValueRange in the Liskov sense... check!
			 */
			private class SizeRange : ValueRange {
				/** Assigns situation-specific default values to the ValueRange
				 * 
				 * @param[in] dist The distribution from which the value will be drawn
				 * @param[in] type The description of orbit size that is used
				 * @param[in] min,max The minimum and maximum values allowed for distributions. May be unused.
				 * @param[in] avg The mean value returned. May be unused.
				 * @param[in] stddev The standard deviation of values returned. May be unused.
				 * 
				 * @post The given values will be used by draw() unless they are specifically overridden by a ConfigNode.
				 * 
				 * @exceptsafe Does not throw exceptions
				 */
				internal SizeRange(Distribution dist, SizeType type = SizeType.SemimajorAxis, 
						double min = 0.0, double max = 1.0, double avg = 0.0, double stddev = 0.0) 
						: base(dist, min, max, avg, stddev) {
					this.type = type;
				}

				/** Returns the parametrization used by this ValueRange
				 * 
				 * @return The orbit size parameter represented by this object.
				 * 
				 * @exceptsafe Does not throw exceptions.
				 */
				internal SizeType getParam() {
					return type;
				}

				/** Ensures that any abstract entries in the config file are properly interpreted
				 * 
				 * @pre @p this.rawMin, @p this.rawMax, @p this.rawAvg, and @p this.rawStdDev contain a 
				 * 	representation of the desired object value
				 * 
				 * @warning Class invariant should not be assumed to hold true prior to calling PersistenceLoad()
				 * 
				 * @exception TypeInitializationException Thrown if the ConfigNode could not be interpreted 
				 * 		as a set of floating-point values
				 * 
				 * @exceptsafe The program is in a consistent state in the event of an exception
				 */
				protected override void parseAll() {
					try {
						min    = parseOrbitSize(     rawMin   );
						max    = parseOrbitSize(     rawMax   );
						avg    = parseOrbitSize(     rawAvg   );
						stdDev = parseOrbitalElement(rawStdDev);
					} catch (ArgumentException e) {
						// Enforce basic exception guarantee, albeit clumsily
						// Double.ToString() does not throw
						rawMin    = min.ToString();
						rawMax    = max.ToString();
						rawAvg    = avg.ToString();
						rawStdDev = stdDev.ToString();
						throw new TypeInitializationException("Starstrider42.CustomAsteroids.Population.ValueRange", e);
					}
				}

				/** Converts an arbitrary string representation of an orbit size to a specific value
				 * 
				 * @param[in] rawValue A string representing the value.
				 * 
				 * @return The value represented by @p rawValue.
				 * 
				 * @pre rawValue has one of the following formats:
				 * 		- a string representation of a floating-point number
				 * 		- a string of the format "Ratio(<Planet>.<stat>, <value>)", where <Planet> is the 
				 * 			name of a loaded celestial body, <stat> is one of (sma, per, apo, ecc, inc, ape, lan), 
				 * 			and <value> is a string representation of a floating-point number
				 * 		- a string of the format "Resonance(<Planet>, <m>:<n>)", where <Planet> is the 
				 * 			name of a loaded celestial body, and <m> and <n> are string representations 
				 * 			of positive integers. In keeping with standard astronomical convention, m > n means 
				 * 			an orbit inside that of <Planet>, while m < n means an exterior orbit
				 * 
				 * @exception ArgumentException Thrown if @p rawValue could not be interpreted as a floating-point value
				 * 
				 * @exceptsafe This method is atomic.
				 */
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
						* Math.Pow((double)n/(double)m, 2.0/3.0);

					// Try the remaining options
					} else {
						return parseOrbitalElement(rawValue);
					}
				}

				// Unfortunately, planet name can have pretty much any character
				private static Regex mmrDecl = new Regex(
					"Resonance\\(\\s*(?<planet>.+)\\s*,\\s*(?<m>\\d+)\\s*:\\s*(?<n>\\d+)\\s*\\)", 
					RegexOptions.IgnoreCase);

				/** Defines the parametrization of orbit size that is used */
				internal enum SizeType {SemimajorAxis, Periapsis, Apoapsis};

				/** The type of parameter describing the orbit */
				[Persistent] private SizeType type;
			}

			/** Specialization of ValueRange for orbital phase parameter.
			 * 
			 * @todo I don't think that PhaseRange is a subtype of ValueRange in the Liskov sense... check!
			 */
			private class PhaseRange : ValueRange {
				/** Assigns situation-specific default values to the ValueRange
				 * 
				 * @param[in] dist The distribution from which the value will be drawn
				 * @param[in] type The description of orbit position that is used
				 * @param[in] epoch The time at which the orbit position should be measured
				 * @param[in] min,max The minimum and maximum values allowed for distributions. May be unused.
				 * @param[in] avg The mean value returned. May be unused.
				 * @param[in] stddev The standard deviation of values returned. May be unused.
				 * 
				 * @post The given values will be used by draw() unless they are specifically overridden by a ConfigNode.
				 * 
				 * @exceptsafe Does not throw exceptions
				 */
				internal PhaseRange(Distribution dist, 
						PhaseType type = PhaseType.MeanAnomaly, EpochType epoch = EpochType.GameStart, 
						double min = 0.0, double max = 1.0, double avg = 0.0, double stddev = 0.0) 
						: base(dist, min, max, avg, stddev) {
					this.type = type;
					this.epoch = epoch;
				}

				/** Returns the parametrization used by this ValueRange
				 * 
				 * @return The orbit position parameter represented by this object.
				 * 
				 * @exceptsafe Does not throw exceptions.
				 */
				internal PhaseType getParam() {
					return type;
				}

				/** Returns the epoch at which the phase is evaluated
				 * 
				 * @return The epoch at which the orbital position is specified
				 * 
				 * @exceptsafe Does not throw exceptions.
				 */
				internal EpochType getEpoch() {
					return epoch;
				}

				/** Defines the parametrization of orbit size that is used */
				internal enum PhaseType {MeanLongitude, MeanAnomaly};
				/** Defines the time at which the phase is measured */
				internal enum EpochType {GameStart, Now};

				/** The type of parameter describing the orbit */
				[Persistent] private PhaseType type;
				/** The time at which the parameter should be calculated */
				[Persistent] private EpochType epoch;
			}
		}
	}
}
