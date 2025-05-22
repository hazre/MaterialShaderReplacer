using UnityEditor;
using UnityEngine;
using nadena.dev.ndmf.localization;

namespace dev.hazre.materialshaderreplacer
{
  [CustomPropertyDrawer(typeof(MSRLocalizedAttribute))]
  public class MSRLocalizedAttributeDrawer : PropertyDrawer
  {
    private GUIContent _label = new GUIContent();
    private string? _currentLanguage;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
      UpdateLabel(label);
      return EditorGUI.GetPropertyHeight(property, _label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
      UpdateLabel(label);
      EditorGUI.PropertyField(position, property, _label, true);
    }

    private void UpdateLabel(GUIContent originalLabel)
    {
      MSRLocalizedAttribute attr = (MSRLocalizedAttribute)attribute;

      if (_currentLanguage != LanguagePrefs.Language || _label.text != MSRL10N.Tr(attr.LocalizationKey))
      {
        _label.text = MSRL10N.Tr(attr.LocalizationKey);
        if (!string.IsNullOrEmpty(attr.TooltipKey))
        {
          _label.tooltip = MSRL10N.Tr(attr.TooltipKey);
        }
        else
        {
          _label.tooltip = originalLabel.tooltip;
        }
        _currentLanguage = LanguagePrefs.Language;
      }

      if (_label.image == null) _label.image = originalLabel.image;
    }
  }
}