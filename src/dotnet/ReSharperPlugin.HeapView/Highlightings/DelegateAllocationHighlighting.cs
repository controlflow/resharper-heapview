using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

// ReSharper disable MemberCanBePrivate.Global

namespace ReSharperPlugin.HeapView.Highlightings
{
  [RegisterConfigurableSeverity(
    SEVERITY_ID, null,
    HeapViewHighlightingsGroupIds.ID, "Delegate allocation",
    "Highlights places where delegate instance creation happens",
    Severity.HINT)]
  [ConfigurableSeverityHighlighting(
    SEVERITY_ID, CSharpLanguage.Name,
    AttributeId = HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
    ShowToolTipInStatusBar = false,
    ToolTipFormatString = MESSAGE)]
  public class DelegateAllocationHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.DelegateAllocation";
    public const string MESSAGE = "Delegate allocation: {0}";

    public DelegateAllocationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }
}