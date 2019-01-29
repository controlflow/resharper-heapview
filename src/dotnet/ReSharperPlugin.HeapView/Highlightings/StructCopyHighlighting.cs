using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Feature.Services.Daemon;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable RedundantArgumentName
// ReSharper disable RedundantArgumentNameForLiteralExpression

/*
[assembly: RegisterConfigurableSeverity(
  id: StructCopyHighlighting.SEVERITY_ID,
  compoundItemName: null,
  group: HeapViewHighlightingsGroupIds.ID,
  title: "Struct copy",
  description: "Highlights expressions where struct copying happens",
  defaultSeverity: Severity.HINT,
  solutionAnalysisRequired: false)]

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  [ConfigurableSeverityHighlighting(
    configurableSeverityId: SEVERITY_ID,
    languages: CSharpLanguage.Name,
    AttributeId = HeapViewAttributeIds.STRUCT_COPY_ID,
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
*/