using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using KSP.Localization;
using UnityEngine;

namespace Starstrider42.CustomAsteroids
{
    /// <summary>
    /// A partial implementation of spawning behaviour that defines everything except the spawn
    /// criteria.
    /// </summary>
    internal abstract class AbstractSpawner
    {
        /// <summary>The length of an Earth day, in seconds.</summary>
        protected const double SECONDS_PER_EARTH_DAY = 24.0 * 3600.0;

        private static readonly Regex astName = new Regex (
            Localizer.GetStringByTag ("#autoLOC_6001923").Replace ("<<1>>", "(?<id>[\\w-]+)"),
            RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        /// <summary>
        /// Initializes internal state common to all spawners.
        /// </summary>
        protected AbstractSpawner ()
        {
        }

        /// <summary>
        /// Called periodically by the asteroid driver. The current implementation calls
        /// <see cref="checkDespawn()"/>  and <see cref="checkSpawn()"/>, but carries out no other
        /// actions.
        /// </summary>
        ///
        /// <returns>The number of seconds after which this method should be called again,
        /// nominally the value of <see cref="checkInterval()"/>. Regardless of time warp or the
        /// value of <c>checkInterval()</c>, asteroid checks will never be scheduled less than 0.1
        /// seconds apart.</returns>
        internal float asteroidCheck ()
        {
#if DEBUG
            Debug.Log ("[CustomAsteroids]: asteroidCheck()");
#endif
            checkDespawn ();
            checkSpawn ();

            return Mathf.Max (checkInterval () / TimeWarp.CurrentRate, 0.1f);
        }

        /// <summary>Change seed of Unity's random number generator to a new value.</summary>
        /// <remarks>For reasons unknown, the RNG must be frequently re-seeded to
        /// prevent cycles.</remarks>
        private void resetRng ()
        {
            int newSeed = UnityEngine.Random.Range (0, int.MaxValue);
            resetRng (newSeed);
        }

        /// <summary>Change seed of Unity's random number generator to a specific value.</summary>
        /// <remarks>For reasons unknown, the RNG must be frequently re-seeded to
        /// prevent cycles.</remarks>
        /// <param name="seed">The new seed.</param>
        private void resetRng (int seed)
        {
#if DEBUG
            Debug.Log ($"[CustomAsteroids]: resetting seed to {seed}");
#endif
            UnityEngine.Random.InitState (seed);
        }

        /// <summary>
        /// <para>The interval, in in-game seconds, at which the spawner checks for asteroid
        /// creation or deletion. Depending on the implementation, this may be far more frequent
        /// than the actual spawn rate. The interval must not be corrected for time warp.</para>
        ///
        /// <para>This method must not throw exceptions.</para>
        /// </summary>
        /// <returns>The number of KSP seconds between consecutive spawn/despawn checks.</returns>
        protected virtual float checkInterval ()
        {
            return 15.0f;
        }

        /// <summary>
        /// <para>Determines whether it is time to spawn a new asteroid, and calls
        /// <see cref="spawnAsteroid()"/> as appropriate. This method is called automatically by
        /// <see cref="AbstractSpawner"/> and should not be called explicitly from its
        /// subclasses.</para>
        ///
        /// <para>This method must not throw exceptions.</para>
        /// </summary>
        protected abstract void checkSpawn ();

        /// <summary>
        /// <para>Determines whether any asteroids need to be removed. This method is called
        /// automatically by <see cref="AbstractSpawner"/> and should not be called explicitly from
        /// its subclasses.</para>
        ///
        /// <para>The default implementation searches the current game for untracked asteroids
        /// whose signal strength has reached zero. This is the same approach used by the stock
        /// spawner and should be adequate for most spawner implementations.</para>
        ///
        /// <para>This method must not throw exceptions.</para>
        /// </summary>
        protected virtual void checkDespawn ()
        {
            if (FlightGlobals.Vessels != null) {
                // C# iterators don't support concurrent modification
                List<Vessel> toDelete = new List<Vessel> ();

                foreach (Vessel v in FlightGlobals.Vessels) {
                    DiscoveryInfo trackState = v.DiscoveryInfo;
                    // This test will fail if and only if v is an unvisited, untracked asteroid
                    // It does not matter whether or not it was tracked in the past
                    if (trackState != null
                            && !trackState.HaveKnowledgeAbout (DiscoveryLevels.StateVectors)) {
                        // Untracked asteroid; how old is it?
                        if (v.DiscoveryInfo.GetSignalLife (Planetarium.GetUniversalTime ()) <= 0) {
                            toDelete.Add (v);
                        }
                    }
                }

                foreach (Vessel oldAsteroid in toDelete) {
                    Debug.Log ("[CustomAsteroids]: "
                              + Localizer.Format ("#autoLOC_CustomAsteroids_LogUnspawn",
                                                  oldAsteroid.GetName ()));
                    oldAsteroid.Die ();
                    CustomAsteroidRegistry.Instance.UnregisterAsteroid (oldAsteroid);
                }
            }
        }

        /// <summary>
        /// Returns true if the player's space center can detect asteroids. This method is not used
        /// by <see cref="AbstractSpawner"/>; in particular, implementations of
        /// <see cref="checkSpawn()"/> are responsible for calling this method if they want to be
        /// sensitive to the tracking station state.
        /// </summary>
        /// <returns><c>true</c> if the player has a fully upgraded tracking station, or is playing
        /// in a game mode where upgrades are not necessary; <c>false</c> otherwise.</returns>
        protected static bool areAsteroidsTrackable ()
        {
            if ((object)GameVariables.Instance == null) {
                // It probably means the game isn't properly loaded?
                return false;
            }

            return GameVariables.Instance.UnlockedSpaceObjectDiscovery (
                ScenarioUpgradeableFacilities.GetFacilityLevel (
                    SpaceCenterFacility.TrackingStation));
        }

        /// <summary>
        /// <para>Creates a new asteroid from a randomly chosen asteroid set. The asteroid will be
        /// given the properties specified by that set, and added to the game as an untracked
        /// object.</para>
        /// <para>If asteroid creation failed for any reason, this method will log the error rather
        /// than propagating the exception into client code.</para>
        /// </summary>
        /// <returns>the newly created asteroid, or null if no asteroid was created. May be used as
        /// a hook by spawners that need more control over asteroid properties. Clients should
        /// assume the returned vessel is already registered in the game.</returns>
        /// <remarks>At least one population must have a positive spawn rate.</remarks>
        protected ProtoVessel spawnAsteroid ()
        {
            resetRng ();
            try {
                AsteroidSet group = AsteroidManager.drawAsteroidSet ();
                ProtoVessel asteroid = spawnAsteroid (group);
                try {
                    registerAsteroid (asteroid, group);
                } catch (ArgumentException e) {
                    Debug.LogWarning ("[CustomAsteroids]: Duplicate entry in CustomAsteroidRegistry.");
                    Debug.LogException (e);
                }
                return asteroid;
            } catch (Exception e) {
                Util.errorToPlayer (e, Localizer.Format ("#autoLOC_CustomAsteroids_ErrorSpawnFail"));
                Debug.LogException (e);
                return null;
            }
        }

        /// <summary>
        /// Registers basic information on an asteroid in <see cref="CustomAsteroidRegistry"/>.
        /// </summary>
        /// <param name="asteroid">The asteroid to register.</param>
        /// <param name="group">The AsteroidSet from which the asteroid was drawn.</param>
        /// <remarks>This method must only be called by <see cref="spawnAsteroid()"/>, and is
        /// provided as part of the API for reference. It stores the following information:
        /// <list type="bullet">
        /// <item>
        ///     <term>parentSet</term>
        ///     <description>The unique ID of <paramref name="group"/>.</description>
        /// </item>
        /// </list>
        /// </remarks>
        static void registerAsteroid (ProtoVessel asteroid, AsteroidSet group)
        {
            CustomAsteroidRegistry.Instance.RegisterAsteroid (
                asteroid, new AsteroidInfo (asteroid, group.getName ()));
        }

        /// <summary>
        /// Creates a new asteroid from the specific asteroid set. The asteroid will be given
        /// appropriate properties as specified in the set's config node, and added to the game as
        /// an untracked object.
        /// </summary>
        /// <remarks>Based heavily on Kopernicus's <c>DiscoverableObjects.SpawnAsteroid</c> by
        /// ThomasKerman. Thanks for reverse-engineering everything!</remarks>
        ///
        /// <param name="group">The set to which the asteroid belongs.</param>
        /// <returns>The asteroid that was added.</returns>
        ///
        /// <exception cref="System.InvalidOperationException">Thrown if <c>group</c> cannot
        /// generate valid data. The program state will be unchanged in the event of an
        /// exception.</exception>
        /// <exception cref="System.NullReferenceException">Thrown if <c>group</c> is
        /// null.</exception>
        private ProtoVessel spawnAsteroid (AsteroidSet group)
        {
            Orbit orbit = makeOrbit (group);
            string name = makeName (group);
            UntrackedObjectClass size = group.drawAsteroidSize ();
            ConfigNode [] partList = makeAsteroidParts (group);
            ConfigNode [] extraNodes = new ConfigNode []
            {
                new ConfigNode ("ACTIONGROUPS"),
                makeDiscoveryInfo(group, size),
                makeVesselModules(group, partList, size),
            };

            // Stock spawner reports its module name, so do the same for custom spawns
            Debug.Log ($"[{GetType ().Name}]: "
                       + Localizer.Format ("#autoLOC_CustomAsteroids_LogSpawn", name, group));

            ConfigNode vessel = ProtoVessel.CreateVesselNode (name, VesselType.SpaceObject, orbit,
                                    0, partList, extraNodes);

            return HighLogic.CurrentGame.AddVessel (vessel);
        }

        /// <summary>
        ///Generates a new asteroid orbit appropriate for the chosen set.
        /// </summary>
        ///
        /// <param name="group">The set to which the asteroid belongs.</param>
        /// <returns>A randomly generated orbit.</returns>
        ///
        /// <exception cref="System.InvalidOperationException">Thrown if <c>group</c> cannot
        /// generate valid orbits. The program state will be unchanged in the event of an
        /// exception.</exception>
        /// <exception cref="System.NullReferenceException">Thrown if <c>group</c> is null.</exception>
        private static Orbit makeOrbit (AsteroidSet group)
        {
            return group.drawOrbit ();
        }

        /// <summary>
        /// Generates a new asteroid name appropriate for the chosen set.
        /// </summary>
        ///
        /// <param name="group">The set to which the asteroid belongs.</param>
        /// <returns>A randomly generated name. The name will be prefixed by a population-specific
        /// name if custom names are enabled, or by "Ast." if they are disabled.</returns>
        ///
        /// <exception cref="System.InvalidOperationException">Thrown if <c>group</c> cannot
        /// generate valid names. The program state will be unchanged in the event of an
        /// exception.</exception>
        /// <exception cref="System.NullReferenceException">Thrown if <c>group</c> is
        /// null.</exception>
        private static string makeName (AsteroidSet group)
        {
            string name = DiscoverableObjectsUtil.GenerateAsteroidName ();
            if (AsteroidManager.getOptions ().getRenameOption ()) {
                GroupCollection parsed = astName.Match (name).Groups;
                if (parsed [0].Success) {
                    string newBase = group.getAsteroidName ();
                    if (!newBase.Contains ("<<1>>")) {
                        newBase += " <<1>>";
                    }
                    string id = parsed ["id"].ToString ();
                    name = Localizer.Format (newBase, id);
                }
                // if asteroid name doesn't match expected format, leave it as-is
#if DEBUG
                Debug.Log ("[CustomAsteroids]: "
                           + Localizer.Format ("#autoLOC_CustomAsteroids_LogRename", name));
#endif
            }

            return name;
        }

        /// <summary>
        /// Generates tracking station info appropriate for the chosen set.
        /// </summary>
        ///
        /// <param name="group">The set to which the asteroid belongs.</param>
        /// <param name="size">The asteroid's size class.</param>
        /// <returns>A ConfigNode storing the asteroid's DiscoveryInfo object.</returns>
        ///
        /// <exception cref="System.InvalidOperationException">Thrown if <c>group</c> cannot
        /// generate valid data. The program state will be unchanged in the event of an
        /// exception.</exception>
        /// <exception cref="System.NullReferenceException">Thrown if <c>group</c> is
        /// null.</exception>
        private static ConfigNode makeDiscoveryInfo (AsteroidSet group, UntrackedObjectClass size)
        {
            Pair<double, double> lifetimes = group.drawTrackingTime ();
            ConfigNode trackingInfo = ProtoVessel.CreateDiscoveryNode (
                                          DiscoveryLevels.Presence, size, lifetimes.first, lifetimes.second);
            return trackingInfo;
        }

        /// <summary>
        /// Generates any vessel modules needed by a particular asteroid.
        /// </summary>
        /// <param name="group">The asteroid group to which the asteroid belongs.</param>
        /// <param name="partList">The part(s) from which the asteroid is made.</param>
        /// <param name="size">The asteroid's size class.</param>
        /// <returns>A <c>VesselModules</c> node containing all vessel modules besides
        /// the defaults (currently <c>FlightIntegrator</c> and <c>AxisGroupModule</c>).</returns>
        /// <exception cref="System.NullReferenceException">Thrown if <c>group</c> or <c>partList</c> is
        /// null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if <c>partList</c> does not
        /// contain exactly one part, or contains an unsupported part. The program state will be
        /// unchanged in the event of an exception.</exception>
        private ConfigNode makeVesselModules(AsteroidSet group, ConfigNode[] partList, UntrackedObjectClass size)
        {
            if (partList.Length != 1)
            {
                throw new InvalidOperationException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorMultiParts", partList));
            }
            AvailablePart part = PartLoader.getPartInfoByName(partList[0].GetValue("name"));

            ConfigNode wrapper = new ConfigNode("VESSELMODULES");
            // CometVessel seems to be removed if there's no ModuleComet, but filter just in case
            if (part.partPrefab.HasModuleImplementing<ModuleComet>())
            {
                string cometType = "intermediate";
                bool cometName = true;
                CometOrbitType stockClass = CometManager.GetCometOrbitType(cometType);
                // As far as I can tell the object class is used to scale the activity level
                ConfigNode moduleComet = CometManager.GenerateDefinition(stockClass, size, new System.Random().Next())
                    .CreateVesselNode(false, 0.0f, !cometName);
                wrapper.AddNode(moduleComet);
            }
            return wrapper;
        }

        /// <summary>
        /// Generates vessel parts and resources appropriate for the chosen set.
        /// </summary>
        ///
        /// <param name="group">The set to which the asteroid belongs.</param>
        /// <returns>An array of ConfigNodes storing the asteroid's parts. The first element MUST
        /// be the root part.</returns>
        ///
        /// <exception cref="System.InvalidOperationException">Thrown if <c>group</c> cannot
        /// generate valid parts. The program state will be unchanged in the event of an
        /// exception.</exception>
        /// <exception cref="System.NullReferenceException">Thrown if <c>group</c> is
        /// null.</exception>
        private static ConfigNode [] makeAsteroidParts (AsteroidSet group)
        {
            // The same "seed" that shows up in ProceduralAsteroid?
            uint seed = (uint)UnityEngine.Random.Range (0, Int32.MaxValue);
            string part = group.drawAsteroidType ();
            try {
                ConfigNode potato = ProtoVessel.CreatePartNode (part, seed);
                return new [] { potato };
            } catch (Exception e) {
                // Really? That's what CreatePartNode throws?
                throw new InvalidOperationException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorTypeBadPart", part), e);
            }
        }
    }
}
