using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.HeapView.Highlightings;

[RegisterConfigurableSeverity(
  ID: SEVERITY_ID,
  CompoundItemName: null,
  Group: HeapViewHighlightingsGroupIds.ID,
  Title: "Closure creation can be eliminated",
  Description: "Highlights places where closure can be eliminated by using the overload(s) of containing method invocation, " +
               "allowing passing extra state parameter(s) to closure function",
  DefaultSeverity: Severity.SUGGESTION)]
[ConfigurableSeverityHighlighting(
  SEVERITY_ID, CSharpLanguage.Name,
  ShowToolTipInStatusBar = false,
  ToolTipFormatString = MESSAGE)]
public class CanEliminateClosureCreationHighlighting : PerformanceHighlightingBase
{
  public const string SEVERITY_ID = "HeapView.CanAvoidClosure";
  public const string MESSAGE = "Closure can be eliminated: {0}";

  public CanEliminateClosureCreationHighlighting(ITreeNode element)
    : base(element, MESSAGE, "method has overload to avoid closure creation") { }
}