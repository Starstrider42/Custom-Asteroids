using System;
using KSP.Localization;
using UnityEngine;

namespace Starstrider42.CustomAsteroids
{
    /// <summary>
    /// Represents a set of asteroids aimed at a celestial body's sphere of influence.
    /// </summary>
    ///
    /// <remarks>To avoid breaking the persistence code, Flyby may not have subclasses.</remarks>
    internal sealed class Flyby : AbstractAsteroidSet
    {
        /// <summary>The name of the celestial object the asteroids will approach.</summary>
        [Persistent]
        readonly string targetBody;
        /// <summary>
        /// The distance by which the asteroid would miss <c>targetBody</c> without gravitational focusing.
        /// </summary>
        [Persistent]
        readonly ApproachRange approach;
        /// <summary>The time to closest approach (again, ignoring <c>targetBody</c>'s gravity).</summary>
        [Persistent]
        readonly ValueRange warnTime;
        /// <summary>The speed relative to <c>targetBody</c>, ignoring its gravity.</summary>
        [Persistent]
        readonly ValueRange vSoi;

        /// <summary>
        /// Creates a dummy flyby group. The object is initialized to a state in which it will not be expected to
        /// generate orbits. Any orbits that <em>are</em> generated will be located inside Kerbin, causing the game
        /// to immediately delete the object with the orbit. Does not throw exceptions.
        /// </summary>
        internal Flyby ()
        {
            targetBody = "Kerbin";
            approach = new ApproachRange (ValueRange.Distribution.Uniform,
                ApproachRange.Type.Periapsis, min: 0);
            warnTime = new ValueRange (ValueRange.Distribution.Uniform);
            vSoi = new ValueRange (ValueRange.Distribution.LogNormal, avg: 300, stdDev: 100);
        }

        public override Orbit drawOrbit ()
        {
            CelestialBody body = AsteroidManager.getPlanetByName (targetBody);

            Debug.Log ("[CustomAsteroids]: " + Localizer.Format ("#autoLOC_CustomAsteroids_LogOrbitDraw", getName ()));

            double deltaV = wrappedDraw (vSoi, getName (), "vSoi");
            double deltaT = wrappedDraw (warnTime, getName (), "warnTime");

            if (deltaV <= 0.0) {
                throw new InvalidOperationException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorFlybyBadVInf", getName (), deltaV));
            }
            // Negative deltaT is allowed

            double peri;
            switch (approach.getParam ()) {
            case ApproachRange.Type.ImpactParameter:
                double b = wrappedDraw (approach, getName (), "approach");
                if (b < 0.0) {
                    throw new InvalidOperationException (
                        Localizer.Format ("#autoLOC_CustomAsteroids_ErrorFlybyBadB", getName (), b));
                }
                double a = -body.gravParameter / (deltaV * deltaV);
                double x = b / a;
                peri = a * (1.0 - Math.Sqrt (x * x + 1.0));
                break;
            case ApproachRange.Type.Periapsis:
                peri = wrappedDraw (approach, getName (), "approach");
                if (peri < 0.0) {
                    throw new InvalidOperationException (
                        Localizer.Format ("#autoLOC_CustomAsteroids_ErrorFlybyBadPeri", getName (), peri));
                }
                break;
            default:
                throw new InvalidOperationException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorFlybyBadApproach", getName (), approach.getParam ()));
            }
#if DEBUG
            Debug.Log ($"[CustomAsteroids]: "
                       + Localizer.Format ("#autoLOC_CustomAsteroids_LogFlyby",
                                          peri, targetBody, deltaT / (6.0 * 3600.0), deltaV)
                      );
#endif

            Orbit newOrbit = createHyperbolicOrbit (body, peri, deltaV, Planetarium.GetUniversalTime () + deltaT);
            // Sun has sphereOfInfluence = +Infinity, so condition will always fail for Sun-centric orbit
            // No special treatment needed for the case where the orbit lies entirely outside the SoI
            while (needsSoITransition (newOrbit)) {
                newOrbit = patchToParent (newOrbit);
            }
            newOrbit.UpdateFromUT (Planetarium.GetUniversalTime ());
            return newOrbit;
        }

