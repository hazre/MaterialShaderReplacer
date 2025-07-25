using UnityEditor;
using UnityEngine;

namespace dev.hazre.materialshaderreplacer
{
	/// <summary>
	/// Ensures proper initialization of localization system during editor startup
	/// </summary>
	[InitializeOnLoad]
	public static class MSRLocalizationInitializer
	{
		static MSRLocalizationInitializer()
		{
			// Delay initialization to ensure asset database is ready
			EditorApplication.delayCall += InitializeLocalization;
		}

		private static void InitializeLocalization()
		{
			// Force localizer initialization
			var test = MSRL10N.Tr("MaterialShaderReplacer:Window:Title");

			// If we got back the key instead of a translation, the localization failed
			if (test == "MaterialShaderReplacer:Window:Title")
			{
				Debug.LogWarning("MSRL10N: Initial localization test failed. Will retry on next usage.");
			}
		}
	}
}