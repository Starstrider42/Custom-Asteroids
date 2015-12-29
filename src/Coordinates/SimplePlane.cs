using UnityEngine;

namespace Starstrider42.CustomAsteroids {
	/// <summary>Standard implementation of <see cref="ReferencePlane"/>.</summary>
	internal class SimplePlane : ReferencePlane {
		public string name { get; private set; }

		/// <summary>Rotation used to implement toDefaultFrame.</summary>
		private readonly Quaternion xform;

		/// <summary>
		/// Creates a new ReferencePlane with the given name and rotation.
		/// </summary>
		/// <param name="id">A unique identifier for this object. Initialises the <see cref="name"/> property.</param>
		/// <param name="thisToDefault">A rotation that transforms vectors from this reference frame 
		/// to the KSP default reference frame.</param>
		protected internal SimplePlane(string id, Quaternion thisToDefault) {
			this.name = id;
			this.xform = thisToDefault;
		}

		public Vector3d toDefaultFrame(Vector3d inFrame) {
			return xform * inFrame;
		}
	}
}
