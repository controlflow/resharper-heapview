using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;
#if RESHARPER8
using JetBrains.ReSharper.Daemon;
#elif RESHARPER9
using JetBrains.ReSharper.Feature.Services.Daemon;
#endif

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [StaticSeverityHighlighting(
    severity: Severity.INFO,
    group: PerformanceHighlightingGroupIds.PERFORMANCE_HINTS,
    OverlapResolve = OverlapResolveKind.NONE,
    AttributeId = HighlightingAttributeIds.UNRESOLVED_ERROR_ATTRIBUTE,
    ShowToolTipInStatusBar = true)]
  public class SlowDelegateCreationHighlighting : PerformanceHighlightingBase
  {
    public SlowDelegateCreationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, "Slow delegate creation", description) { }
  }
}