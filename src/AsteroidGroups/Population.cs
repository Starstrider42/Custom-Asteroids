using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// Represents a set of asteroids with similar stable orbits.
	/// </summary>
	/// 
	/// <remarks>To avoid breaking the persistence code, Population may not have subclasses.</remarks>
	internal sealed class Population : AsteroidSet, IPersistenceLoad {
		/// <summary>Defines the syntax for a composition class declaration.</summary>
		private static readonly Regex classOccurrence = new Regex("(?<rate>[-+.e\\d]+)\\s+(?<id>\\w+)", 
			RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

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
		/// <summary>The plane relative to which the orbit is specified. Null means to use the default plane.</summary>
		[Persistent] private readonly string refPlane;
		/// <summary>Relative ocurrence rates of asteroid classes.</summary>
		private List<Pair<string, double>> classRatios;
		/// <summary>Config-friendly copy of classRatios.</summary>
		[Persistent(name="asteroidTypes", collectionIndex="key")]
		private string[] readableRatios;

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

			this.orbitSize = new  SizeRange(ValueRange.Distribution.LogUniform, SizeRange.Type.SemimajorAxis);
			this.eccentricity = new ValueRange(ValueRange.Distribution.Rayleigh, min: 0.0, max: 1.0);
			this.inclination = new ValueRange(ValueRange.Distribution.Rayleigh);
			this.periapsis = new  PeriRange(ValueRange.Distribution.Uniform, PeriRange.Type.Argument, 
				min: 0.0, max: 360.0);
			this.ascNode = new ValueRange(ValueRange.Distribution.Uniform, min: 0.0, max: 360.0);
			this.orbitPhase = new PhaseRange(ValueRange.Distribution.Uniform, min: 0.0, max: 360.0, 
				type: PhaseRange.PhaseType.MeanAnomaly, epoch: PhaseRange.EpochType.GameStart);

			this.refPlane = null;

			this.classRatios = new List<Pair<string, double>>();
			this.readableRatios = null;
		}

		public Orbit drawOrbit() {
			try {
				// Would like to only calculate this once, but I don't know for sure that this object will 
				//		be initialized after FlightGlobals
				CelestialBody orbitee = AsteroidManager.getPlanetByName(centralBody);

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
				case PeriRange.Type.Argument:
					aPe = peri;
					break;
				case PeriRange.Type.Longitude:
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
				case SizeRange.Type.SemimajorAxis:
					a = size;
					break;
				case SizeRange.Type.Periapsis:
					a = size / (1.0 - e);
					break;
				case SizeRange.Type.Apoapsis:
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
				frameTransform(newOrbit);
				newOrbit.UpdateFromUT(Planetarium.GetUniversalTime());

				return newOrbit;
			} catch (ArgumentException e) {
				throw new InvalidOperationException("[CustomAsteroids]: could not create orbit", e);
			}
		}

		public ConfigNode drawAsteroidData() {
			var data = new CustomAsteroidData();

			try {
			if (classRatios.Count > 0) {
				string classId = RandomDist.weightedSample(classRatios);
				var nodeList = GameDatabase.Instance.GetConfigNodes("ASTEROID_CLASS").Where(node => node.GetValue("name") == classId);
				if (!nodeList.Any()) {
					throw new InvalidOperationException("CustomAsteroids: no such asteroid class '" + classId + "'");
				}

				foreach (ConfigNode asteroidClass in nodeList) {
					data.composition = asteroidClass.GetValue("title");
					data.density = Single.Parse(asteroidClass.GetValue("density"));
					data.sampleExperimentId = asteroidClass.GetValue("sampleExperimentId");
					data.sampleExperimentXmitScalar = Single.Parse(asteroidClass.GetValue("sampleExperimentXmitScalar"));
				}
			}

			var returnNode = new ConfigNode();
			ConfigNode.CreateConfigFromObject(data, returnNode);
			return returnNode;
			} catch (ArgumentOutOfRangeException e) {
				throw new InvalidOperationException("CustomAsteroids: could not select asteroid class.", e);
			}
		}

		public double getSpawnRate() {
			return spawnRate;
		}

		public string getName() {
			return name;
		}

		public string getAsteroidName() {
			return title;
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
		/// Callback used by ConfigNode.LoadObjectFromConfig().
		/// </summary>
		public void PersistenceLoad() {
			if (readableRatios != null) {
				for (int i = 0; i < readableRatios.Length; i++) {
					if (classOccurrence.Match(readableRatios[i]).Groups[0].Success) {
						GroupCollection parsed = classOccurrence.Match(readableRatios[i]).Groups;
						double rate;
						if (!Double.TryParse(parsed["rate"].ToString(), out rate)) {
							throw new ArgumentException("Cannot parse '" + parsed["rate"] + "' as a floating point number");
						}

						classRatios.Add(new Pair<string, double>(parsed["id"].ToString(), rate));
					} else {
						//throw new ArgumentException("Cannot parse '" + readableRatios[i] + "' as a number and name");
					}
				}
			}
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
		/// Transforms the orbit from the population's reference frame to the KSP frame.
		/// </summary>
		/// 
		/// <param name="orbit">The orbit relative to the population's reference frame.</param>
		/// 
		/// <exception cref="System.InvalidOperationException">Thrown if the desired transform could not be 
		/// carried out.</exception>
		private void frameTransform(Orbit orbit) {
			ReferencePlane plane = refPlane != null 
				? AsteroidManager.getReferencePlane(refPlane) 
				: AsteroidManager.getDefaultPlane();
			if (plane != null) {
				double ut = Planetarium.GetUniversalTime();
				Vector3d x = orbit.getRelativePositionAtUT(ut);
				Vector3d v = orbit.getOrbitalVelocityAtUT(ut);

				#if DEBUG
				Debug.Log("Transforming orbit from frame " + plane);
				#endif

				Vector3d xNew = plane.toDefaultFrame(x);
				Vector3d vNew = plane.toDefaultFrame(v);

				orbit.UpdateFromStateVectors(xNew, vNew, orbit.referenceBody, ut);
			} else if (refPlane != null) {
				throw new InvalidOperationException("No such reference frame: " + refPlane);
			}
		}
	}
}
