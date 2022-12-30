using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.HeapView.Highlightings;

[UsedImplicitly(ImplicitUseKindFlags.Access,ImplicitUseTargetFlags.WithMembers | ImplicitUseTargetFlags.WithInheritors)]
public abstract class PerformanceHighlightingBase : IHighlighting
{
  private readonly ITreeNode myElement;

  protected PerformanceHighlightingBase(ITreeNode element, string format, string description)
  {
    myElement = element;
    ToolTip = string.Format(format, description);
  }

  public bool IsValid() => myElement.IsValid();

  public DocumentRange CalculateRange()
  {
    if (myElement is ICSharpExpression expression)
      return expression.GetExpressionRange();

    return myElement.GetDocumentRange();
  }

  public string ToolTip { get; }
  public string ErrorStripeToolTip => ToolTip;
}