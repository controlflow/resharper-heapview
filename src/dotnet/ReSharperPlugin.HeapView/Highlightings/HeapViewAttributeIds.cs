using JetBrains.Annotations;
using JetBrains.TextControl.DocumentMarkup;

namespace ReSharperPlugin.HeapView.Highlightings;

[RegisterHighlighter(
  BOXING_HIGHLIGHTING_ID,
  GroupId = GROUP_ID,
  ForegroundColor = "#34AFE5",
  DarkForegroundColor = "#34AFE5",
  EffectType = EffectType.SOLID_UNDERLINE,
  Layer = HighlighterLayer.SYNTAX)]

[RegisterHighlighter(
  ALLOCATION_HIGHLIGHTING_ID,
  GroupId = GROUP_ID,
  ForegroundColor = "#34AFE5",
  DarkForegroundColor = "#34AFE5",
  EffectType = EffectType.SOLID_UNDERLINE,
  Layer = HighlighterLayer.SYNTAX)]

[RegisterHighlighterGroup(
  GroupId: GROUP_ID,
  PresentableName: "Heap Allocation Viewer",
  Priority: HighlighterGroupPriority.COMMON_SETTINGS,
  RiderNamesProviderType = typeof(HeapViewSettingsNamesProvider))]

[PublicAPI]
public static class HeapViewAttributeIds
{
  public const string GROUP_ID = "ReSharper HeapView Boxing";

  public const string BOXING_HIGHLIGHTING_ID = "ReSharper HeapView Boxing";
  public const string ALLOCATION_HIGHLIGHTING_ID = "ReSharper HeapView Allocation";
}
