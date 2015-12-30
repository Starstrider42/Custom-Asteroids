using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// Singleton class storing asteroid types.
	/// </summary>
	internal class TypeLoader {
		/// <summary>The set of loaded asteroid types.</summary>
		private readonly List<AsteroidType> asteroidTypes;

		/// <summary>
		/// Creates an empty solar system. Does not throw exceptions.
		/// </summary>
		private TypeLoader() {
			asteroidTypes = new List<AsteroidType>();
		}

		/// <summary>
		/// Factory method obtaining Custom Asteroids settings from KSP config state.
		/// </summary>
		/// 
		/// <returns>A newly constructed TypeLoader object containing a full list
		/// 	of all valid asteroid types in asteroid config files.</returns>
		/// 
		/// <exception cref="System.TypeInitializationException">Thrown if the TypeLoader object 
		/// 	could not be constructed. The program is in a consistent state in the event of an 
		/// 	exception.</exception>
		internal static TypeLoader load() {
			try {
				// Start with an empty population list
				TypeLoader allTypes = new TypeLoader();

				// Search for populations in all config files
				UrlDir.UrlConfig[] configList = GameDatabase.Instance.GetConfigs("CustomAsteroidTypes");
				foreach (UrlDir.UrlConfig curSet in configList) {
					foreach (ConfigNode curNode in curSet.config.nodes) {
						#if DEBUG
						Debug.Log("[CustomAsteroids]: ConfigNode '" + curNode + "' loaded");
						#endif
						try {
							AsteroidType type = null;
							switch (curNode.name) {
							case "ASTEROID_CLASS":
								type = new FixedClass();
								break;
							// silently ignore any other nodes present
							}
							if (type != null) {
								ConfigNode.LoadObjectFromConfig(type, curNode);
								allTypes.asteroidTypes.Add(type);
							}
						} catch (Exception e) {
							var nodeName = curNode.GetValue("name") ?? "";
							Debug.LogError("[CustomAsteroids]: failed to load type '"
								+ nodeName + "'");
							Debug.LogException(e);
							if (e.InnerException != null) {
								Util.errorToPlayer("Could not load asteroid class \"{0}\". Cause: \"{1}\"\nRoot Cause: \"{2}\".", 
									nodeName, e.Message, e.GetBaseException().Message);
							} else {
								Util.errorToPlayer("Could not load asteroid class \"{0}\". Cause: \"{1}\".", 
									nodeName, e.Message);
							}
						}	// Attempt to parse remaining types
					}
				}

				#if DEBUG
				foreach (AsteroidType x in allTypes.asteroidTypes) {
					Debug.Log("[CustomAsteroids]: Asteroid type '" + x + "' loaded");
				}
				#endif

				if (allTypes.asteroidTypes.Count == 0) {
					Debug.LogWarning("[CustomAsteroids]: Custom Asteroids could not find any types in GameData!");
					ScreenMessages.PostScreenMessage(
						"Custom Asteroids could not find any asteroid types in GameData.\nNew asteroids will have " +
						"default compositions.", 
						5.0f, ScreenMessageStyle.UPPER_CENTER);
				}

				return allTypes;
			} catch (Exception e) {
				throw new TypeInitializationException("Starstrider42.CustomAsteroids.TypeLoader", e);
			}
		}

		/// <summary>
		/// Randomly selects an asteroid class. The selection is weighted by the proportions passed 
		/// to the method.
		/// </summary>
		/// 
		/// <param name="typeRatios">The proportions in which to select the types.</param>
		/// <returns>A reference to the selected asteroid class. Shall not be null.</returns>
		/// 
		/// <exception cref="System.InvalidOperationException">Thrown if there are no classes from 
		/// which to choose, or if all proportions are zero, or if any proportion is negative</exception> 
		internal AsteroidType drawAsteroidType<Dummy>(Proportions<Dummy> typeRatios) {
			try {
				string classId = RandomDist.weightedSample(typeRatios.asPairList());
				AsteroidType asteroidClass = asteroidTypes.Find(type => type.getName().Equals(classId));
				if (asteroidClass == null) {
					throw new InvalidOperationException("No such asteroid class '" + classId + "'");
				}

				return asteroidClass;
			} catch (ArgumentException e) {
				throw new InvalidOperationException("Could not draw asteroid type", e);
			}
		}
	}
}
