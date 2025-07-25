// this file is based on code from anatawa12's AvatarOptimizer
// https://github.com/anatawa12/AvatarOptimizer/blob/master/Internal/Localization/Editor/AAOL10N.cs

using System.Collections.Generic;
using System;
using nadena.dev.ndmf.localization;
using UnityEditor;
using UnityEngine;

namespace dev.hazre.materialshaderreplacer
{
	/// <summary>
	/// Wrapper class for MaterialShaderReplacer Localizer.
	/// </summary>
	public static class MSRL10N
	{
		public static readonly IReadOnlyDictionary<SystemLanguage, string> SupportedLanguageMap = new Dictionary<SystemLanguage, string>
		{
				{ SystemLanguage.English, "en-us.po" },
				{ SystemLanguage.Japanese, "ja-jp.po" },
				{ SystemLanguage.German, "de-de.po" }
		};

		public static IEnumerable<string> SupportedLanguages => SupportedLanguageMap.Values;

		private static Localizer _localizer;
		private static bool _initializationAttempted = false;

		public static Localizer Localizer
		{
			get
			{
				if (_localizer == null && !_initializationAttempted)
				{
					InitializeLocalizer();
				}
				return _localizer;
			}
		}

		private static void InitializeLocalizer()
		{
			_initializationAttempted = true;

			try
			{
				_localizer = new Localizer("en-us", LoadLocalizationAssets);
			}
			catch (Exception e)
			{
				Debug.LogError($"MSRL10N: Failed to initialize localizer: {e.Message}");
				// Create a fallback localizer that just returns the keys
				_localizer = new Localizer("en-us", () => new List<LocalizationAsset>());
			}
		}

		private static List<LocalizationAsset> LoadLocalizationAssets()
		{
			var localizationFolder = AssetDatabase.GUIDToAssetPath("40f966fef79afb84485c651998c79e08");
			if (string.IsNullOrEmpty(localizationFolder))
			{
				Debug.LogError("MSRL10N: Localization folder GUID could not be resolved. Please check the GUID.");
				return new List<LocalizationAsset>();
			}
			localizationFolder += "/";

			var assets = new List<LocalizationAsset>();
			foreach (var lang in SupportedLanguages)
			{
				var assetPath = localizationFolder + lang;
				var asset = AssetDatabase.LoadAssetAtPath<LocalizationAsset>(assetPath);
				if (asset != null)
				{
					assets.Add(asset);
				}
				else
				{
					Debug.LogWarning($"MSRL10N: Could not load localization asset for language '{lang}' at path '{assetPath}'");
				}
			}

			if (assets.Count == 0)
			{
				Debug.LogWarning("MSRL10N: No localization assets were loaded. Localization will fallback to keys.");
			}

			return assets;
		}

		public static string Tr(string key)
		{
			if (Localizer == null)
			{
				Debug.LogWarning($"MSRL10N: Localizer not initialized, returning key: {key}");
				return key;
			}

			var result = Localizer.GetLocalizedString(key);

			// If we get back the key unchanged, it likely means localization failed
			if (result == key && !_initializationAttempted)
			{
				RefreshLocalizer();
				return Tr(key);
			}

			return result;
		}

		public static string? TryTr(string key)
		{
			if (Localizer?.TryGetLocalizedString(key, out var result) == true)
				return result;
			return null;
		}

		/// <summary>
		/// Forces reinitialization of the localizer. Call this if localization isn't working.
		/// </summary>
		public static void RefreshLocalizer()
		{
			_localizer = null;
			_initializationAttempted = false;
			Debug.Log("MSRL10N: Localizer refreshed. Next localization call will reinitialize.");
		}

		public static Action DrawCustomLanguagePicker { get; set; }

		public static void DrawLanguagePicker() => DrawCustomLanguagePicker?.Invoke();
	}
}