using JetBrains.ReSharper.HeapView;
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
  BoxingAllocationHighlighting.SEVERITY_ID, null, AllocationHighlightingGroupIds.ID,
  "Boxing allocation", "Highlights language construct or expression where boxing happens",
  Severity.HINT, solutionAnalysisRequired: false)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [ConfigurableSeverityHighlighting(
    SEVERITY_ID, CSharpLanguage.Name, AttributeId = Compatibility.BOXING_HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false, ToolTipFormatString = MESSAGE)]
  public class BoxingAllocationHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.BoxingAllocation";
    public const string MESSAGE = "Boxing allocation: {0}";

    public BoxingAllocationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }
}