using System.Collections.Generic;
using KSP.Localization;
using UnityEngine;

namespace Starstrider42.CustomAsteroids
{
    /// <summary>
    /// A ScenarioModule that stores extra information about unloaded asteroids.
    /// </summary>
    /// <remarks>Much of this information is available for loaded asteroids through the
    /// <see cref="CustomAsteroidData" /> module.</remarks>
    ///
    [KSPScenario (
        ScenarioCreationOptions.AddToAllGames,
        GameScenes.SPACECENTER,
        GameScenes.TRACKSTATION,
        GameScenes.FLIGHT)]
    public sealed class CustomAsteroidRegistry : ScenarioModule
    {
        /// <summary>Reference to a unique CustomAsteroidRegistry.</summary>
        /// <value>The CustomAsteroidRegistry instance used by the current game scene.</value>
        /// <remarks>The reference is not guaranteed to remain valid across scene changes.</remarks>
        public static CustomAsteroidRegistry Instance {
            get;
            private set;
        }

        /// <summary>Constructor called by KSP environment</summary>
        /// <remarks>This constructor enforces CustomAsteroidRegistry's pseudo-singleton
        /// relationship with <see cref="Instance"/>. KSP somehow clears
        /// <see cref="Instance"/> before creating a new scneario object on scene changes,
        /// but it's not clear how.</remarks>
        public CustomAsteroidRegistry ()
        {
            if (Instance != null && Instance != this) {
                Debug.LogError ("[CustomAsteroidRegistry] Duplicate registry found.");
                Destroy (Instance);
            }
            Instance = this;
        }

        /// <summary>
        /// Summarize key information for an asteroid.
        /// </summary>
        /// <param name="vessel">The asteroid whose information needs to be stored.</param>
        /// <param name="info">The information to store.</param>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="vessel"/>
        /// is already registered.</exception>
        internal void RegisterAsteroid (ProtoVessel vessel, AsteroidInfo info)
        {
            asteroids.Add (vessel.persistentId, info);
        }

        /// <summary>
        /// Summarize key information for an asteroid.
        /// </summary>
        /// <param name="vessel">The asteroid whose information needs to be stored.</param>
        /// <param name="info">The information to store.</param>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="vessel"/>
        /// is already registered.</exception>
        internal void RegisterAsteroid (Vessel vessel, AsteroidInfo info)
        {
            asteroids.Add (vessel.persistentId, info);
        }

        /// <summary>
        /// Remove all information for a recently deleted asteroid.
        /// </summary>
        /// <param name="vessel">The asteroid to remove.</param>
        /// <remarks>Does not throw exceptions.</remarks>
        internal void UnregisterAsteroid (ConfigNode vessel)
        {
            uint id = uint.Parse (vessel.GetValue ("persistentId"));
            asteroids.Remove (id);
        }

        /// <summary>
        /// Remove all information for a recently deleted asteroid.
        /// </summary>
        /// <param name="vessel">The asteroid to remove.</param>
        /// <remarks>Does not throw exceptions.</remarks>
        internal void UnregisterAsteroid (Vessel vessel)
        {
            asteroids.Remove (vessel.persistentId);
        }

        /// <summary>
        /// Remove all outdated information from the registry.
        /// </summary>
        /// <remarks>This call removes any registry entries whose asteroids no longer exist.</remarks>
        internal void CleanRegistry ()
        {
            var extantVessels = new HashSet<uint> ();
            foreach (Vessel v in FlightGlobals.Vessels) {
                extantVessels.Add (v.persistentId);
            }

            var toDelete = new HashSet<uint> ();
            foreach (uint key in asteroids.Keys) {
                if (!extantVessels.Contains (key)) {
                    toDelete.Add (key);
                }
            }
            foreach (uint missingId in toDelete) {
                asteroids.Remove (missingId);
            }
        }

        /// <summary>
        /// Return all information on a specific asteroid.
        /// </summary>
        /// <returns>The registered info, or <c>null</c> if the asteroid is not registered.</returns>
        /// <param name="vessel">The asteroid to search for.</param>
        internal AsteroidInfo LookupAsteroid (Vessel vessel)
        {
            AsteroidInfo result = null;
            asteroids.TryGetValue (vessel.persistentId, out result);
            return result;
        }

        /// <summary>
        /// Called when the save game including the module is saved. <c>node</c> is initialized
        /// with the persistent contents of this object.
        /// </summary>
        /// <param name="node">The ConfigNode representing this ScenarioModule.</param>
        public override void OnSave (ConfigNode node)
        {
            base.OnSave (node);

            foreach (AsteroidInfo info in asteroids.Values) {
                node.AddNode (info.ToConfigNode ());
            }
        }

