using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;

// ReSharper disable MemberCanBePrivate.Global

namespace ReSharperPlugin.HeapView.Highlightings;

[RegisterConfigurableSeverity(
  ID: SEVERITY_ID,
  CompoundItemName: null,
  Group: HeapViewHighlightingsGroupIds.ID,
  Title: "Implicitly captured variables",
  Description: "Highlights places where closure implicitly captures some variables " +
               "that can contain references, resulting in memory leaks",
  DefaultSeverity: Severity.DO_NOT_SHOW)]
[ConfigurableSeverityHighlighting(
  SEVERITY_ID, CSharpLanguage.Name,
  OverlapResolve = OverlapResolveKind.WARNING,
  ShowToolTipInStatusBar = false,
  ToolTipFormatString = MESSAGE)]
public class ImplicitCaptureWarning(ITreeNode element, string description)
  : PerformanceHighlightingBase(element, MESSAGE, description)
{
  public const string SEVERITY_ID = "HeapView.ImplicitCapture";
  public const string MESSAGE = "Implicit capture of {0}";
}