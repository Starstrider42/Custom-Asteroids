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

				this.orbitSize    = new ValueRange(ValueRange.Distribution.LogUniform);
				this.eccentricity = new ValueRange(ValueRange.Distribution.Rayleigh, min: 0.0, max: 1.0);
				this.inclination  = new ValueRange(ValueRange.Distribution.Rayleigh);
				this.periapsis    = new ValueRange(ValueRange.Distribution.Uniform, min: 0.0, max: 360.0);
				this.ascNode      = new ValueRange(ValueRange.Distribution.Uniform, min: 0.0, max: 360.0);
				this.orbitPhase   = new ValueRange(ValueRange.Distribution.Uniform, min: 0.0, max: 360.0);
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
					// Unambiguous properties
					double e = eccentricity.draw();
					// Sign of inclination is redundant with 180-degree shift in longitude of ascending node
					// So it's ok to just have positive inclinations
					double i = inclination.draw();

					double aPe = periapsis.draw();		// argument of periapsis
					double lAn = ascNode.draw();		// longitude of ascending node

					// Properties with multiple parametrizations
					double a   = orbitSize.draw();
					double mEp = orbitPhase.draw();		// mean anomaly at epoch?

					// Fix accidentally hyperbolic orbits
					if (a * (1.0-e) < 0.0) {
						a = -a;
					}

					Debug.Log("CustomAsteroids: new orbit at " + a + " m, e = " + e + ", i = " + i 
						+ ", aPe = " + aPe + ", lAn = " + lAn + ", mEp = " + mEp);

					// Does Orbit(...) throw exceptions?
					Orbit newOrbit = new Orbit(i, e, a, lAn, aPe, mEp, Planetarium.GetUniversalTime(), orbitee);
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

				internal enum Distribution {Uniform, LogUniform, Rayleigh};

				[Persistent] private Distribution dist;
				[Persistent] private double min;
				[Persistent] private double max;
				[Persistent] private double avg;
				[Persistent] private double stddev;
			}

			[Persistent] private string name;
			[Persistent] private string centralBody;
			[Persistent] private double spawnRate;
			[Persistent] private ValueRange orbitSize;
			[Persistent] private ValueRange eccentricity;
			[Persistent] private ValueRange inclination;
			[Persistent] private ValueRange periapsis;
			[Persistent] private ValueRange ascNode;
			[Persistent] private ValueRange orbitPhase;
		}
	}
}
