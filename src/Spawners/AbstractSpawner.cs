using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Starstrider42.CustomAsteroids {
	/**
	 * A partial implementation of spawning behaviour that defines everything except the spawn criteria.
	 */
	internal abstract class AbstractSpawner {
		/**
		 * Returns the sizeCurve used by the stock spawner as of KSP 1.0.5.
		 * 
		 * @return the following distribution: 12% class A, 13% class B, 49% class C, 13% class D, and 12% class E
		 */
		private static readonly FloatCurve stockSizeCurve;

		static AbstractSpawner() {
			stockSizeCurve = new FloatCurve(new Keyframe[] {
				new Keyframe(0.0f, 0.0f , 1.5f  , 1.5f), 
				new Keyframe(0.3f, 0.45f, 0.875f, 0.875f), 
				new Keyframe(0.7f, 0.55f, 0.875f, 0.875f), 
				new Keyframe(1.0f, 1.0f , 1.5f  , 1.5f)
			});
		}

		/**
		 * Initialises internal state common to all spawners.
		 */
		protected AbstractSpawner() {
		}

		/**
		 * Called periodically by the asteroid driver. The current implementation calls checkDespawn() 
		 * and checkSpawn(), but carries out no other actions.
		 * 
		 * @return the number of seconds after which this method should be called again, nominally the value of 
		 * checkInterval(). Regardless of time warp or the value of checkInterval(), asteroid checks will 
		 * never be scheduled less than 0.1 seconds apart.
		 */
		internal float asteroidCheck() {
			#if DEBUG
			Debug.Log("[CustomAsteroids]: asteroidCheck()  called.");
			#endif
			checkDespawn();
			checkSpawn();

			return Mathf.Max(checkInterval() / TimeWarp.CurrentRate, 0.1f);
		}

		/**
		 * The interval, in in-game seconds, at which the spawner checks for asteroid creation or deletion. 
		 * Depending on the implementation, this may be far more frequent than the actual spawn rate. The interval 
		 * must not be corrected for time warp.
		 * 
		 * @return number of KSP seconds between consecutive spawn/despawn checks
		 */
		protected virtual float checkInterval() {
			return 15.0f;
		}

		/**
		 * Determines whether it is time to spawn a new asteroid, and calls 
		 * spawnAsteroid() as appropriate. This method is called automatically by AbstractSpawner 
		 * and should not be called explicitly from its subclasses.
		 */
		protected abstract void checkSpawn();

		/**
		 * Determines whether any asteroids need to be removed. This method is called automatically 
		 * by AbstractSpawner and should not be called explicitly from its subclasses.
		 * 
		 * The default implementation searches the current game for untracked asteroids whose signal strength has 
		 * reached zero. This is the same approach used by the stock spawner and should be adequate for most 
		 * spawner implementations.
		 */
		protected virtual void checkDespawn() {
			if (FlightGlobals.Vessels != null) {
				// Not sure if C# lists support concurrent modification; play it safe
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

		/**
		 * Returns true if the player's space center can detect asteroids. This method is not used
		 * by AbstractSpawner; in particular, implementations of checkSpawn() are responsible for 
		 * calling this method if they want to be sensitive to the tracking station state.
		 * 
		 * @return true if the player has a fully upgraded tracking station, or is playing in a game mode 
		 * where upgrades are not necessary; false otherwise
		 */
		protected bool areAsteroidsTrackable() {
			if (GameVariables.Instance == null) {
				// It probably means the game isn't properly loaded?
				return false;
			} else {
				return GameVariables.Instance.UnlockedSpaceObjectDiscovery(
					ScenarioUpgradeableFacilities.GetFacilityLevel(
						SpaceCenterFacility.TrackingStation));
			}
		}

		/**
		 * Creates a new asteroid from a randomly chosen population. The asteroid will be given the properties 
		 * specified by that population.
		 * 
		 * If asteroid creation failed for any reason, this method will log the error rather than propagating 
		 * the exception into client code.
		 * 
		 * @return the newly created asteroid, or null if no asteroid was created. May be used as a hook by 
		 * 		spawners that need more control over asteroid properties. Clients should assume the returned 
		 * 		vessel is already registered in the game.
		 */
		protected ProtoVessel spawnAsteroid() {
			try {
				return spawnAsteroid(AsteroidManager.drawPopulation());
			} catch (Exception e) {
				if (e.InnerException != null) {
					Util.ErrorToPlayer("Could not create new asteroid. Cause: \"{0}\"\nRoot Cause: \"{1}\".", 
						e.Message, e.GetBaseException().Message);
				} else {
					Util.ErrorToPlayer("Could not create new asteroid. Cause: \"{0}\".", 
						e.Message);
				}
				Debug.LogException(e);
				return null;
			}
		}

		/**
		 * Creates a new asteroid from the specific population. The asteroid will be given appropriate 
		 * properties as specified in the population config file.
		 * 
		 * Based heavily on Kopernicus's DiscoverableObjects.SpawnAsteroid by ThomasKerman.
		 * Thanks for reverse-engineering everything!
		 * 
		 * @param[in] pop the population to which the asteroid belongs. May be null to represent 
		 * 		the default population.
		 * 
		 * @return the asteroid that was added
		 * 
		 * @post a newly created asteroid is added to the game as an untracked object
		 * 
		 * @exception InvalidOperationException Thrown if a 
		 * 		population exists, but cannot generate valid data
		 * 
		 * @exceptsafe the game state is unchanged in the event of an exception
		 */
		private ProtoVessel spawnAsteroid(Population pop) {
			Orbit orbit = makeOrbit(pop);
			string name = makeName(pop);
			ConfigNode trackingInfo = makeDiscoveryInfo(pop);
			ConfigNode[] partList = makeAsteroidParts(pop);

			// Stock spawner reports its module name, so do the same for custom spawns
			Debug.Log("[" + this.GetType().Name + "]: New object found: " + name + " in population " 
				+ (pop != null ? pop.ToString() : AsteroidManager.defaultPopulation().ToString()) + ".");

			ConfigNode vessel = ProtoVessel.CreateVesselNode(
				name,
				VesselType.SpaceObject,
				orbit,
				0,
				partList,
				new ConfigNode("ACTIONGROUPS"),
				trackingInfo
			);

			// IMPORTANT: no exceptions past this point!

			return HighLogic.CurrentGame.AddVessel(vessel);
		}

		/**
		 * Generates a new asteroid orbit appropriate for the chosen population.
		 * 
		 * @param[in] pop the population to which the asteroid belongs. May be null to represent 
		 * 		the default population.
		 * 
		 * @return a randomly generated orbit. If @p pop is not null, the orbit will be generated 
		 * 		from the corresponding config information; otherwise, it will be generated using a 
		 * 		hardcoded approximation to the stock asteroid spawner.
		 */
		private static Orbit makeOrbit(Population pop) {
			return pop != null ? pop.drawOrbit() : AsteroidManager.defaultPopulation().drawOrbit();
		}

		/**
		 * Generates a new asteroid name appropriate for the chosen population.
		 * 
		 * @param[in] pop the population to which the asteroid belongs. May be null to represent 
		 * 		the default population.
		 * 
		 * @return a randomly generated name. The name will be prefixed by a population-specific name 
		 * 		if custom names are enabled, or by "Ast." if they are disabled.
		 */
		private static string makeName(Population pop) {
			string name = DiscoverableObjectsUtil.GenerateAsteroidName();
			#if DEBUG
			Debug.Log("[CustomAsteroids]: Stock name = " + name);
			#endif
			if (AsteroidManager.getOptions().getRenameOption()) {
				string newBase = pop != null 
					? pop.getAsteroidName() : AsteroidManager.defaultPopulation().getAsteroidName();
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

		/**
		 * Generates tracking station info appropriate for the chosen population.
		 * 
		 * @param[in] pop the population to which the asteroid belongs. May be null to represent 
		 * 		the default population.
		 * 
		 * @return a ConfigNode storing the asteroid's DiscoveryInfo object
		 */
		private static ConfigNode makeDiscoveryInfo(Population pop) {
			Pair<float, float> trackTimes = AsteroidManager.getOptions().getUntrackedTimes();
			double lifetime = UnityEngine.Random.Range(trackTimes.First, trackTimes.Second) * SECONDS_PER_EARTH_DAY;
			double maxLifetime = trackTimes.Second * SECONDS_PER_EARTH_DAY;
			UntrackedObjectClass size = (UntrackedObjectClass) ((int) 
				(stockSizeCurve.Evaluate(UnityEngine.Random.Range(0.0f, 1.0f)) * Enum.GetNames(typeof(UntrackedObjectClass)).Length)
			);
			ConfigNode trackingInfo = ProtoVessel.CreateDiscoveryNode(DiscoveryLevels.Presence, size, lifetime, maxLifetime);
			return trackingInfo;
		}

		/**
		 * Generates vessel parts and resources appropriate for the chosen population.
		 * 
		 * @param[in] pop the population to which the asteroid belongs. May be null to represent 
		 * 		the default population.
		 * 
		 * @return an array of ConfigNodes storing the asteroid's parts. 
		 * 		The first element MUST be the root part.
		 */
		private static ConfigNode[] makeAsteroidParts(Population pop) {
			// The same "seed" that shows up in ProceduralAsteroid?
			uint seed = (uint) UnityEngine.Random.Range(0, Int32.MaxValue);
			ConfigNode potato = ProtoVessel.CreatePartNode("PotatoRoid", seed);
			return new[] { potato };
		}

		protected const double SECONDS_PER_EARTH_DAY = 24.0 * 3600.0;
	}
}
