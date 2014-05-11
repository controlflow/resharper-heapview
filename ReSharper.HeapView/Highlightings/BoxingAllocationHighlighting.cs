using JetBrains.Annotations;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
#if RESHARPER8
using JetBrains.ReSharper.Daemon;
#elif RESHARPER9
using JetBrains.ReSharper.Feature.Services.Daemon;
#endif

// TODO: use this in 9.0 with SharedShell
//[assembly: RegisterHighlighter(
//  id: BoxingAllocationHighlighting.HIGHLIGHTING_ID,
//  EffectColor = "Green", EffectType = EffectType.SOLID_UNDERLINE,
//  Layer = HighlighterLayer.SYNTAX, VSPriority = VSPriority.IDENTIFIERS)]

[assembly: RegisterConfigurableSeverity(
  BoxingAllocationHighlighting.SEVERITY_ID, null, AllocationHighlightingGroupIds.ID,
  "Boxing allocation", "Highlights language construct or expression where boxing happens",
  Severity.HINT, solutionAnalysisRequired: false)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [ConfigurableSeverityHighlighting(
    SEVERITY_ID, CSharpLanguage.Name, AttributeId = HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false, ToolTipFormatString = MESSAGE)]
  public class BoxingAllocationHighlighting : PerformanceHighlightingBase
  {
    public const string HIGHLIGHTING_ID = "ReSharper Boxing Occurance";
    public const string SEVERITY_ID = "HeapView.BoxingAllocation";
    public const string MESSAGE = "Boxing allocation: {0}";

    public BoxingAllocationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }
}