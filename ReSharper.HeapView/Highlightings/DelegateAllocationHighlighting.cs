using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;

[assembly: RegisterConfigurableSeverity(
  DelegateAllocationHighlighting.SEVERITY_ID, null, AllocationHighlightingGroupIds.ID,
  "Delegate allocation", "Highlights places where delegate instance creation happens",
  Severity.HINT, solutionAnalysisRequired: false)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [ConfigurableSeverityHighlighting(
    SEVERITY_ID, CSharpLanguage.Name, AttributeId = HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false, ToolTipFormatString = MESSAGE)]
  public class DelegateAllocationHighlighting : PerformanceHighlightingBase
  {
    public const string HIGHLIGHTING_ID = "ReSharper Heap Allocation";
    public const string SEVERITY_ID = "HeapView.DelegateAllocation";
    public const string MESSAGE = "Delegate allocation: {0}";

    public DelegateAllocationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }
}