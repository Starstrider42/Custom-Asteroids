using System;
using UnityEngine;

namespace Starstrider42.CustomAsteroids {
	internal class RefAsOrbit : ReferencePlane, IPersistenceLoad {
		/// <summary>A unique name for the reference plane.</summary>
		[Persistent(name = "name")] private readonly string id;
		/// <summary>The longitude of ascending node of the reference plane relative to KSP's default plane.</summary>
		[Persistent(name = "longAscNode")] private readonly string rawLongAscNode;
		/// <summary>The inclination of the reference plane relative to KSP's default plane.</summaname
		[Persistent(name = "inclination")] private readonly string rawInclination;
		/// <summary>The angle between the plane's ascending node and the reference direction.</summary>
		[Persistent(name = "argReference")] private readonly string rawArgReference;

		/// <summary>Workhorse object for handling the geometry.</summary>
		private SimplePlane impl;

		internal RefAsOrbit() {
			this.id = "invalid";
			this.rawLongAscNode = "0";
			this.rawInclination = "0";
			this.rawArgReference = "0";
			this.impl = null;
		}

		public Vector3d toDefaultFrame(Vector3d inFrame) {
			if (impl != null) {
				return impl.toDefaultFrame(inFrame);
			} else {
				throw new InvalidOperationException("RefAsOrbit has not been initialized properly.");
			}
		}

		public string name {
			get { return id; }
		}

		public override string ToString() {
			return name;
		}

		/// <summary>
		/// Callback used by ConfigNode.LoadObjectFromConfig(). Ensures that any abstract entries in the config file 
		/// are properly interpreted. Warning: Class invariants should not be assumed to hold true prior to 
		/// calling this method.
		/// </summary>
		/// 
		/// <remarks><c>this.rawLongAscNode</c>, <c>this.rawInclination</c>, and <c>this.rawArgReference</c> must 
		/// contain a representation of the desired object value prior to method invocation.</remarks>
		/// 
		/// <exception cref="TypeInitializationException">Thrown if the ConfigNode could not be interpreted 
		/// 	as a set of floating-point values. The program will be in a consistent state in the event of 
		/// 	an exception.</exception> 
		public void PersistenceLoad() {
			try {
				float longAscNode = (float) ValueRange.parseOrbitalElement(rawLongAscNode);
				float inclination = (float) ValueRange.parseOrbitalElement(rawInclination);
				float argReference = (float) ValueRange.parseOrbitalElement(rawArgReference);

				// Rotations are always around planetarium axes, so treat as extrinsic
				// Rotate first by argReference, then by inclination, then by longAscNode
				// Unity documentation gives the wrong order of operations
				Quaternion q = Quaternion.Euler(0, 0, longAscNode) * Quaternion.Euler(inclination, 0, argReference);
				impl = new SimplePlane(id, q);
			} catch (ArgumentException e) {
				throw new TypeInitializationException("Starstrider42.CustomAsteroids.RefAsOrbit", e);
			}
		}
	}

}
