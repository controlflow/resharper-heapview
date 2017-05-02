using JetBrains.Annotations;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Feature.Services.Daemon;
// ReSharper disable MemberCanBePrivate.Global

[assembly: RegisterConfigurableSeverity(
  BoxingAllocationHighlighting.SEVERITY_ID, null,
  HeapViewHighlightingsGroupIds.ID, "Boxing allocation",
  "Highlights language construct or expression where boxing happens",
  Severity.HINT
#if !RESHARPER2016_3
  , false
#endif
  )]

[assembly: RegisterConfigurableSeverity(
  BoxingAllocationPossibleHighlighting.SEVERITY_ID, null,
  HeapViewHighlightingsGroupIds.ID, "Boxing allocation (possible)",
  "Highlights language construct or expression where boxing possibly happens",
  Severity.HINT
#if !RESHARPER2016_3
  , false
#endif
)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [ConfigurableSeverityHighlighting(
    SEVERITY_ID, CSharpLanguage.Name,
    AttributeId = HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false,
    ToolTipFormatString = MESSAGE)]
  public class BoxingAllocationHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.BoxingAllocation";
    public const string MESSAGE = "Boxing allocation: {0}";

    public BoxingAllocationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }

  [ConfigurableSeverityHighlighting(
    SEVERITY_ID, CSharpLanguage.Name,
    AttributeId = HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false,
    ToolTipFormatString = MESSAGE)]
  public class BoxingAllocationPossibleHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.BoxingAllocation.Possible";
    public const string MESSAGE = "Possible boxing allocation: {0}";

    public BoxingAllocationPossibleHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }
}