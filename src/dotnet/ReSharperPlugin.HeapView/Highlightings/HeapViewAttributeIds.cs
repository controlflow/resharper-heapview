using JetBrains.TextControl.DocumentMarkup;

namespace ReSharperPlugin.HeapView.Highlightings
{
  [RegisterHighlighter(
    BOXING_HIGHLIGHTING_ID,
    GroupId = GROUP_ID,
    ForegroundColor = "Red",
    DarkForegroundColor = "Red",
    EffectType = EffectType.SOLID_UNDERLINE,
    Layer = HighlighterLayer.SYNTAX, VSPriority = VSPriority.IDENTIFIERS)]
  [RegisterHighlighter(
    ALLOCATION_HIGHLIGHTING_ID,
    GroupId = GROUP_ID,
    ForegroundColor = "Orange",
    DarkForegroundColor = "Orange",
    EffectType = EffectType.SOLID_UNDERLINE,
    Layer = HighlighterLayer.SYNTAX, VSPriority = VSPriority.IDENTIFIERS)]
  [RegisterHighlighter(
    STRUCT_COPY_ID,
    GroupId = GROUP_ID,
    ForegroundColor = "SkyBlue",
    DarkForegroundColor = "SkyBlue",
    EffectType = EffectType.SOLID_UNDERLINE,
    Layer = HighlighterLayer.SYNTAX, VSPriority = VSPriority.IDENTIFIERS)]
  [RegisterHighlighterGroup(
    GROUP_ID, "Heap Allocation Viewer", HighlighterGroupPriority.COMMON_SETTINGS,
    RiderNamesProviderType = typeof(HeapViewSettingsNamesProvider))]
  public static class HeapViewAttributeIds
  {
    public const string GROUP_ID = "ReSharper HeapView Boxing";

    public const string BOXING_HIGHLIGHTING_ID = "ReSharper HeapView Boxing";
    public const string ALLOCATION_HIGHLIGHTING_ID = "ReSharper HeapView Allocation";
    public const string STRUCT_COPY_ID = "ReSharper HeapView Struct copy";
  }
}
