// this file is based on code from anatawa12's AvatarOptimizer
// https://github.com/anatawa12/AvatarOptimizer/blob/master/Internal/Localization/Editor/AAOL10N.cs

using System.Collections.Generic;
using nadena.dev.ndmf.localization;
using nadena.dev.ndmf.ui;
using UnityEditor;
using UnityEngine;

namespace dev.hazre.materialshaderreplacer
{
  /// <summary>
  /// Wrapper class for MaterialShaderReplacer Localizer.
  /// </summary>
  public static class MSRL10N
  {
    public static readonly Localizer Localizer = new Localizer("en-us", () =>
    {
      var localizationFolder = AssetDatabase.GUIDToAssetPath("40f966fef79afb84485c651998c79e08");
      if (string.IsNullOrEmpty(localizationFolder))
      {
        Debug.LogError("MSRL10N: Localization folder GUID could not be resolved. Please check the GUID.");
        return new List<LocalizationAsset>();
      }
      localizationFolder += "/";

      return new List<LocalizationAsset>
        {
          AssetDatabase.LoadAssetAtPath<LocalizationAsset>(localizationFolder + "en-us.po"),
          AssetDatabase.LoadAssetAtPath<LocalizationAsset>(localizationFolder + "ja-jp.po"),
          AssetDatabase.LoadAssetAtPath<LocalizationAsset>(localizationFolder + "de-de.po"),
        };
    });

    public static string Tr(string key) => Localizer.GetLocalizedString(key);

    public static string? TryTr(string key)
    {
      if (Localizer.TryGetLocalizedString(key, out var result))
        return result;
      return null;
    }

    public static void DrawLanguagePicker() => LanguageSwitcher.DrawImmediate();
  }
}