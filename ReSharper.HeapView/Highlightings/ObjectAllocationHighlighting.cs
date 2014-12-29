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
  id: ObjectAllocationHighlighting.SEVERITY_ID,
  compoundItemName: null,
  group: AllocationHighlightingGroupIds.ID,
  title: "Object allocation",
  description: "Highlights language construct or expression where object allocation happens",
  defaultSeverity: Severity.HINT,
  solutionAnalysisRequired: false)]

[assembly: RegisterConfigurableSeverity(
  id: ObjectAllocationEvidentHighlighting.SEVERITY_ID,
  compoundItemName: null,
  group: AllocationHighlightingGroupIds.ID,
  title: "Object allocation (evident)",
  description: "Highlights object creation expressions where explicit allocation happens",
  defaultSeverity: Severity.HINT,
  solutionAnalysisRequired: false)]

[assembly: RegisterConfigurableSeverity(
  id: ObjectAllocationEvidentHighlighting.SEVERITY_ID,
  compoundItemName: null,
  group: AllocationHighlightingGroupIds.ID,
  title: "Object allocation (possible)",
  description: "Highlights language construct or expression where possible object allocation happens",
  defaultSeverity: Severity.HINT,
  solutionAnalysisRequired: false)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [ConfigurableSeverityHighlighting(
    configurableSeverityId: SEVERITY_ID,
    languages: CSharpLanguage.Name,
    AttributeId = Compatibility.ALLOCATION_HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false,
    ToolTipFormatString = MESSAGE)]
  public class ObjectAllocationHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.ObjectAllocation";
    public const string MESSAGE = "Object allocation: {0}";

    public ObjectAllocationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }

  [ConfigurableSeverityHighlighting(
    configurableSeverityId: SEVERITY_ID,
    languages: CSharpLanguage.Name,
    AttributeId = Compatibility.ALLOCATION_HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false,
    ToolTipFormatString = MESSAGE)]
  public class ObjectAllocationEvidentHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.ObjectAllocation.Evident";
    public const string MESSAGE = "Object allocation: {0}";

    public ObjectAllocationEvidentHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }

  [ConfigurableSeverityHighlighting(
    configurableSeverityId: SEVERITY_ID,
    languages: CSharpLanguage.Name,
    AttributeId = Compatibility.ALLOCATION_HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false,
    ToolTipFormatString = MESSAGE)]
  public class ObjectAllocationPossibleHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.ObjectAllocation.Possible";
    public const string MESSAGE = "Possible object allocation: {0}";

    public ObjectAllocationPossibleHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }
}