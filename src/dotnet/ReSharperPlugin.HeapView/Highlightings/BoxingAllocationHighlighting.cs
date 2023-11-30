using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.HeapView.Highlightings;

[RegisterConfigurableSeverity(
  ID: SEVERITY_ID,
  CompoundItemName: null,
  Group: HeapViewHighlightingsGroupIds.ID,
  Title: "Boxing allocation",
  Description: "Highlights language construct or expression where boxing happens",
  DefaultSeverity: Severity.HINT)]

[ConfigurableSeverityHighlighting(
  SEVERITY_ID, CSharpLanguage.Name,
  AttributeId = HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID,
  OverlapResolve = OverlapResolveKind.WARNING,
  ShowToolTipInStatusBar = false,
  ToolTipFormatString = MESSAGE)]
public class BoxingAllocationHighlighting(ITreeNode element, string description)
  : PerformanceHighlightingBase(element, MESSAGE, description)
{
  public const string SEVERITY_ID = "HeapView.BoxingAllocation";
  public const string MESSAGE = "Boxing allocation: {0}";
}

[RegisterConfigurableSeverity(
  ID: SEVERITY_ID,
  CompoundItemName: null,
  Group: HeapViewHighlightingsGroupIds.ID,
  Title: "Boxing allocation (possible)",
  Description: "Highlights language construct or expression where boxing possibly happens",
  DefaultSeverity: Severity.HINT)]

[ConfigurableSeverityHighlighting(
  SEVERITY_ID, CSharpLanguage.Name,
  AttributeId = HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID,
  OverlapResolve = OverlapResolveKind.WARNING,
  ShowToolTipInStatusBar = false,
  ToolTipFormatString = MESSAGE)]
public class PossibleBoxingAllocationHighlighting(ITreeNode element, string description)
  : PerformanceHighlightingBase(element, MESSAGE, description)
{
  public const string SEVERITY_ID = "HeapView.PossibleBoxingAllocation";
  public const string MESSAGE = "Possible boxing allocation: {0}";
}