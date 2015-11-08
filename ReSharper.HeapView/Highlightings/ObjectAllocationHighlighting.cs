using JetBrains.Annotations;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Feature.Services.Daemon;
// ReSharper disable MemberCanBePrivate.Global

[assembly: RegisterConfigurableSeverity(
  ObjectAllocationHighlighting.SEVERITY_ID, null,
  HeapViewHighlightingsGroupIds.ID, "Object allocation",
  "Highlights language construct or expression where object allocation happens",
  Severity.HINT, false)]

[assembly: RegisterConfigurableSeverity(
  ObjectAllocationEvidentHighlighting.SEVERITY_ID, null,
  HeapViewHighlightingsGroupIds.ID, "Object allocation (evident)",
  "Highlights object creation expressions where explicit allocation happens",
  Severity.HINT, false)]

[assembly: RegisterConfigurableSeverity(
  ObjectAllocationPossibleHighlighting.SEVERITY_ID, null,
  HeapViewHighlightingsGroupIds.ID, "Object allocation (possible)",
  "Highlights language construct where object allocation can possibly happens",
  Severity.HINT, false)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [ConfigurableSeverityHighlighting(SEVERITY_ID, CSharpLanguage.Name,
    AttributeId = HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false, ToolTipFormatString = MESSAGE)]
  public class ObjectAllocationHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.ObjectAllocation";
    public const string MESSAGE = "Object allocation: {0}";

    public ObjectAllocationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }

  [ConfigurableSeverityHighlighting(SEVERITY_ID, CSharpLanguage.Name,
    AttributeId = HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false, ToolTipFormatString = MESSAGE)]
  public class ObjectAllocationEvidentHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.ObjectAllocation.Evident";
    public const string MESSAGE = "Object allocation: {0}";

    public ObjectAllocationEvidentHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }

  [ConfigurableSeverityHighlighting(SEVERITY_ID, CSharpLanguage.Name,
    AttributeId = HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false, ToolTipFormatString = MESSAGE)]
  public class ObjectAllocationPossibleHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.ObjectAllocation.Possible";
    public const string MESSAGE = "Possible object allocation: {0}";

    public ObjectAllocationPossibleHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }
}