/** Code for generating asteroid orbits
 * @file Population.cs
 * @author Starstrider42
 * @date Created April 9, 2014
 */

using System;
using UnityEngine;

namespace Starstrider42 {

	namespace CustomAsteroids
	{
		/** Represents a set of asteroids with similar orbits
		 */
		public class Population
		{
			/** Generates a random orbit consistent with the population properties
			 * 
			 * @return The orbit of a randomly selected member of the population
			 * 
			 * @note Current implementation has a hardcoded asteroid belt. I intend to make it more general later
			 */
			public Orbit drawOrbit() {
				// Some mods might rename it to "Kerbol" or something else
				CelestialBody sun  = FlightGlobals.Bodies.Find(body => body.name == "Sun");
				if (sun == null) sun = FlightGlobals.Bodies[0];

				double aJool = FlightGlobals.Bodies.Find(body => body.name == "Jool").GetOrbit().semiMajorAxis;

				double a = RandomDist.drawLogUniform(aJool*Math.Pow(0.25,2.0/3.0), aJool*Math.Pow(0.5,2.0/3.0));
				double e = RandomDist.drawRayleigh(0.18);
				double i = RandomDist.drawSign() * RandomDist.drawRayleigh(7.5);		// I *think* angles are in degrees...

				Debug.Log("CustomAsteroids: new orbit at " + a + " m, e = " + e + ", i = " + i);

				Orbit newOrbit = new Orbit(i, e, a, RandomDist.drawAngle(), RandomDist.drawAngle(), RandomDist.drawAngle(), 
					Planetarium.GetUniversalTime(),sun);
				//Orbit newOrbit = new Orbit(RandomDist.drawAngle(), 0f, 1060053.49854083, 217.714701468054, 126.848000556171, 0.52911447506945, Planetarium.GetUniversalTime(), FlightGlobals.Bodies.Find(body => body.name == "Eve"));
				newOrbit.UpdateFromUT(Planetarium.GetUniversalTime());

				return newOrbit;
			}
		}
	}
}
