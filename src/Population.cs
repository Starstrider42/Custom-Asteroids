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
		 */
		internal class Population
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
				this.name        = "INVALID";
				this.centralBody = "Sun";
				this.spawnRate = 0.0;			// Safeguard: don't make asteroids until the values are set sensibly
				this.smaMin    = 1.0;
				this.smaMax    = 1.0;
				this.eccAvg    = 0.0;
				this.incAvg    = 0.0;
			}

			/** Creates a population with specific properties
			 * 
			 * @param[in] name The name of the population. Currently unused.
			 * @param[in] central The name of the body the asteroids will orbit.
			 * @param[in] rate The desired rate at which asteroids appear in the population. Currently relative to 
			 * 		the rates of all other populations.
			 * @param[in] aMin,aMax The minimum and maximum semimajor axes allowed in the population.
			 * @param[in] eAvg, iAvg The average eccentricity and (absolute value of) inclination of the population.
			 * 
			 * @pre @p centralBody is the exact name of a celestial object in KSP.
			 * @pre @p rate &ge; 0
			 * @pre 0 < @p aMin &le; @p aMax
			 * @pre @p eAvg &ge; 0;
			 * @pre @p iAvg &ge; 0;
			 * 
			 * @exceptsafe Object construction is atomic.
			 * 
			 * @note The current implementation does not throw exceptions, but this may change in future versions.
			 */
			internal Population(string name, string central, 
					double rate, double aMin, double aMax, double eAvg, double iAvg) {
				// Don't bother testing preconditions, since I have to check again in DrawOrbit()
				this.name        = name;
				this.centralBody = central;
				this.spawnRate   = rate;
				this.smaMin      = aMin;
				this.smaMax      = aMax;
				this.eccAvg      = eAvg;
				this.incAvg      = iAvg;
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
					double a = RandomDist.drawLogUniform(smaMin, smaMax);
					double e = RandomDist.drawRayleigh(eccAvg);
					// Explicit sign is redundant with 180-degree shift in longitude of ascending node
					double i = /*RandomDist.drawSign() * */ RandomDist.drawRayleigh(incAvg);
					double aPe = RandomDist.drawAngle();		// argument of periapsis
					double lAn = RandomDist.drawAngle();		// longitude of ascending node
					double mEp = RandomDist.drawAngle();		// mean anomaly at epoch?

					Debug.Log("CustomAsteroids: new orbit at " + a + " m, e = " + e + ", i = " + i);

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

			[Persistent] private string name;
			[Persistent] private string centralBody;
			[Persistent] private double spawnRate;
			[Persistent] private double smaMin;
			[Persistent] private double smaMax;
			[Persistent] private double eccAvg;
			[Persistent] private double incAvg;
		}
	}
}
