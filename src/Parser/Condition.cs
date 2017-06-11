using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KSP.Localization;
using UnityEngine;

namespace Starstrider42.CustomAsteroids
{
    /// <summary>
    /// Represents a simple combination of boolean conditions. For persistance purposes, the
    /// collection can be viewed as a collection of strings, with each string representing an
    /// elementary condition, plus additional data on how the conditions are related.
    /// </summary>
    internal class Condition
    {
        /// <summary>Determines whether all conditions or any conditions must be met.</summary>
        [Persistent]
        Operator combine;

        // TODO: find a way to allow collection classes to contain ConfigNode values that don't
        // represent elements
        [Persistent (collectionIndex = "condition")]
        readonly ConditionGroup<string> conditions;

        /// <summary>
        /// Sets the default values of the condition (no clauses, all clauses must be true).
        /// </summary>
        /// <remarks>Must be public so that it is accessible to [default namespace].ConfigNode.</remarks>
        public Condition ()
        {
            combine = Operator.And;
            conditions = new ConditionGroup<string> ();
        }

        /// <summary>
        /// Evaluates this condition.
        /// </summary>
        public bool check ()
        {
            return conditions.check (combine);
        }

        public override string ToString ()
        {
            return string.Join (string.Format (" {0} ", combine), conditions.ToArray ());
        }

        /// <summary>Indicates whether conditions are required (And) or optional (Or).</summary>
        internal enum Operator
        {
            And,
            Or
        }
    }

    /// <summary>
    /// Represents a simple combination of boolean conditions. For persistance purposes, the
    /// collection can be viewed as a collection of strings, with each string representing an
    /// elementary condition.
    /// </summary>
    ///
    /// <remarks>To be recognized as a collection by ConfigNodes, a class must implement
    /// <c>System.Collections.Generic.IEnumerable<T></c>, have a matching type parameter <c>T</c>,
    /// and implement <c>System.Collections.IList</c>. Quite the odd combination...</remarks>
    ///
    /// <typeparam name="Dummy">Required for proper ConfigNode handling. Only
    /// Condition&lt;string&rt; will work correctly.</typeparam>
    internal class ConditionGroup<Dummy> : System.Collections.IList, IEnumerable<string>
    {
        /// <summary>Parsed representation of the conditions to check.</summary>
        readonly IList<GamePredicate> clauses;

        /// <summary>
        /// Sets the default values of the condition (no clauses, all clauses must be true).
        /// </summary>
        /// <remarks>Must be public so that it is accessible to ConfigNode.</remarks>
        public ConditionGroup ()
        {
            clauses = new List<GamePredicate> (2);
        }

        /// <summary>
        /// Evaluates this condition.
        /// </summary>
        internal bool check (Condition.Operator combine)
        {
            switch (combine) {
            case Condition.Operator.And:
                return clauses.All (clause => clause.check ());
            case Condition.Operator.Or:
                return clauses.Any (clause => clause.check ());
            default:
                throw new InvalidOperationException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorConditionStateBadOp", combine));
            }
        }

