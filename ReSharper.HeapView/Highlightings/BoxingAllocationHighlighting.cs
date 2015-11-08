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
  Severity.HINT, false)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [ConfigurableSeverityHighlighting(SEVERITY_ID, CSharpLanguage.Name,
    AttributeId = HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false, ToolTipFormatString = MESSAGE)]
  public class BoxingAllocationHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.BoxingAllocation";
    public const string MESSAGE = "Boxing allocation: {0}";

    public BoxingAllocationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }
}