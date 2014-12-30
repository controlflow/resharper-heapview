using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
#if RESHARPER8
using JetBrains.ReSharper.Daemon;
#elif RESHARPER9
using JetBrains.ReSharper.Feature.Services.Daemon;
#endif
// ReSharper disable MemberCanBePrivate.Global

[assembly: RegisterConfigurableSeverity(
  id: StructCopyHighlighting.SEVERITY_ID,
  compoundItemName: null,
  group: AllocationHighlightingGroupIds.ID,
  title: "Struct copy",
  description: "Highlights expressions where struct copying happens",
  defaultSeverity: Severity.HINT,
  solutionAnalysisRequired: false)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [ConfigurableSeverityHighlighting(
    configurableSeverityId: SEVERITY_ID,
    languages: CSharpLanguage.Name,
    AttributeId = Compatibility.STRUCT_COPY_ID,
    ShowToolTipInStatusBar = false,
    ToolTipFormatString = MESSAGE)]
  public class StructCopyHighlighting : PerformanceHighlightingBase
  {
    public const string SEVERITY_ID = "HeapView.StructCopy";
    public const string MESSAGE = "Struct copy: {0}";

    public StructCopyHighlighting([NotNull] ITreeNode element, [NotNull] string description)
      : base(element, MESSAGE, description) { }
  }
}