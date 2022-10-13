using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

// ReSharper disable MemberCanBePrivate.Global

namespace ReSharperPlugin.HeapView.Highlightings;

[RegisterConfigurableSeverity(
  ID: SEVERITY_ID,
  CompoundItemName: null,
  Group: HeapViewHighlightingsGroupIds.ID,
  Title: "Object allocation",
  Description: "Highlights language construct or expression where object allocation happens",
  DefaultSeverity: Severity.HINT)]
[ConfigurableSeverityHighlighting(
  SEVERITY_ID, CSharpLanguage.Name,
  AttributeId = HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
  OverlapResolve = OverlapResolveKind.WARNING,
  ShowToolTipInStatusBar = false,
  ToolTipFormatString = MESSAGE)]
public class ObjectAllocationHighlighting : PerformanceHighlightingBase
{
  public const string SEVERITY_ID = "HeapView.ObjectAllocation";
  public const string MESSAGE = "Object allocation: {0}";

  public ObjectAllocationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
    : base(element, MESSAGE, description) { }
}

[RegisterConfigurableSeverity(
  ID: SEVERITY_ID,
  CompoundItemName: null,
  Group: HeapViewHighlightingsGroupIds.ID,
  Title: "Object allocation (evident)",
  Description: "Highlights object creation expressions where explicit allocation happens",
  DefaultSeverity: Severity.HINT)]
[ConfigurableSeverityHighlighting(
  SEVERITY_ID, CSharpLanguage.Name,
  AttributeId = HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
  OverlapResolve = OverlapResolveKind.WARNING,
  ShowToolTipInStatusBar = false,
  ToolTipFormatString = MESSAGE)]
public class ObjectAllocationEvidentHighlighting : PerformanceHighlightingBase
{
  public const string SEVERITY_ID = "HeapView.ObjectAllocation.Evident";
  public const string MESSAGE = "Object allocation: {0}";

  public ObjectAllocationEvidentHighlighting([NotNull] ITreeNode element, [NotNull] string description)
    : base(element, MESSAGE, description) { }
}

[RegisterConfigurableSeverity(
  ID: SEVERITY_ID,
  CompoundItemName: null,
  Group: HeapViewHighlightingsGroupIds.ID,
  Title: "Object allocation (possible)",
  Description: "Highlights language construct where object allocation can possibly happens",
  DefaultSeverity: Severity.HINT)]
[ConfigurableSeverityHighlighting(
  SEVERITY_ID, CSharpLanguage.Name,
  AttributeId = HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
  OverlapResolve = OverlapResolveKind.WARNING,
  ShowToolTipInStatusBar = false,
  ToolTipFormatString = MESSAGE)]
public class ObjectAllocationPossibleHighlighting : PerformanceHighlightingBase
{
  public const string SEVERITY_ID = "HeapView.ObjectAllocation.Possible";
  public const string MESSAGE = "Possible object allocation: {0}";

  public ObjectAllocationPossibleHighlighting([NotNull] ITreeNode element, [NotNull] string description)
    : base(element, MESSAGE, description) { }
}