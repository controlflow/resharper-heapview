using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.HeapView.Highlightings;

[UsedImplicitly(ImplicitUseKindFlags.Access,ImplicitUseTargetFlags.WithMembers | ImplicitUseTargetFlags.WithInheritors)]
public abstract class PerformanceHighlightingBase(ITreeNode element, string format, string description) : IHighlighting
{
  public bool IsValid() => element.IsValid();

  public DocumentRange CalculateRange()
  {
    if (element is ICSharpExpression expression)
      return expression.GetExpressionRange();

    return element.GetDocumentRange();
  }

  public string ToolTip { get; } = string.Format(format, description);
  public string ErrorStripeToolTip => ToolTip;
}