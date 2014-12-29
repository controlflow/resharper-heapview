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
  id: SlowDelegateCreationHighlighting.SEVERITY_ID,
  compoundItemName: null,
  group: AllocationHighlightingGroupIds.ID,
  title: "Slow delegate creation",
  description: "Highlights delegate creation that is slow because of CLR x86 JIT",
  defaultSeverity: Severity.WARNING,
  solutionAnalysisRequired: false)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [ConfigurableSeverityHighlighting(
    configurableSeverityId: SEVERITY_ID,
    languages: CSharpLanguage.Name,
    AttributeId = HighlightingAttributeIds.UNRESOLVED_ERROR_ATTRIBUTE,
    OverlapResolve = OverlapResolveKind.NONE,
    ShowToolTipInStatusBar = false,
    ToolTipFormatString = MESSAGE)]
  public class SlowDelegateCreationHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.SlowDelegateCreation";
    public const string MESSAGE = "Slow delegate creation: {0}";

    public SlowDelegateCreationHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }
}