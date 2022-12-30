using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.HeapView.Highlightings;

[RegisterConfigurableSeverity(
  ID: SEVERITY_ID,
  CompoundItemName: null,
  Group: HeapViewHighlightingsGroupIds.ID,
  Title: "Delegate allocation",
  Description: "Highlights places where delegate instance creation happens",
  DefaultSeverity: Severity.HINT)]
[ConfigurableSeverityHighlighting(
  SEVERITY_ID, CSharpLanguage.Name,
  AttributeId = HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
  OverlapResolve = OverlapResolveKind.WARNING,
  ShowToolTipInStatusBar = false,
  ToolTipFormatString = MESSAGE)]
public class DelegateAllocationHighlighting : PerformanceHighlightingBase
{
  public const string SEVERITY_ID = "HeapView.DelegateAllocation";
  public const string MESSAGE = "Delegate allocation: {0}";

  public DelegateAllocationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
    : base(element, MESSAGE, description) { }
}