using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Psi.Tree;

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