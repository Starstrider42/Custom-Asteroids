using System.Collections.Generic;
using UnityEngine;
using System;

namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// Represents asteroid-specific information not included in the stock game.
	/// </summary>
	public sealed class CustomAsteroidData : PartModule {
		/// <summary>The name of the composition class to use.</summary>
		/// <remarks>Intended for use by code.</remarks>
		[KSPField]
		public string composition;
		/// <summary>The name of the composition class to use.</summary>
		/// <remarks>Intended for display to the user.</remarks>
		[KSPField (guiActive = true, guiActiveEditor = true, guiName = "Type")]
		public string displayComposition;

		public CustomAsteroidData() {
			displayComposition = composition = "Stony";
		}

		#if DEBUG
		[KSPEvent(
			guiActive = true,
			guiActiveUnfocused = true,
			unfocusedRange = 1000,
			externalToEVAOnly = false,
			guiName = "Survey Asteroid")]
		public void dumpResources() {
			ModuleAsteroidInfo summary = part.FindModuleImplementing<ModuleAsteroidInfo>();
			List<ModuleAsteroidResource> resources = part.FindModulesImplementing<ModuleAsteroidResource>();

			if (summary != null) {
				double resourceMass = summary.currentMassVal - summary.massThresholdVal;

				System.Text.StringBuilder report = new System.Text.StringBuilder();
				report.AppendLine(String.Format(
						"ASTEROID {0} RESOURCE REPORT",
						part.FindModuleImplementing<ModuleAsteroid>().AsteroidName));
				report.AppendLine(String.Format("Class: {0}", composition));
				report.AppendLine(String.Format(
						"{0:F1} out of {1:F1} tons ({2:P1}) taken by resources",
						resourceMass,
						summary.currentMassVal,
						resourceMass / summary.currentMassVal));
				report.AppendLine();

				foreach (ModuleAsteroidResource resource in resources) {
					report.AppendLine(String.Format("Resource: {0}", resource.resourceName));
					report.AppendLine(String.Format(
							"\tAmount {0:F1} tons ({1:P1})",
							resource.abundance * resourceMass,
							resource.displayAbundance));
					if (resource.abundance < 1e-6) {
						report.AppendLine(String.Format("\tFound in only {0:D}% of asteroids.", resource.presenceChance));
					}
					report.AppendLine(String.Format("\tNominal abundance {0:D}-{1:D}%", resource.lowRange, resource.highRange));
					report.AppendLine();
				}

				Debug.Log(report.ToString());
				ScreenMessages.PostScreenMessage("Asteroid resources recorded and logged.", 
					5.0f, ScreenMessageStyle.UPPER_CENTER);
			}
		}
		#endif
	}
}
