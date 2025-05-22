using System;
using UnityEngine;

namespace dev.hazre.materialshaderreplacer
{
  public sealed class MSRLocalizedAttribute : PropertyAttribute
  {
    public string LocalizationKey { get; }
    public string? TooltipKey { get; }

    public MSRLocalizedAttribute(string localizationKey) : this(localizationKey, null) { }

    public MSRLocalizedAttribute(string localizationKey, string? tooltipKey)
    {
      LocalizationKey = localizationKey ?? throw new ArgumentNullException(nameof(localizationKey));
      TooltipKey = tooltipKey;
    }
  }
}