// this file is based on code from anatawa12's AvatarOptimizer
// https://github.com/anatawa12/AvatarOptimizer/blob/master/Internal/Localization/Editor/AAOL10N.cs

using System.Collections.Generic;
using System; // For Action
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
        { SystemLanguage.English, "en-us" },
        { SystemLanguage.Japanese, "ja-jp" },
        { SystemLanguage.German, "de-de" }
    };

    public static IEnumerable<string> SupportedLanguages => SupportedLanguageMap.Values;

    public static readonly Localizer Localizer = new Localizer("en-us", () =>
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
        var asset = AssetDatabase.LoadAssetAtPath<LocalizationAsset>(localizationFolder + lang + ".po");
        if (asset != null)
        {
          assets.Add(asset);
        }
        else
        {
          Debug.LogWarning($"MSRL10N: Could not load localization asset for language '{lang}' at path '{localizationFolder + lang + ".po"}'");
        }
      }
      return assets;
    });

    public static string Tr(string key) => Localizer.GetLocalizedString(key);

    public static string? TryTr(string key)
    {
      if (Localizer.TryGetLocalizedString(key, out var result))
        return result;
      return null;
    }

    public static Action DrawCustomLanguagePicker { get; set; }

    public static void DrawLanguagePicker() => DrawCustomLanguagePicker?.Invoke();
  }
}