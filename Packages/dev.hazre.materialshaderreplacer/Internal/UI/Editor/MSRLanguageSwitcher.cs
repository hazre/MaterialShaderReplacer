using System.Linq;
using System.Globalization;
using nadena.dev.ndmf.localization;
using UnityEditor;
using UnityEngine;

namespace dev.hazre.materialshaderreplacer
{
	[InitializeOnLoad]
	public static class MSRLanguageSwitcherInitializer
	{
		static MSRLanguageSwitcherInitializer()
		{
			MSRL10N.DrawCustomLanguagePicker = MSRLanguageSwitcher.DrawImmediate;
		}
	}

	public static class MSRLanguageSwitcher
	{
		private static string MapUnitySystemLanguageToIetfTag(SystemLanguage lang)
		{
			if (MSRL10N.SupportedLanguageMap.TryGetValue(lang, out var ietfTag))
			{
				return ietfTag;
			}
			return MSRL10N.Localizer.DefaultLanguage;
		}

		public static void DrawImmediate()
		{
			var supportedLanguagesArray = MSRL10N.SupportedLanguages.ToArray();
			var packageDefaultLang = MSRL10N.Localizer.DefaultLanguage;

			string languageToDisplayAsSelected;

			// 1. Check NDMF's current language setting
			var ndmfCurrentGlobalLang = LanguagePrefs.Language;
			if (supportedLanguagesArray.Contains(ndmfCurrentGlobalLang))
			{
				languageToDisplayAsSelected = ndmfCurrentGlobalLang;
			}
			else
			{
				// 2. If NDMF global is not set or not supported by MSR, check Unity language
				var osUnityLangEnum = Application.systemLanguage;
				var osLangIetf = MapUnitySystemLanguageToIetfTag(osUnityLangEnum);

				if (supportedLanguagesArray.Contains(osLangIetf))
				{
					languageToDisplayAsSelected = osLangIetf;
				}
				else
				{
					// 3. Fallback to package's default language
					languageToDisplayAsSelected = packageDefaultLang;
					if (!supportedLanguagesArray.Contains(packageDefaultLang))
					{
						languageToDisplayAsSelected = supportedLanguagesArray.FirstOrDefault();
					}
				}
			}

			if (languageToDisplayAsSelected == null && supportedLanguagesArray.Length > 0)
			{
				languageToDisplayAsSelected = supportedLanguagesArray[0];
			}

			var curIndex = System.Array.IndexOf(supportedLanguagesArray, languageToDisplayAsSelected);
			if (curIndex == -1 && supportedLanguagesArray.Length > 0)
			{
				curIndex = 0;
			}

			var displayNames = supportedLanguagesArray.Select(lang =>
			{
				var nativeName = CultureInfo.GetCultureInfo(lang).NativeName;
				return string.IsNullOrEmpty(nativeName) ? lang : nativeName;
			}).ToArray();

			var newIndex = EditorGUILayout.Popup(MSRL10N.Tr("MSRLanguageSwitcher:EditorLanguageLabel"), curIndex, displayNames);

			if (newIndex != curIndex && newIndex >= 0 && newIndex < supportedLanguagesArray.Length)
			{
				LanguagePrefs.Language = supportedLanguagesArray[newIndex];
			}
		}
	}
}