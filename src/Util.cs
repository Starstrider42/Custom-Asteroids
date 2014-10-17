using System;
using System.Reflection;
using UnityEngine;

namespace Starstrider42 {

	/** Simple implementation of an ordered pair.
	 * 
	 * Works around the lack of tuple support in .NET 3.5, and can 
	 * 	do things that KeyValuePair can't
	 * 
	 * @tparam T The type of the first pair element
	 * @tparam U The type of the second pair element
	 */
	internal class Pair<T, U> {
		/** Should have a default constructor */
		public Pair() {
		}

		/** Creates a new ordered pair
		 * 
		 * @param[in] first,second The values to store
		 * 
		 * @post The new object represents the pair (first, second).
		 * 
		 * @exceptsafe Does not throw exceptions.
		 */
		public Pair(T first, U second) {
			this.First = first;
			this.Second = second;
		}

		/** The first element of the pair */
		[Persistent] public T First { get; set; }
		/** The second element of the pair */
		[Persistent] public U Second { get; set; }
	}

	namespace CustomAsteroids {
		/** General-purpose functions that don't belong elsewhere
		 */
		internal static class Util {

			/** Prints an error message visible to the player
			 * @param[in] format The error message, or a composite format string for the message.
			 * @param[in] param Any parameters to place in the composite format string (optional).
			 * 
			 * @exceptsafe Does not throw exceptions
			 * 
			 * @note Based on code from RemoteTech
			 */
			public static void ErrorToPlayer(string format, params object[] param) {
				if (AsteroidManager.getOptions().getErrorReporting()) {
					ScreenMessages.PostScreenMessage(new ScreenMessage(
						String.Format("CustomAsteroids: " + format, param), 5.0f, ScreenMessageStyle.UPPER_RIGHT));
				}
			}
		}

		/** Boilerplate code for adding a scenario module to newly started games
		 */
		internal class AddScenario<SM> : MonoBehaviour
			where SM: ScenarioModule {
			/** Called on the frame when a script is enabled just before any of the Update methods is called the first time.
			 * 
			 * @see[Unity Documentation] (http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.Start.html)
			 * 
			 * @todo What exceptions are thrown by StartCoroutine?
			 */
			public void Start()
			{
				StartCoroutine("confirmScenarioAdded");
			}

			/** This function is called when the object will be destroyed.
			 * 
			 * @see [Unity Documentation] (http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.OnDestroy.html)
			 * 
			 * @todo What exceptions are thrown by StopCoroutine?
			 */
			public void OnDestroy() {
				StopCoroutine("confirmScenarioAdded");
			}

			/** Ensures the scenario is added
			 * 
			 * @return Controls the delay before execution resumes
			 * 
			 * @see [Unity documentation](http://docs.unity3d.com/Documentation/ScriptReference/MonoBehaviour.StartCoroutine.html)
			 * 
			 * @post The currently loaded game has an instance of the SM scenario module
			 */
			private System.Collections.IEnumerator confirmScenarioAdded() {
				while (HighLogic.CurrentGame.scenarios[0].moduleRef == null) {
					yield return 0;
				}

				ProtoScenarioModule curSpawner = HighLogic.CurrentGame.scenarios.
					Find(scenario => scenario.moduleRef is CustomAsteroidSpawner);

				if (curSpawner == null) {
					Debug.Log("CustomAsteroids: Adding " + typeof(SM).Name + " to game '" 
						+ HighLogic.CurrentGame.Title + "'");
					HighLogic.CurrentGame.AddProtoScenarioModule(typeof(SM), 
						GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT);
				}
			}
		}
	}
}
