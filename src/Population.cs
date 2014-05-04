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
				CelestialBody orbitee  = FlightGlobals.Bodies.Find(body => body.name == this.centralBody);
				if (orbitee == null) {
					throw new InvalidOperationException("CustomAsteroids: could not find celestial body named " 
						+ this.centralBody);
				}

				Debug.Log("CustomAsteroids: drawing orbit from " + name);

				try {
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
				} catch (ArgumentOutOfRangeException e) {
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
			private class ValueRange {
				/** Assigns situation-specific default values to the ValueRange
				 * 
				 * @param[in] dist The distribution from which the value will be drawn
				 * @param[in] min,max The minimum and maximum values allowed for distributions. May be unused.
				 * @param[in] avg The mean value returned. May be unused.
				 * @param[in] stddev The standard deviation of values returned. May be unused.
				 *
				 * @post The given values will be used by draw() unless they are specifically overridden by a ConfigNode.
				 * 
				 * @exceptsafe Does not throw exceptions
				 */
				internal ValueRange(Distribution dist, double min = 0.0, double max = 1.0, 
					double avg = 0.0, double stddev = 0.0) {
					this.dist   = dist;
					this.min    = min;
					this.max    = max;
					this.avg    = avg;
					this.stddev = stddev;
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
						throw new System.InvalidOperationException("Invalid distribution specified, code " + dist);
					}
				}

				/** Defines the type of probability distribution from which the value is drawn
				 */
				internal enum Distribution {Uniform, LogUniform, Rayleigh};

				// For some reason, ConfigNode can't load a SizeRange unless SizeRange has access to these members -- even though ConfigNodes seem to completely ignore permissions in all other cases
				/** The probability distribution from which the value is drawn */
				[Persistent] protected Distribution dist;
				/** The minimum allowed value (not always used) */
				[Persistent] protected double min;
				/** The maximum allowed value (not always used) */
				[Persistent] protected double max;
				/** The average value (not always used) */
				[Persistent] protected double avg;
				/** The standard deviation of the values (not always used) */
				[Persistent] protected double stddev;
			}

			/** Specialization of ValueRange for orbital size parameter.
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

				/** Defines the parametrization of orbit size that is used */
				internal enum SizeType {SemimajorAxis, Periapsis, Apoapsis};

				/** The type of parameter describing the orbit */
				[Persistent] private SizeType type;
			}

			/** Specialization of ValueRange for orbital phase parameter.
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