        /// <summary>
        /// Parses a string as a predicate keyword.
        /// </summary>
        ///
        /// <param name="input">A string, assumed to be in the format "[body].[property]".</param>
        /// <returns>A predicate representing the desired property for the desired body.</returns>
        ///
        /// <exception cref="ArgumentException">Thrown if <c>input</c> does not have the correct
        /// format. The program state shall be unchanged in the event of an exception.</exception>
        static GamePredicate parse (string input)
        {
            Regex inputTemplate = new Regex (
                "(?<planet>.+)\\s*\\.\\s*(?<pred>\\w+?)(?<type>manned|unmanned)?$",
                RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

            if (inputTemplate.Match (input).Groups [0].Success) {
                GroupCollection parsed = inputTemplate.Match (input).Groups;
                string planet = parsed ["planet"].ToString ();
                string pred = parsed ["pred"].ToString ();
                VesselType type = parsed ["type"] == null || parsed ["type"].ToString () == ""
                    ? VesselType.Any
                    : parsed ["type"].ToString ().ToLower () == "manned"
                        ? VesselType.Manned : VesselType.Unmanned;

                switch (pred.ToLower ()) {
                case "reached":
                    return new AchievementPredicate (new []
                        {
                            new [] { planet, "Escape" },
                            new [] { planet, "Flyby" }
                        }, type);
                case "hadorbit":
                    return new AchievementPredicate (new [] { new [] { planet, "Orbit" } }, type);
                case "hadlanded":
                    return new AchievementPredicate (new []
                        {
                            new [] { planet, "Landing" },
                            new [] { planet, "Splashdown" }
                        }, type);
                case "science":
                    return new AchievementPredicate (new [] { new [] { planet, "Science" } }, type);
                case "nowpresent":
                    return new VesselPredicate (planet, VesselPredicate.Test.Present, type);
                case "noworbit":
                    return new VesselPredicate (planet, VesselPredicate.Test.Orbiting, type);
                case "nowlanded":
                    return new VesselPredicate (planet, VesselPredicate.Test.Landed, type);
                default:
                    throw new ArgumentException (
                        Localizer.Format ("#autoLOC_CustomAsteroids_ErrorConditionInputBadType", pred),
                        nameof (input));
                }
            }
            throw new ArgumentException (
                Localizer.Format ("#autoLOC_CustomAsteroids_ErrorConditionInputBad", input),
                nameof (input));
        }

        /// <summary>
        /// Converts a predicate to a string in the format "[body].[property]".
        /// </summary>
        ///
        /// <param name="innerData">The predicate to convert.</param>
        /// <returns>A string <c>x</c> such that <c>parse(x) == innerData</c>.</returns>
        ///
        /// <exception cref="ArgumentException">Thrown if <c>innerData</c> does not correspond to
        /// any string in the desired format.</exception>
        static string unparse (GamePredicate innerData)
        {
            try {
                return innerData.ToString ();
            } catch (InvalidOperationException e) {
                throw new ArgumentException (
                    Localizer.Format ("#autoLOC_CustomAsteroids_ErrorConditionStateBadPred",
                                      innerData),
                    nameof (innerData), e);
            }
        }

        public int Add (object value)
        {
            clauses.Add (parse ((string)value));
            return clauses.Count;
        }

        public bool Contains (object value)
        {
            return clauses.Contains (parse ((string)value));
        }

        public void Clear ()
        {
            clauses.Clear ();
        }

        public int IndexOf (object value)
        {
            return clauses.IndexOf (parse ((string)value));
        }

        public void Insert (int index, object value)
        {
            clauses.Insert (index, parse ((string)value));
        }

        public void Remove (object value)
        {
            clauses.Remove (parse ((string)value));
        }

        public void RemoveAt (int index)
        {
            clauses.RemoveAt (index);
        }

        public object this [int index] {
            get {
                return unparse (clauses [index]);
            }
            set {
                clauses [index] = parse ((string)value);
            }
        }

        public bool IsReadOnly {
            get {
                return false;
            }
        }

        public bool IsFixedSize {
            get {
                return false;
            }
        }

        public void CopyTo (Array array, int index)
        {
            GamePredicate [] buffer = new GamePredicate [array.Length];
            clauses.CopyTo (buffer, index);
            for (int i = 0; i < array.Length; i++) {
                array.SetValue (unparse (buffer [i]), i);
            }
        }

        public int Count {
            get {
                return clauses.Count;
            }
        }

        public object SyncRoot {
            get {
                return clauses;
            }
        }

        public bool IsSynchronized {
            get {
                return false;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        public IEnumerator<string> GetEnumerator ()
        {
            return new StringView (clauses);
        }

        /// <summary>Represents a specific condition related to the game state.</summary>
        interface GamePredicate
        {
            /// <summary>
            /// Evaluates this predicate.
            /// </summary>
            ///
            /// <exception cref="InvalidOperationException">Thrown if the condition is
            /// corrupted or otherwise impossible to evaluate.</exception>
            bool check ();

            /// <summary>
            /// Returns the standard string representation of this predicate.
            /// </summary>
            /// <returns>A string <c>x</c> such that <c>parse(x) == this</c>.</returns>
            ///
            /// <exception cref="InvalidOperationException">Thrown if this predicate cannot be
            /// parsed from any string.</exception>
            string ToString ();
        }

        enum VesselType
        {
            Any,
            Manned,
            Unmanned
        }

        /// <summary>
        /// Represents a test of the game's achievement state.
        /// </summary>
        class AchievementPredicate : GamePredicate
        {
            /// <summary>A set of paths representing the location of the achievement within the
            /// progress tree.</summary>
            readonly IList<string []> lookup;
            /// <summary>Allows testing of specifically manned or unmanned missions.</summary>
            readonly VesselType whoQualifies;

            /// <summary>
            /// Wraps the given achievement as a predicate test.
            /// </summary>
            ///
            /// <param name="paths">A collection of paths. Each path gives, in heirarchical order,
            /// the nested locations of the ProgressNode to test. If more than one path is given,
            /// any of the corresponding achievements may be used to satisfy this predicate.</param>
            /// <param name="qualifies">Whether the achievement should be tested for any flight,
            /// a manned flight, or an unmanned flight.</param>
            internal AchievementPredicate (ICollection<string []> paths, VesselType qualifies = VesselType.Any)
            {
                lookup = new List<string []> (paths);
                whoQualifies = qualifies;
            }

            public bool check ()
            {
                foreach (string [] path in lookup) {
                    // Make sure we have the node for the current scene
                    ProgressNode node = ProgressTracking.Instance.FindNode (path);
                    if (node == null) {
                        throw new InvalidOperationException (
                            Localizer.Format (
                                "#autoLOC_CustomAsteroids_ErrorConditionContext",
                                ToString ())
                            + " "
                            + Localizer.Format (
                                "#autoLOC_CustomAsteroids_ErrorConditionStateBadAchieve",
                                string.Join (".", path)));
                    }

                    bool result;
                    switch (whoQualifies) {
                    case VesselType.Any:
                        result = node.IsComplete;
                        break;
                    case VesselType.Manned:
                        result = node.IsCompleteManned;
                        break;
                    case VesselType.Unmanned:
                        result = node.IsCompleteUnmanned;
                        break;
                    default:
                        throw new InvalidOperationException (
                            Localizer.Format (
                                "#autoLOC_CustomAsteroids_ErrorConditionContext",
                                ToString ())
                            + ""
                            + Localizer.Format (
                                "#autoLOC_CustomAsteroids_ErrorConditionStateBadRestriction",
                                whoQualifies));
                    }

                    if (result) {
                        return true;
                    }
                    // else try the next node
                }

                // No element of lookup is complete
                return false;
            }

            public override string ToString ()
            {
                foreach (string [] achievement in lookup) {
                    if (achievement.Length < 2) {
                        throw new InvalidOperationException (
                            Localizer.Format (
                                "#autoLOC_CustomAsteroids_ErrorConditionStateBadAchieve",
                                achievement));
                    }
                    string achieveType = achievement [0];
                    switch (achievement [1].ToLower ()) {
                    case "escape":
                    case "flyby":
                        achieveType += ".reached";
                        break;
                    case "orbit":
                        achieveType += ".hadOrbit";
                        break;
                    case "landing":
                    case "splashdown":
                        achieveType += ".hadLanded";
                        break;
                    case "science":
                        achieveType += ".science";
                        break;
                    default:
                        continue;           // Pick first achievement that's one of the above
                    }

                    switch (whoQualifies) {
                    case VesselType.Any:
                        return achieveType;
                    case VesselType.Manned:
                    case VesselType.Unmanned:
                        return achieveType + whoQualifies;
                    default:
                        throw new InvalidOperationException (
                            Localizer.Format (
                                "#autoLOC_CustomAsteroids_ErrorConditionStateBadRestriction",
                                whoQualifies));
                    }
                }

                throw new InvalidOperationException (
                    Localizer.Format (
                        "#autoLOC_CustomAsteroids_ErrorConditionStateNoAchieve",
                        lookup));
            }
        }

        /// <summary>
        /// Represents a test of the game's active vessels.
        /// </summary>
        class VesselPredicate : GamePredicate
        {
            /// <summary>
            /// The index associated with the planet of interest. Assumed to remain constant across
            /// scene changes.
            /// </summary>
            readonly int body;

            /// <summary>The state in which a vessel must be present around body.</summary>
            readonly Test state;

            /// <summary>Allows testing of specifically manned or unmanned missions.</summary>
            readonly VesselType whoQualifies;

            /// <summary>
            /// Sets up a search for vessels matching the desired criteria.
            /// </summary>
            ///
            /// <param name="body">The name of the celestial body where vessels are to be
            /// found.</param>
            /// <param name="condition">What vessels must be doing to pass the test.</param>
            /// <param name="type">Whether the predicate can be satisfied by manned vessels,
            /// unmanned vessels, or both.</param>
            internal VesselPredicate (string body, Test condition, VesselType type)
            {
                this.body = FlightGlobals.Bodies.FindIndex (cb => cb.name.Equals (body));
                state = condition;
                whoQualifies = type;
            }

            public bool check ()
            {
                foreach (Vessel v in FlightGlobals.Vessels) {
                    if (v.vesselType == global::VesselType.SpaceObject
                            || v.vesselType == global::VesselType.Unknown) {
                        continue;
                    }

                    bool correctType = whoQualifies == VesselType.Any || (isVesselManned (v)
                            ? whoQualifies == VesselType.Manned
                            : whoQualifies == VesselType.Unmanned);
#if DEBUG
                    Debug.Log (Localizer.Format ("#autoLOC_CustomAsteroids_LogAchievement",
                                                v.GetName (), whoQualifies, correctType));
#endif

                    if (correctType && v.mainBody == FlightGlobals.Bodies [body]) {
                        switch (state) {
                        case Test.Present:
                            return true;
                        case Test.Orbiting:
                            if (!v.LandedOrSplashed
                                && v.orbit.eccentricity < 1.0
                                && v.orbit.PeA > v.mainBody.atmosphereDepth) {
                                return true;
                            }
                            break;
                        case Test.Landed:
                            if (v.LandedOrSplashed) {
                                return true;
                            }
                            break;
                        default:
                            throw new InvalidOperationException (
                                Localizer.Format (
                                    "#autoLOC_CustomAsteroids_ErrorConditionContext",
                                    ToString ())
                                + " "
                                + Localizer.Format (
                                    "#autoLOC_CustomAsteroids_ErrorConditionStateBadVesssel",
                                    state));
                        }
                    }
                }

                return false;
            }

            /// <summary>
            /// Tests whether the vessel is manned or unmanned. This method works whether the vessel
            /// is loaded or not.
            /// </summary>
            ///
            /// <param name="v">The vessel to test.</param>
            /// <returns><c>true</c>, if manned, <c>false</c> if unmanned.</returns>
            static bool isVesselManned (Vessel v)
            {
#if DEBUG
                Debug.Log (Localizer.Format ("#autoLOC_CustomAsteroids_LogCrew",
                                            v.GetName (), v.GetVesselCrew ().Count,
                                             v.GetCrewCapacity ()));
#endif

                // v.GetCrewCapacity() and v.GetCrewCount() appear to only work if vessel is loaded
                // ProtoPartSnapshots don't store crew capacity...
                // TODO: find a solution with the current KSP API
                return v.GetCrewCapacity () > 0 || v.GetVesselCrew ().Count > 0;
            }

            /// <summary>
            /// Returns the standard string representation of this predicate.
            /// </summary>
            /// <returns>A string <c>x</c> such that <c>parse(x) == this</c>.</returns>
            ///
            /// <exception cref="InvalidOperationException">Thrown if this predicate cannot be
            /// parsed from any string.</exception>
            public override string ToString ()
            {
                string desc = FlightGlobals.Bodies [body].name + ".";
                switch (state) {
                case Test.Present:
                    desc += "nowpresent";
                    break;
                case Test.Orbiting:
                    desc += "noworbit";
                    break;
                case Test.Landed:
                    desc += "nowlanded";
                    break;
                default:
                    throw new InvalidOperationException (
                        Localizer.Format (
                            "#autoLOC_CustomAsteroids_ErrorConditionStateBadVesssel",
                            state));
                }

                switch (whoQualifies) {
                case VesselType.Any:
                    return desc;
                case VesselType.Manned:
                case VesselType.Unmanned:
                    return desc + whoQualifies;
                default:
                    throw new InvalidOperationException (
                        Localizer.Format (
                            "#autoLOC_CustomAsteroids_ErrorConditionStateBadRestriction",
                            whoQualifies));
                }
            }

            internal enum Test
            {
                Present,
                Orbiting,
                Landed
            }
        }

        class StringView : IEnumerator<string>
        {
            readonly IEnumerator<GamePredicate> baseEnum;

            internal StringView (IEnumerable<GamePredicate> impl)
            {
                baseEnum = impl.GetEnumerator ();
            }

            public bool MoveNext ()
            {
                return baseEnum.MoveNext ();
            }

            public void Reset ()
            {
                baseEnum.Reset ();
            }

            object System.Collections.IEnumerator.Current {
                get {
                    return Current;
                }
            }

            public void Dispose ()
            {
                baseEnum.Dispose ();
            }

            public string Current {
                get {
                    return unparse (baseEnum.Current);
                }
            }
        }
    }
}