        /// <summary>
        /// Creates a hyperbolic orbit around the given celestial body.
        /// </summary>
        /// <returns>The newly created orbit, with state vectors anchored to its periapsis.</returns>
        /// <param name="body">The celestial body at the focus of the orbit.</param>
        /// <param name="periapsis">The periapsis (from the body center), in meters. Must not be negative.</param>
        /// <param name="vInf">The excess speed associated with the orbit, in meters per second. Must be positive.</param>
        /// <param name="utPeri">The absolute time of periapsis passage.</param>
        ///
        /// <exception cref="ArgumentException">Thrown if either <c>periapsis</c> or <c>vInf</c> are out
        /// of bounds.</exception>
        static Orbit createHyperbolicOrbit (CelestialBody body, double periapsis, double vInf, double utPeri)
        {
            if (vInf <= 0.0) {
                throw new ArgumentException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorHyperBadVInf", vInf), nameof (vInf));
            }
            if (periapsis < 0.0) {
                throw new ArgumentException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorHyperBadPeri", periapsis), nameof (periapsis));
            }

            double a = -body.gravParameter / (vInf * vInf);
            double e = 1.0 - periapsis / a;

            // Random orientation, to be consistent with CreateRandomOrbitFlyBy
            double i = RandomDist.drawIsotropic ();
            double lAn = RandomDist.drawAngle ();
            double aPe = RandomDist.drawAngle ();

            Debug.Log ($"[CustomAsteroids]: "
                       + Localizer.Format ("#autoLOC_CustomAsteroids_LogHyperOrbit", body.bodyName, a, e, i, aPe, lAn));

            return new Orbit (i, e, a, lAn, aPe, 0.0, utPeri, body);
        }

        /// <summary>
        /// Returns the orbit adjacent to the given orbit. <c>oldOrbit</c> must leave its reference body' SoI. If the
        /// orbit's epoch of periapsis is in the future, the returned orbit will precede <c>oldOrbit</c>; if it is in
        /// the past, the returned orbit will follow <c>oldOrbit</c>.
        /// </summary>
        /// <returns>An orbit around the parent body of <c>oldOrbit</c>'s body, that patches seamlessly with
        /// <c>oldOrbit</c> at its body' SoI boundary. See main description for whether it's an incoming or outgoing
        /// patch.</returns>
        /// <param name="oldOrbit">The orbit to use as a reference for patching. The time interval in which the orbit
        /// is inside the indicated SoI must not include the current time.</param>
        ///
        /// <exception cref="ArgumentException">Thrown if <c>oldOrbit</c> does not intersect a sphere
        /// of influence.</exception>
        /// <exception cref="InvalidOperationException">Thrown if an object following <c>oldOrbit</c> would be
        /// inside its parent body's sphere of influence at the current time.</exception>
        static Orbit patchToParent (Orbit oldOrbit)
        {
            if (!needsSoITransition (oldOrbit)) {
                throw new InvalidOperationException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorSoiInvalid", oldOrbit.referenceBody));
            }
            if (oldOrbit.referenceBody.GetOrbitDriver () == null) {
                throw new ArgumentException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorSoiNoParent", oldOrbit.referenceBody));
            }
            CelestialBody oldParent = oldOrbit.referenceBody;
            CelestialBody newParent = oldParent.orbit.referenceBody;

            double utSoi = getSoiCrossing (oldOrbit);
#if DEBUG
            Debug.Log (Localizer.Format ("#autoLOC_CustomAsteroids_LogSoi", oldParent, newParent, utSoi));
#endif
            // Need position/velocity relative to newParent, not oldParent or absolute
            Vector3d xNewParent = oldOrbit.getRelativePositionAtUT (utSoi)
                                  + oldParent.orbit.getRelativePositionAtUT (utSoi);
            Vector3d vNewParent = oldOrbit.getOrbitalVelocityAtUT (utSoi)
                                  + oldParent.orbit.getOrbitalVelocityAtUT (utSoi);

            Orbit newOrbit = new Orbit ();
            newOrbit.UpdateFromStateVectors (xNewParent, vNewParent, newParent, utSoi);
            return newOrbit;
        }

