using JetBrains.Annotations;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Feature.Services.Daemon;
// ReSharper disable MemberCanBePrivate.Global

[assembly: RegisterConfigurableSeverity(
  ClosureAllocationHighlighting.SEVERITY_ID, null,
  HeapViewHighlightingsGroupIds.ID, "Closure allocation",
  "Highlights places where closure class creation happens",
  Severity.HINT, false)]

[assembly: RegisterConfigurableSeverity(
  CanAvoidClosureHighlighting.SEVERITY_ID, null,
  HeapViewHighlightingsGroupIds.ID, "Can avoid closure creation",
  "Highlights places where closure can be eliminated by using the overload(s) of containing method invocation, " +
  "allowing passing extra state parameter(s) to closure function",
  Severity.SUGGESTION, false)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [ConfigurableSeverityHighlighting(
    SEVERITY_ID, CSharpLanguage.Name,
    AttributeId = HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false, ToolTipFormatString = MESSAGE)]
  public class ClosureAllocationHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.ClosureAllocation";
    public const string MESSAGE = "Closure allocation: {0}";

    public ClosureAllocationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }

  [ConfigurableSeverityHighlighting(
    SEVERITY_ID, CSharpLanguage.Name,
    ShowToolTipInStatusBar = false, ToolTipFormatString = MESSAGE)]
  public class CanAvoidClosureHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.CanAvoidClosure";
    public const string MESSAGE = "Closure allocation can be eliminated: {0}";

    public CanAvoidClosureHighlighting([NotNull] ITreeNode element)
      : base(element, MESSAGE, "this method has overload to avoid closure creation") { }
  }
}