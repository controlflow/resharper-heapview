using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;

// ReSharper disable MemberCanBePrivate.Global

namespace ReSharperPlugin.HeapView.Highlightings;

[RegisterConfigurableSeverity(
  ID: SEVERITY_ID,
  CompoundItemName: null,
  Group: HeapViewHighlightingsGroupIds.ID,
  Title: "Implicitly captured",
  Description: "Highlights places where closure implicitly captures some variables that can contain references, resulting in memory leaks",
  DefaultSeverity: Severity.DO_NOT_SHOW)]
[ConfigurableSeverityHighlighting(
  SEVERITY_ID, CSharpLanguage.Name,
  AttributeId = HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
  OverlapResolve = OverlapResolveKind.WARNING,
  ShowToolTipInStatusBar = false,
  ToolTipFormatString = MESSAGE)]
public class ImplicitCaptureWarning : PerformanceHighlightingBase
{
  public const string SEVERITY_ID = "HeapView.ImplicitCapture";
  public const string MESSAGE = "Implicit capture of {0}";

  public ImplicitCaptureWarning([NotNull] ITreeNode element, [NotNull] string description)
    : base(element, MESSAGE, description) { }
}