using JetBrains.Annotations;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
#if RESHARPER8
using JetBrains.ReSharper.Daemon;
#elif RESHARPER9
using JetBrains.ReSharper.Feature.Services.Daemon;
#endif
// ReSharper disable MemberCanBePrivate.Global

[assembly: RegisterConfigurableSeverity(
  ClosureAllocationHighlighting.SEVERITY_ID, null, AllocationHighlightingGroupIds.ID,
  "Closure allocation", "Highlights places where closure class creation happens",
  Severity.HINT, solutionAnalysisRequired: false)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [ConfigurableSeverityHighlighting(
    SEVERITY_ID, CSharpLanguage.Name, AttributeId = Compatibility.ALLOCATION_HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false, ToolTipFormatString = MESSAGE)]
  public class ClosureAllocationHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.ClosureAllocation";
    public const string MESSAGE = "Closure allocation: {0}";

    public ClosureAllocationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }
}