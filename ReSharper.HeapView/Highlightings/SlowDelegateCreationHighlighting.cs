using JetBrains.Annotations;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
#if RESHARPER8
using JetBrains.ReSharper.Daemon;
#elif RESHARPER9
using JetBrains.ReSharper.Feature.Services.Daemon;
#endif

[assembly: RegisterConfigurableSeverity(
  SlowDelegateCreationHighlighting.SEVERITY_ID, null, AllocationHighlightingGroupIds.ID,
  "Slow delegate creation", "Highlights delegate creation that is slow because of CLR x86 JIT",
  Severity.ERROR, solutionAnalysisRequired: false)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [ConfigurableSeverityHighlighting(SEVERITY_ID, CSharpLanguage.Name,
    AttributeId = HighlightingAttributeIds.UNRESOLVED_ERROR_ATTRIBUTE,
    OverlapResolve = OverlapResolveKind.NONE, ShowToolTipInStatusBar = false,
    ToolTipFormatString = MESSAGE)]
  public class SlowDelegateCreationHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.SlowDelegateCreation";
    public const string MESSAGE = "Slow delegate creation: {0}";

    public SlowDelegateCreationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }
}