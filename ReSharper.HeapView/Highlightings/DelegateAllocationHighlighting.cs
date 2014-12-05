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
  DelegateAllocationHighlighting.SEVERITY_ID, null, AllocationHighlightingGroupIds.ID,
  "Delegate allocation", "Highlights places where delegate instance creation happens",
  Severity.HINT, solutionAnalysisRequired: false)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [ConfigurableSeverityHighlighting(
    SEVERITY_ID, CSharpLanguage.Name, AttributeId = Compatibility.ALLOCATION_HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false, ToolTipFormatString = MESSAGE)]
  public class DelegateAllocationHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.DelegateAllocation";
    public const string MESSAGE = "Delegate allocation: {0}";

    public DelegateAllocationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }
}