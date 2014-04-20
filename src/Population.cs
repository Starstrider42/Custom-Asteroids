/** Code for generating asteroid orbits
 * @file Population.cs
 * @author Starstrider42
 * @date Created April 9, 2014
 */

using System;
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
						// I'm defining longitude in the reference plane, not in the asteroid's orbital plane
						// Cos[l] == (Cos[Ω] Cos[θ + ω] - Sin[Ω] Sin[θ + ω] Cos[i])/Sqrt[Cos[θ + ω]^2 + Cos[i]^2 Sin[θ + ω]^2];
						// Sin[l] == (Sin[Ω] Cos[θ + ω] + Cos[Ω] Sin[θ + ω] Cos[i])/Sqrt[Cos[θ + ω]^2 + Cos[i]^2 Sin[θ + ω]^2];
						// Let's hope I translated the Mathematica solution correctly
						// Why doesn't KSP.Orbit have a function for this?
						double   iRad =     i * Math.PI/180.0;
						double aPeRad =   aPe * Math.PI/180.0;
						double lAnRad =   lAn * Math.PI/180.0;
						double  phRad = phase * Math.PI/180.0;
						/** @todo Confirm or correct that this condition determines the sign of mEp
						 */
						mEp = (Math.Sin(phRad-lAnRad-aPeRad*Math.Cos(iRad)) >= 0 ? 1 : -1) * 
							Math.Acos(2.0 * (Math.Cos(iRad) * Math.Cos(aPeRad) * Math.Cos(phRad - lAnRad) 
									+ Math.Sin(aPeRad) * Math.Sin(phRad - lAnRad))
								/ Math.Sqrt(3.0 + Math.Cos(2.0*iRad) 
									- 2.0 * Math.Cos(2.0 * (phRad - lAnRad)) * Math.Sin(iRad) * Math.Sin(iRad)) );
						// Inclination is the hard part... what if we assume it's zero?
						/*mEp = (Math.Sin(phRad-lAnRad-aPeRad) >= 0 ? 1 : -1) * 
							Math.Acos((Math.Cos(aPeRad) * Math.Cos(phRad - lAnRad) + Math.Sin(aPeRad) * Math.Sin(phRad - lAnRad)) );*/
						break;
					default:
						throw new InvalidOperationException("CustomAsteroids: cannot describe orbit position using type " 
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
						throw new InvalidOperationException("CustomAsteroids: cannot describe orbit position using type " 
							+ orbitSize.getParam());
					}

					// Fix accidentally hyperbolic orbits
					if (a * (1.0-e) < 0.0) {
						a = -a;
					}

					Debug.Log("CustomAsteroids: new orbit at " + a + " m, e = " + e + ", i = " + i 
						+ ", aPe = " + aPe + ", lAn = " + lAn + ", mEp = " + mEp);

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
			 * @see [Object.ToString()](http://msdn.microsoft.com/en-us/library/system.object.tostring.aspx)
			 */
			public override string ToString() {
				return name;
			}

			////////////////////////////////////////////////////////
			// Population properties

			[Persistent] private string name;
			[Persistent] private string centralBody;
			[Persistent] private double spawnRate;
			[Persistent] private  SizeRange orbitSize;
			[Persistent] private ValueRange eccentricity;
			[Persistent] private ValueRange inclination;
			[Persistent] private ValueRange periapsis;
			[Persistent] private ValueRange ascNode;
			[Persistent] private PhaseRange orbitPhase;


			/** Represents the set of values an orbital element may assume
			 * 
			 * The same consistency caveats as for Population apply here.
			 */
			private class ValueRange {
				/** Allows situation-specific defaults to be assigned before the ConfigNode overwrites them
				 * 
				 * @param[in] dist The distribution from which the value will be drawn
				 * @param[in] min,max The minimum and maximum values allowed for distributions. May be unused.
				 * @param[in] avg The mean value returned. May be unused.
				 * @param[in] stddev The standard deviation of values returned. May be unused.
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
				 * @except System.InvalidOperationException Thrown if the parameters are inappropriate 
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
				[Persistent] protected Distribution dist;
				[Persistent] protected double min;
				[Persistent] protected double max;
				[Persistent] protected double avg;
				[Persistent] protected double stddev;
			}

			private class SizeRange : ValueRange {
				/** Allows situation-specific defaults to be assigned before the ConfigNode overwrites them
				 * 
				 * @param[in] type The description of orbit size that is used
				 * @param[in] dist The distribution from which the value will be drawn
				 * @param[in] min,max The minimum and maximum values allowed for distributions. May be unused.
				 * @param[in] avg The mean value returned. May be unused.
				 * @param[in] stddev The standard deviation of values returned. May be unused.
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

				/** Defines the parametrization of orbit size that is used
				 */
				internal enum SizeType {SemimajorAxis, Periapsis, Apoapsis};

				[Persistent] private SizeType type;
			}

			private class PhaseRange : ValueRange {
				/** Allows situation-specific defaults to be assigned before the ConfigNode overwrites them
				 * 
				 * @param[in] type The description of orbit position that is used
				 * @param[in] dist The distribution from which the value will be drawn
				 * @param[in] min,max The minimum and maximum values allowed for distributions. May be unused.
				 * @param[in] avg The mean value returned. May be unused.
				 * @param[in] stddev The standard deviation of values returned. May be unused.
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

				/** Defines the parametrization of orbit size that is used
				 */
				internal enum PhaseType {MeanLongitude, MeanAnomaly};
				internal enum EpochType {GameStart, Now};

				[Persistent] private PhaseType type;
				[Persistent] private EpochType epoch;
			}
		}
	}
}
