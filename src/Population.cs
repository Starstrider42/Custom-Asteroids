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
		 */
		internal class Population
		{
			/** Default constructor.
			 * 
			 * Without later modification, the object is not suitable for use
			 * 
			 * @exceptsafe Does not throw exceptions.
			 * 
			 * @note Required by interface of CustomNode.LoadObjectFromConfig()
			 */
			internal Population() {
				this.name        = "";
				this.centralBody = "Kerbin";
				// Make sure that we don't get any asteroids until the values are set sensibly
				this.spawnRate = 0.0;
				this.smaMin    = 0.0;
				this.smaMax    = 0.0;
				this.eccAvg    = 0.0;
				this.incAvg    = 0.0;
			}

			/** Creates a population with specific properties
			 * 
			 * @param[in] name The name of the population. Currently unused.
			 * @param[in] central The name of the body the asteroids will orbit. Must EXACTLY match the name of 
			 * 		a celestial object in KSP.
			 * @param[in] rate The desired rate at which asteroids appear in the population. Currently relative to 
			 * 		the rates of all other populations.
			 * @param[in] aMin,aMax The minimum and maximum semimajor axes allowed in the population.
			 * @param[in] eAvg, iAvg The average eccentricity and (absolute value of) inclination of the population.
			 * 
			 * @todo Find a way to let constructor throw exceptions without breaking PopulationLoader
			 */
			internal Population(string name, string central, 
					double rate, double aMin, double aMax, double eAvg, double iAvg) {
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
			 * @exception System.ArgumentOutOfRangeException Thrown if population has invalid parameters.
			 * 
			 * @todo Use of reflection means there is no way to guarantee validity of Population object 
			 * 		at initialization time... can I avoid throwing exceptions from here?
			 * 
			 * @exceptsafe The program is in a consistent state in the event of an exception
			 */
			internal Orbit drawOrbit() {
				// Would like to only calculate this once, but I don't know for sure that this object will 
				//		be initialized after FlightGlobals
				CelestialBody orbitee  = FlightGlobals.Bodies.Find(body => body.name == this.centralBody);
				// Body 0 should be a sun in most installs, regardless of mod
				if (orbitee == null) orbitee = FlightGlobals.Bodies[0];

				Debug.Log("CustomAsteroids: drawing orbit from " + name);

				double a = RandomDist.drawLogUniform(smaMin, smaMax);
				double e = RandomDist.drawRayleigh(eccAvg);
				// Explicit sign is redundant with 180-degree shift in longitude of ascending node
				double i = /*RandomDist.drawSign() * */ RandomDist.drawRayleigh(incAvg);
				double aPe = RandomDist.drawAngle();		// argument of periapsis
				double lAn = RandomDist.drawAngle();		// longitude of ascending node
				double mEp = RandomDist.drawAngle();		// mean anomaly at epoch?

				Debug.Log("CustomAsteroids: new orbit at " + a + " m, e = " + e + ", i = " + i);

				Orbit newOrbit = new Orbit(i, e, a, lAn, aPe, mEp, Planetarium.GetUniversalTime(), orbitee);
				newOrbit.UpdateFromUT(Planetarium.GetUniversalTime());

				return newOrbit;
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
