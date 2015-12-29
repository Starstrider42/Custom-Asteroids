using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// Singleton class storing reference data.
	/// </summary>
	internal class ReferenceLoader {
		/// <summary>The set of loaded reference planes.</summary>
		private readonly List<ReferencePlane> refs;

		/// <summary>The default reference. Must match the name of an element of refs.</summary>
		private string defaultRef;

		/// <summary>
		/// Creates an empty solar system. Does not throw exceptions.
		/// </summary>
		private ReferenceLoader() {
			this.refs = new List<ReferencePlane>();
			this.defaultRef = null;
		}

		/// <summary>
		/// Factory method obtaining Custom Asteroids settings from KSP config state.
		/// </summary>
		/// 
		/// <returns>A newly constructed ReferenceLoader object containing a full list
		/// 	of all valid reference planes in asteroid config files.</returns>
		/// 
		/// <exception cref="System.TypeInitializationException">Thrown if the ReferenceLoader object 
		/// 	could not be constructed. The program is in a consistent state in the event of an 
		/// 	exception.</exception> 
		internal static ReferenceLoader load() {
			try {
				// Start with an empty population list
				ReferenceLoader allRefs = new ReferenceLoader();

				// Search for reference planes in all config files
				UrlDir.UrlConfig[] configList = GameDatabase.Instance.GetConfigs("CustomAsteroidPlanes");
				foreach (UrlDir.UrlConfig curSet in configList) {
					allRefs.defaultRef = curSet.config.GetValue("defaultRef");
					foreach (ConfigNode curNode in curSet.config.nodes) {
						#if DEBUG
						Debug.Log("[CustomAsteroids]: ConfigNode '" + curNode + "' loaded");
						#endif
						try {
							ReferencePlane plane = null;
							switch (curNode.name) {
							case "REFPLANE":
								plane = new RefAsOrbit();
								break;
							case "REFVECTORS":
								plane = new RefVectors();
								break;
							// silently ignore any other nodes present
							}
							if (plane != null) {
								ConfigNode.LoadObjectFromConfig(plane, curNode);
								allRefs.refs.Add(plane);
							}
						} catch (Exception e) {
							var nodeName = curNode.GetValue("name");
							Debug.LogError("[CustomAsteroids]: failed to load reference plane '"
								+ nodeName + "'");
							Debug.LogException(e);
							if (e.InnerException != null) {
								Util.errorToPlayer("Could not load asteroid group \"{0}\". Cause: \"{1}\"\nRoot Cause: \"{2}\".", 
									nodeName, e.Message, e.GetBaseException().Message);
							} else {
								Util.errorToPlayer("Could not load asteroid group \"{0}\". Cause: \"{1}\".", 
									nodeName, e.Message);
							}
						}	// Attempt to parse remaining populations
					}
				}

				#if DEBUG
				foreach (ReferencePlane x in allRefs.refs) {
					Debug.Log("[CustomAsteroids]: reference plane '" + x + "' loaded");
				}
				#endif

				if (allRefs.defaultRef != null && allRefs.getReferenceSet() == null) {
					Debug.LogError("[CustomAsteroids]: no such reference plane '" + allRefs.defaultRef + "'");
					Util.errorToPlayer("Could not find reference plane \"{0}\".", allRefs.defaultRef);
					allRefs.defaultRef = null;
				}

				return allRefs;
			} catch (Exception e) {
				throw new TypeInitializationException("Starstrider42.CustomAsteroids.ReferenceLoader", e);
			}
		}

		/// <summary>
		/// Returns the default reference set. Does not throw exceptions.
		/// </summary>
		/// 
		/// <returns>The default reference set, or null if no such set exists.</returns>
		internal ReferencePlane getReferenceSet() {
			return getReferenceSet(defaultRef);
		}

		/// <summary>
		/// Returns a specific reference set by its name. Does not throw exceptions.
		/// </summary>
		/// 
		/// <param name="refName">The name attribute of the desired set.</param>
		/// <returns>The indicated reference set, or null if no such set exists.</returns>
		internal ReferencePlane getReferenceSet(string refName) {
			return refName != null ? refs.Find(refPlane => refPlane.name.Equals(refName)) : null;
		}
	}
}
