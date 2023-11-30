using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;

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
public class ObjectAllocationHighlighting(ITreeNode element, string description)
  : PerformanceHighlightingBase(element, MESSAGE, description)
{
  public const string SEVERITY_ID = "HeapView.ObjectAllocation";
  public const string MESSAGE = "Object allocation: {0}";
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
public class ObjectAllocationEvidentHighlighting(ITreeNode element, string description)
  : PerformanceHighlightingBase(element, MESSAGE, description)
{
  public const string SEVERITY_ID = "HeapView.ObjectAllocation.Evident";
  public const string MESSAGE = "Object allocation: {0}";
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
public class ObjectAllocationPossibleHighlighting(ITreeNode element, string description)
  : PerformanceHighlightingBase(element, MESSAGE, description)
{
  public const string SEVERITY_ID = "HeapView.ObjectAllocation.Possible";
  public const string MESSAGE = "Possible object allocation: {0}";
}