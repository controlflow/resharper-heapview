using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.HeapView.Highlightings;

public abstract class PerformanceHighlightingBase : IHighlighting
{
  [NotNull] private readonly ITreeNode myElement;

  protected PerformanceHighlightingBase([NotNull] ITreeNode element, [NotNull] string format, [NotNull] string description)
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

  [NotNull] public string ToolTip { get; }
  [NotNull] public string ErrorStripeToolTip => ToolTip;
}