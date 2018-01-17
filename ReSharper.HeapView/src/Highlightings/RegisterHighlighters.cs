using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.TextControl.DocumentMarkup;

[assembly: RegisterHighlighter(
    HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID,
#if RESHARPER2017_3
    GroupId = HeapViewAttributeIds.GROUP_ID,
#endif
    EffectColor = "Red", EffectType = EffectType.SOLID_UNDERLINE,
    Layer = HighlighterLayer.SYNTAX, VSPriority = VSPriority.IDENTIFIERS)]

[assembly: RegisterHighlighter(
    HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
#if RESHARPER2017_3
    GroupId = HeapViewAttributeIds.GROUP_ID,
#endif
    EffectColor = "Orange", EffectType = EffectType.SOLID_UNDERLINE,
    Layer = HighlighterLayer.SYNTAX, VSPriority = VSPriority.IDENTIFIERS)]

[assembly: RegisterHighlighter(
    HeapViewAttributeIds.STRUCT_COPY_ID,
#if RESHARPER2017_3
    GroupId = HeapViewAttributeIds.GROUP_ID,
#endif
    EffectColor = "SkyBlue", EffectType = EffectType.SOLID_UNDERLINE,
    Layer = HighlighterLayer.SYNTAX, VSPriority = VSPriority.IDENTIFIERS)]

#if RESHARPER2017_3

[assembly:
    RegisterHighlighterGroup(
        HeapViewAttributeIds.GROUP_ID, "Heap Allocation Viewer", HighlighterGroupPriority.COMMON_SETTINGS,
        RiderNamesProviderType = typeof(HeapViewSettingsNamesProvider),
        DemoText =
@"<CSHARP_KEYWORD>struct</CSHARP_KEYWORD> <STRUCT_IDENTIFIER>Boxing</STRUCT_IDENTIFIER>
{
    <CSHARP_KEYWORD>void</CSHARP_KEYWORD> <METHOD_IDENTIFIER>M</METHOD_IDENTIFIER>(<CSHARP_KEYWORD>string</CSHARP_KEYWORD> <PARAMETER_IDENTIFIER>a</PARAMETER_IDENTIFIER>)
    {
        <CSHARP_KEYWORD>object</CSHARP_KEYWORD> <LOCAL_VARIABLE_IDENTIFIER>obj</LOCAL_VARIABLE_IDENTIFIER> = <CSHARP_NUMBER><HEAP_VIEW_BOXING>42</HEAP_VIEW_BOXING></CSHARP_NUMBER>; <CSHARP_LINE_COMMENT>// implicit conversion Int32 ~> Object</CSHARP_LINE_COMMENT>
        <CSHARP_KEYWORD>string</CSHARP_KEYWORD> <LOCAL_VARIABLE_IDENTIFIER>path</LOCAL_VARIABLE_IDENTIFIER> = <PARAMETER_IDENTIFIER>a</PARAMETER_IDENTIFIER> <HEAP_VIEW_ALLOCATION>+</HEAP_VIEW_ALLOCATION> <HEAP_VIEW_BOXING>'/'</HEAP_VIEW_BOXING> + <LOCAL_VARIABLE_IDENTIFIER>obj</LOCAL_VARIABLE_IDENTIFIER>; <CSHARP_LINE_COMMENT>// implicit conversion Char ~> Object</CSHARP_LINE_COMMENT>
        <CSHARP_KEYWORD>int</CSHARP_KEYWORD> <LOCAL_VARIABLE_IDENTIFIER>code</LOCAL_VARIABLE_IDENTIFIER> = <CSHARP_KEYWORD>this</CSHARP_KEYWORD>.<METHOD_IDENTIFIER><HEAP_VIEW_BOXING>GetHashCode</HEAP_VIEW_BOXING></METHOD_IDENTIFIER>(); <CSHARP_LINE_COMMENT>// non-overriden virtual method call on struct</CSHARP_LINE_COMMENT>
    }
}")]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
    public class HeapViewSettingsNamesProvider : PrefixBasedSettingsNamesProvider
    {
        public HeapViewSettingsNamesProvider() : base("ReSharper HeapView", "HEAP_VIEW")
        {
        }
    }
}
#endif