using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl.DocumentMarkup;

[assembly: RegisterHighlighter(
  id: ObjectAllocationHighlighting.HIGHLIGHTING_ID,
  EffectColor = "Blue",
  EffectType = EffectType.SOLID_UNDERLINE,
  Layer = HighlighterLayer.SYNTAX,
  VSPriority = VSPriority.IDENTIFIERS)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [StaticSeverityHighlighting(
    severity: Severity.INFO,
    group: PerformanceHighlightingGroupIds.PERFORMANCE_HINTS,
    OverlapResolve = OverlapResolveKind.NONE,
    AttributeId = HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = true)]
  public class ObjectAllocationHighlighting : PerformanceHighlightingBase
  {
    public const string HIGHLIGHTING_ID = "ReSharper Heap Allocation";

    public ObjectAllocationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, "Heap allocation", description) { }
  }
}