        /// <summary>
        /// Returns whether the given orbit undergoes any SoI transitions between its characteristic epoch and the
        /// current time. Does not throw exceptions.
        /// </summary>
        /// <returns><c>true</c>, if the orbit needs to be corrected for an SoI transition, <c>false</c> otherwise.</returns>
        /// <param name="orbit">The orbit to test</param>
        static bool needsSoITransition (Orbit orbit)
        {
            double now = Planetarium.GetUniversalTime ();
            double soi = orbit.referenceBody.sphereOfInfluence;

            if (orbit.eccentricity >= 1.0) {
                return orbit.getRelativePositionAtUT (now).magnitude > soi;
            } else {
                // If an elliptical orbit leaves the SoI, then getRelativePositionAtUT gives misleading results
                // after the SoI transition.
                return (orbit.ApR > soi)
                    && ((Math.Abs (now - orbit.epoch) > 0.5 * orbit.period)
                        || (orbit.getRelativePositionAtUT (now).magnitude > soi));
            }
        }

        /// <summary>
        /// Returns the time at which the given orbit enters its parent body's sphere of influence (if in the future)
        /// or exits it (if in the past). If the orbit is always outside the sphere of influence, returns the nominal
        /// time of periapsis.
        /// </summary>
        /// <returns>The UT of SoI entry/exit, to within 1 second.</returns>
        /// <param name="hyperbolic">An unbound orbit. MUST have a parent body with a sphere of influence.</param>
        ///
        /// <exception cref="ArgumentException">Thrown if orbit does not intersect a sphere of influence.</exception>
        static double getSoiCrossing (Orbit hyperbolic)
        {
            // Desired accuracy of result, in seconds
            const double PRECISION = 1.0;

            double soi = hyperbolic.referenceBody.sphereOfInfluence;
            if (double.IsInfinity (soi) || double.IsNaN (soi)) {
                throw new ArgumentException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorSoiNoSoi", hyperbolic.referenceBody),
                    nameof (hyperbolic));
            }
            if (hyperbolic.eccentricity < 1.0 && hyperbolic.ApR < soi) {
                throw new ArgumentException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorSoiNoExit", hyperbolic), nameof (hyperbolic));
            }

            double innerUT = hyperbolic.epoch - hyperbolic.ObTAtEpoch;
            double outerUT = innerUT;
            double pseudoPeriod = Math.Abs (2.0 * Math.PI / hyperbolic.GetMeanMotion (hyperbolic.semiMajorAxis));

            if (innerUT > Planetarium.GetUniversalTime ()) {
                // Search for SoI entry
                while (hyperbolic.getRelativePositionAtUT (outerUT).magnitude < soi) {
                    outerUT -= 0.5 * pseudoPeriod;
                }
            } else {
                // Search for SoI exit
                while (hyperbolic.getRelativePositionAtUT (outerUT).magnitude < soi) {
                    outerUT += 0.5 * pseudoPeriod;
                }
            }

            // Pinpoint SoI entry/exit
            while (Math.Abs (outerUT - innerUT) > PRECISION) {
                double ut = 0.5 * (innerUT + outerUT);
                if (hyperbolic.getRelativePositionAtUT (ut).magnitude < soi) {
                    // Too close; look higher
                    innerUT = ut;
                } else {
                    // Too far; look lower
                    outerUT = ut;
                }
            }

            return 0.5 * (innerUT + outerUT);
        }
    }
}