        /// <summary>
        /// Called when the module is either constructed or loaded as part of a save game. After
        /// this method returns, the module will be initialized with any settings in <c>node</c>.
        /// </summary>
        /// <param name="node">The ConfigNode representing this ScenarioModule.</param>
        public override void OnLoad (ConfigNode node)
        {
            base.OnLoad (node);

            var buffer = new Dictionary<uint, AsteroidInfo> ();
            ConfigNode [] entries = node.GetNodes ("asteroid");
            foreach (ConfigNode asteroidNode in entries) {
                string encodedId = asteroidNode.GetValue ("id");
                uint id;
                if (uint.TryParse (encodedId, out id)) {
                    buffer.Add (id, AsteroidInfo.FromConfigNode (asteroidNode));
                } else {
                    Debug.LogError ($"[CustomAsteroidRegistry]: invalid asteroid ID {encodedId}, not loading");
                }
            }
            asteroids = buffer;
        }

        /// <summary>
        /// A mapping of CA asteroids to the asteroid set they came from.
        /// </summary>
        /// <value>The key is the unique ID of the asteroid vessel.</value>
        /// <remarks>May contain asteroids that no longer exist.</remarks>
        Dictionary<uint, AsteroidInfo> asteroids = new Dictionary<uint, AsteroidInfo> ();
    }

    /// <summary>
    /// An entry in the asteroid registry.
    /// </summary>
    internal sealed class AsteroidInfo
    {
        /// <summary>
        /// Persistent ID for the asteroid vessel.
        /// </summary>
        [Persistent]
        // Note: [Persistent] does not work with properties
        public uint id;

        /// <summary>
        /// The asteroid set from which this asteroid came.
        /// </summary>
        /// <remarks>May be null if the set is unknown or inapplicable (e.g., stock asteroids).</remarks>
        [Persistent]
        // Note: [Persistent] does not work with properties
        public string parentSet;

        /// <summary>
        /// Default constructor, to be used with <see cref="FromConfigNode"/>.
        /// </summary>
        AsteroidInfo ()
        {
            id = default (uint);
            parentSet = null;
        }

        /// <summary>
        /// Summarize key information for a newly created asteroid.
        /// </summary>
        /// <param name="vessel">The asteroid whose information needs to be stored.</param>
        /// <param name="parentSet">The asteroid set from which the asteroid was created.</param>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="vessel"/>
        /// is missing its unique identifier.</exception>
        public AsteroidInfo (ConfigNode vessel, string parentSet)
        {
            try {
                this.id = uint.Parse (vessel.GetValue ("persistentId"));
            } catch (System.FormatException e) {
                throw new System.ArgumentNullException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorVesselNoId"), e);
            } catch (System.NullReferenceException e) {
                throw new System.ArgumentNullException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorVesselNull"), e);
            }
            this.parentSet = parentSet;
        }

        /// <summary>
        /// Summarize key information for a newly created asteroid.
        /// </summary>
        /// <param name="vessel">The asteroid whose information needs to be stored.</param>
        /// <param name="parentSet">The asteroid set from which the asteroid was created.</param>
        public AsteroidInfo (ProtoVessel vessel, string parentSet)
        {
            try {
                this.id = vessel.persistentId;
            } catch (System.NullReferenceException e) {
                throw new System.ArgumentNullException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorVesselNull"), e);
            }
            this.parentSet = parentSet;
        }

        /// <summary>
        /// Summarize key information for a newly created asteroid.
        /// </summary>
        /// <param name="vessel">The asteroid whose information needs to be stored.</param>
        /// <param name="parentSet">The asteroid set from which the asteroid was created.</param>
        public AsteroidInfo (Vessel vessel, string parentSet)
        {
            try {
                this.id = vessel.persistentId;

            } catch (System.NullReferenceException e) {
                throw new System.ArgumentNullException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorVesselNull"), e);
            }
            this.parentSet = parentSet;
        }

        /// <summary>
        /// Restore an AsteroidInfo from its persisted form.
        /// </summary>
        /// <param name="node">The ConfigNode representing this object.</param>
        public static AsteroidInfo FromConfigNode (ConfigNode node)
        {
            var info = new AsteroidInfo ();
            ConfigNode.LoadObjectFromConfig (info, node);
            return info;
        }

        /// <summary>
        /// Create a ConfigNode representing this object.
        /// </summary>
        public ConfigNode ToConfigNode ()
        {
            var node = new ConfigNode ("asteroid");
            ConfigNode.CreateConfigFromObject (this, node);
            return node;
        }
    }
}
