using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;
#if RESHARPER8
using JetBrains.ReSharper.Daemon;
#elif RESHARPER9
using JetBrains.ReSharper.Feature.Services.Daemon;
#endif

// todo: use in 9.0
//[assembly: RegisterHighlighter(
//  id: ObjectAllocationHighlighting.HIGHLIGHTING_ID,
//  EffectColor = "Blue",
//  EffectType = EffectType.SOLID_UNDERLINE,
//  Layer = HighlighterLayer.SYNTAX,
//  VSPriority = VSPriority.IDENTIFIERS)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [StaticSeverityHighlighting(
    severity: Severity.INFO,
    group: AllocationHighlightingGroupIds.PERFORMANCE_HINTS,
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