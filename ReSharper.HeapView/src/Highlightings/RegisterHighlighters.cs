using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.TextControl.DocumentMarkup;

[assembly: RegisterHighlighter(
    HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID,
    EffectColor = "Red", EffectType = EffectType.SOLID_UNDERLINE,
    Layer = HighlighterLayer.SYNTAX, VSPriority = VSPriority.IDENTIFIERS)]

[assembly: RegisterHighlighter(
    HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
    EffectColor = "Orange", EffectType = EffectType.SOLID_UNDERLINE,
    Layer = HighlighterLayer.SYNTAX, VSPriority = VSPriority.IDENTIFIERS)]

[assembly: RegisterHighlighter(
    HeapViewAttributeIds.STRUCT_COPY_ID,
    EffectColor = "SkyBlue", EffectType = EffectType.SOLID_UNDERLINE,
    Layer = HighlighterLayer.SYNTAX, VSPriority = VSPriority.IDENTIFIERS)]

