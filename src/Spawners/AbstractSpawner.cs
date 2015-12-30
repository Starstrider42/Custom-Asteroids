using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// A partial implementation of spawning behaviour that defines everything except the spawn criteria.
	/// </summary>
	internal abstract class AbstractSpawner {
		/// <summary>The length of an Earth day, in seconds.</summary>
		protected const double SECONDS_PER_EARTH_DAY = 24.0 * 3600.0;

		/// <summary>
		/// Returns the sizeCurve used by the stock spawner as of KSP 1.0.5. This corresponds to the following 
		/// size distribution: 12% class A, 13% class B, 49% class C, 13% class D, and 12% class E.
		/// </summary>
		private static readonly FloatCurve stockSizeCurve = new FloatCurve(new [] {
				new Keyframe(0.0f, 0.0f, 1.5f, 1.5f), 
				new Keyframe(0.3f, 0.45f, 0.875f, 0.875f), 
				new Keyframe(0.7f, 0.55f, 0.875f, 0.875f), 
				new Keyframe(1.0f, 1.0f, 1.5f, 1.5f)
			});

		/// <summary>
		/// Initializes internal state common to all spawners.
		/// </summary>
		protected AbstractSpawner() {
		}

		/// <summary>
		/// Called periodically by the asteroid driver. The current implementation calls <see cref="checkDespawn()"/>  
		/// and <see cref="checkSpawn()"/>, but carries out no other actions.
		/// </summary>
		/// 
		/// <returns>The number of seconds after which this method should be called again, nominally the value of 
		/// <see cref="checkInterval()"/>. Regardless of time warp or the value of <c>checkInterval()</c>, asteroid 
		/// checks will never be scheduled less than 0.1 seconds apart.</returns>
		internal float asteroidCheck() {
			#if DEBUG
			Debug.Log("[CustomAsteroids]: asteroidCheck()  called.");
			#endif
			checkDespawn();
			checkSpawn();

			return Mathf.Max(checkInterval() / TimeWarp.CurrentRate, 0.1f);
		}

		/// <summary>
		/// <para>The interval, in in-game seconds, at which the spawner checks for asteroid creation or deletion. 
		/// Depending on the implementation, this may be far more frequent than the actual spawn rate. The interval 
		/// must not be corrected for time warp.</para>
		/// 
		/// <para>This method must not throw exceptions.</para>
		/// </summary>
		/// <returns>The number of KSP seconds between consecutive spawn/despawn checks.</returns>
		protected virtual float checkInterval() {
			return 15.0f;
		}

		/// <summary>
		/// <para>Determines whether it is time to spawn a new asteroid, and calls <see cref="spawnAsteroid()"/> 
		/// as appropriate. This method is called automatically by <see cref="AbstractSpawner"/> and should not be 
		/// called explicitly from its subclasses.</para>
		/// 
		/// <para>This method must not throw exceptions.</para>
		/// </summary>
		protected abstract void checkSpawn();

		/// <summary>
		/// <para>Determines whether any asteroids need to be removed. This method is called automatically 
		/// by <see cref="AbstractSpawner"/> and should not be called explicitly from its subclasses.</para>
		/// 
		/// <para>The default implementation searches the current game for untracked asteroids whose signal strength 
		/// has reached zero. This is the same approach used by the stock spawner and should be adequate for most 
		/// spawner implementations.</para>
		/// 
		/// <para>This method must not throw exceptions.</para>
		/// </summary>
		protected virtual void checkDespawn() {
			if (FlightGlobals.Vessels != null) {
				// Not sure if C# iterators support concurrent modification; play it safe
				List<Vessel> toDelete = new List<Vessel>();

				foreach (Vessel v in FlightGlobals.Vessels) {
					DiscoveryInfo trackState = v.DiscoveryInfo;
					// This test will fail if and only if v is an unvisited, untracked asteroid
					// It does not matter whether or not it was tracked in the past
					if (trackState != null && !trackState.HaveKnowledgeAbout(DiscoveryLevels.StateVectors)) {
						// Untracked asteroid; how old is it?
						if (v.DiscoveryInfo.GetSignalLife(Planetarium.GetUniversalTime()) <= 0) {
							toDelete.Add(v);
						}
					}
				}

				foreach (Vessel oldAsteroid in toDelete) {
					Debug.Log("[CustomAsteroids]: asteroid " + oldAsteroid.GetName()
						+ " has been untracked for too long and is now lost.");
					oldAsteroid.Die();
				}
			}
		}

		/// <summary>
		/// Returns true if the player's space center can detect asteroids. This method is not used by 
		/// <see cref="AbstractSpawner"/>; in particular, implementations of <see cref="checkSpawn()"/> are 
		/// responsible for calling this method if they want to be sensitive to the tracking station state.
		/// </summary>
		/// <returns><c>true</c> if the player has a fully upgraded tracking station, or is playing in a game mode 
		/// where upgrades are not necessary; <c>false</c> otherwise.</returns>
		protected static bool areAsteroidsTrackable() {
			if (GameVariables.Instance == null) {
				// It probably means the game isn't properly loaded?
				return false;
			}

			return GameVariables.Instance.UnlockedSpaceObjectDiscovery(
				ScenarioUpgradeableFacilities.GetFacilityLevel(
					SpaceCenterFacility.TrackingStation));
		}

		/// <summary>
		/// <para>Creates a new asteroid from a randomly chosen asteroid set. The asteroid will be given the 
		/// properties specified by that set, and added to the game as an untracked object.</para>
		/// <para>If asteroid creation failed for any reason, this method will log the error rather than propagating 
		/// the exception into client code.</para>
		/// </summary>
		/// <returns>the newly created asteroid, or null if no asteroid was created. May be used as a hook by spawners 
		/// that need more control over asteroid properties. Clients should assume the returned vessel is already 
		/// registered in the game.</returns>
		protected ProtoVessel spawnAsteroid() {
			try {
				return spawnAsteroid(AsteroidManager.drawAsteroidSet());
			} catch (Exception e) {
				if (e.InnerException != null) {
					Util.errorToPlayer("Could not create new asteroid. Cause: \"{0}\"\nRoot Cause: \"{1}\".", 
						e.Message, e.GetBaseException().Message);
				} else {
					Util.errorToPlayer("Could not create new asteroid. Cause: \"{0}\".", 
						e.Message);
				}
				Debug.LogException(e);
				return null;
			}
		}

		/// <summary>
		/// Creates a new asteroid from the specific asteroid set. The asteroid will be given appropriate properties 
		/// as specified in the set's config node, and added to the game as an untracked object.
		/// </summary>
		/// <remarks>Based heavily on Kopernicus's <c>DiscoverableObjects.SpawnAsteroid</c> by ThomasKerman. Thanks 
		/// for reverse-engineering everything!</remarks>
		/// 
		/// <param name="group">The set to which the asteroid belongs.</param>
		/// <returns>The asteroid that was added.</returns>
		/// 
		/// <exception cref="System.InvalidOperationException">Thrown if <c>group</c> cannot generate valid data. The 
		/// program state will be unchanged in the event of an exception.</exception> 
		/// <exception cref="System.NullReferenceException">Thrown if <c>group</c> is null.</exception> 
		private ProtoVessel spawnAsteroid(AsteroidSet group) {
			Orbit orbit = makeOrbit(group);
			string name = makeName(group);
			ConfigNode trackingInfo = makeDiscoveryInfo(group);
			ConfigNode asteroidType = group.drawAsteroidData();
			ConfigNode[] partList = makeAsteroidParts(group);

			// Stock spawner reports its module name, so do the same for custom spawns
			Debug.Log(string.Format("[{0}]: New object found: {1} in asteroid set {2}.", GetType().Name, name, group));

			ConfigNode vessel = ProtoVessel.CreateVesselNode(name, VesselType.SpaceObject, orbit, 
				                    0, partList, new ConfigNode("ACTIONGROUPS"), trackingInfo);

			// IMPORTANT: no exceptions past this point!

			return HighLogic.CurrentGame.AddVessel(vessel);
		}

		/// <summary>
		///Generates a new asteroid orbit appropriate for the chosen set.
		/// </summary>
		/// 
		/// <param name="group">The set to which the asteroid belongs.</param>
		/// <returns>A randomly generated orbit.</returns>
		/// 
		/// <exception cref="System.InvalidOperationException">Thrown if <c>group</c> cannot generate valid orbits. The 
		/// program state will be unchanged in the event of an exception.</exception>
		/// <exception cref="System.NullReferenceException">Thrown if <c>group</c> is null.</exception> 
		private static Orbit makeOrbit(AsteroidSet group) {
			return group.drawOrbit();
		}

		/// <summary>
		/// Generates a new asteroid name appropriate for the chosen set.
		/// </summary>
		/// 
		/// <param name="group">The set to which the asteroid belongs.</param>
		/// <returns>A randomly generated name. The name will be prefixed by a population-specific name if custom 
		/// names are enabled, or by "Ast." if they are disabled.</returns>
		/// 
		/// <exception cref="System.InvalidOperationException">Thrown if <c>group</c> cannot generate valid names. The 
		/// program state will be unchanged in the event of an exception.</exception>
		/// <exception cref="System.NullReferenceException">Thrown if <c>group</c> is null.</exception> 
		private static string makeName(AsteroidSet group) {
			string name = DiscoverableObjectsUtil.GenerateAsteroidName();
			if (AsteroidManager.getOptions().getRenameOption()) {
				string newBase = group.getAsteroidName();
				if (name.IndexOf("Ast. ") >= 0) {
					// Keep only the ID number
					string id = name.Substring(name.IndexOf("Ast. ") + "Ast. ".Length);
					name = newBase + " " + id;
				}
				// if asteroid name doesn't match expected format, leave it as-is
				#if DEBUG
				Debug.Log("[CustomAsteroids]: Asteroid renamed to " + name);
				#endif
			}

			return name;
		}

		/// <summary>
		/// Generates tracking station info appropriate for the chosen set.
		/// </summary>
		/// 
		/// <param name="group">The set to which the asteroid belongs.</param>
		/// <returns>A ConfigNode storing the asteroid's DiscoveryInfo object.</returns>
		/// 
		/// <exception cref="System.InvalidOperationException">Thrown if <c>group</c> cannot generate valid data. The 
		/// program state will be unchanged in the event of an exception.</exception>
		/// <exception cref="System.NullReferenceException">Thrown if <c>group</c> is null.</exception> 
		private static ConfigNode makeDiscoveryInfo(AsteroidSet group) {
			Pair<float, float> trackTimes = AsteroidManager.getOptions().getUntrackedTimes();
			double lifetime = UnityEngine.Random.Range(trackTimes.first, trackTimes.second) * SECONDS_PER_EARTH_DAY;
			double maxLifetime = trackTimes.second * SECONDS_PER_EARTH_DAY;
			UntrackedObjectClass size = (UntrackedObjectClass) (int) 
				(stockSizeCurve.Evaluate(UnityEngine.Random.Range(0.0f, 1.0f))
			                            * Enum.GetNames(typeof(UntrackedObjectClass)).Length);
			ConfigNode trackingInfo = ProtoVessel.CreateDiscoveryNode(
				                          DiscoveryLevels.Presence, size, lifetime, maxLifetime);
			return trackingInfo;
		}

		/// <summary>
		/// Generates vessel parts and resources appropriate for the chosen set.
		/// </summary>
		///
		/// <param name="group">The set to which the asteroid belongs.</param>
		/// <returns>An array of ConfigNodes storing the asteroid's parts. The first element MUST be the root 
		/// part.</returns>
		/// 
		/// <exception cref="System.InvalidOperationException">Thrown if <c>group</c> cannot generate valid parts. The 
		/// program state will be unchanged in the event of an exception.</exception>
		/// <exception cref="System.NullReferenceException">Thrown if <c>group</c> is null.</exception> 
		private static ConfigNode[] makeAsteroidParts(AsteroidSet group) {
			// The same "seed" that shows up in ProceduralAsteroid?
			uint seed = (uint) UnityEngine.Random.Range(0, Int32.MaxValue);
			ConfigNode potato = ProtoVessel.CreatePartNode("PotatoRoid", seed);
			ConfigNode customData = new ProtoPartModuleSnapshot(group.drawAsteroidData()).moduleValues;
			// For some reason the module name isn't added automatically...?
			if (!customData.HasValue("name")) {
				customData.AddValue("name", typeof(CustomAsteroidData).Name);
			}
			#if DEBUG
			Debug.Log("PartModule = " + customData);
			#endif
			potato.AddNode(customData);
			return new[] { potato };
		}
	}
}
