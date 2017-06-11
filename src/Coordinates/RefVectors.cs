using System;
using KSP.Localization;
using UnityEngine;

namespace Starstrider42.CustomAsteroids
{
    internal class RefVectors : ReferencePlane, IPersistenceLoad
    {
        /// <summary>A unique name for the reference plane.</summary>
        [Persistent (name = "name")]
        readonly string id;
        /// <summary>A vector perpendicular to the reference plane.</summary>
        [Persistent]
        readonly Vector3d normVector;
        /// <summary>A vector pointing towards the reference direction.</summary>
        [Persistent]
        readonly Vector3d refVector;

        /// <summary>Workhorse object for handling the geometry.</summary>
        SimplePlane impl;

        internal RefVectors ()
        {
            id = "invalid";
            normVector = Vector3d.zero;
            refVector = Vector3d.zero;
            impl = null;
        }

        public Vector3d toDefaultFrame (Vector3d inFrame)
        {
            if (impl != null) {
                return impl.toDefaultFrame (inFrame);
            } else {
                throw new InvalidOperationException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorRefVectorCorrupt", name));
            }
        }

        public string name {
            get { return id; }
        }

        public override string ToString ()
        {
            return name;
        }

        /// <summary>
        /// Callback used by ConfigNode.LoadObjectFromConfig(). Ensures vectors in config file are
        /// properly interpreted. Warning: Class invariants should not be assumed to hold true prior
        /// to calling this method.
        /// </summary>
        ///
        /// <remarks><c>this.normVector</c> and <c>this.refVector</c> must be initialized (non-zero)
        /// prior to method invocation.</remarks>
        ///
        /// <exception cref="TypeInitializationException">Thrown if one of the vectors has zero
        /// length, or if the two vectors are aligned.</exception>
        public void PersistenceLoad ()
        {
            try {
                if (normVector.magnitude < 1e-8) {
                    throw new InvalidOperationException (
                        Localizer.Format ("#autoLOC_CustomAsteroids_ErrorRefVectorNoZero", name));
                }

                // Only the component in the plane is useful
                Vector3d trueReference = refVector - Vector3d.Project (refVector, normVector);
                if (trueReference.magnitude < 1e-8) {
                    throw new InvalidOperationException (
                        Localizer.Format ("#autoLOC_CustomAsteroids_ErrorRefVectorDegenerate", name));
                }

                Quaternion q = Quaternion.FromToRotation (Planetarium.up.xzy, normVector);
                // FromToRotation can give unintuitive rotations, so check reference direction carefully
                float theta = (float)Vector3d.Angle (q * Planetarium.right.xzy, trueReference);
                // Unity documentation gives wrong order of operations; u*v means
                // "rotate by v, then rotate by u".
                impl = new SimplePlane (id, Quaternion.AngleAxis (theta, normVector) * q);
            } catch (ArgumentException e) {
                throw new TypeInitializationException ("Starstrider42.CustomAsteroids.RefVectors", e);
            }
        }
    }
}
