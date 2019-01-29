using JetBrains.TextControl.DocumentMarkup;
using ReSharperPlugin.HeapView.Highlightings;

[assembly: RegisterHighlighter(
    HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID,
    GroupId = HeapViewAttributeIds.GROUP_ID,
    EffectColor = "Red", EffectType = EffectType.SOLID_UNDERLINE,
    Layer = HighlighterLayer.SYNTAX, VSPriority = VSPriority.IDENTIFIERS)]

[assembly: RegisterHighlighter(
    HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
    GroupId = HeapViewAttributeIds.GROUP_ID,
    EffectColor = "Orange", EffectType = EffectType.SOLID_UNDERLINE,
    Layer = HighlighterLayer.SYNTAX, VSPriority = VSPriority.IDENTIFIERS)]

[assembly: RegisterHighlighter(
    HeapViewAttributeIds.STRUCT_COPY_ID,
    GroupId = HeapViewAttributeIds.GROUP_ID,
    EffectColor = "SkyBlue", EffectType = EffectType.SOLID_UNDERLINE,
    Layer = HighlighterLayer.SYNTAX, VSPriority = VSPriority.IDENTIFIERS)]

[assembly:
    RegisterHighlighterGroup(
        HeapViewAttributeIds.GROUP_ID, "Heap Allocation Viewer", HighlighterGroupPriority.COMMON_SETTINGS,
        RiderNamesProviderType = typeof(HeapViewSettingsNamesProvider))]

namespace ReSharperPlugin.HeapView.Highlightings
{
    public class HeapViewSettingsNamesProvider : PrefixBasedSettingsNamesProvider
    {
        public HeapViewSettingsNamesProvider() : base("ReSharper HeapView", "HEAP_VIEW")
        {
        }
    }
}
