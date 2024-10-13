using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.UI.RichText;

namespace ReSharperPlugin.HeapView.Highlightings;

[UsedImplicitly(
  ImplicitUseKindFlags.Access,
  ImplicitUseTargetFlags.WithMembers | ImplicitUseTargetFlags.WithInheritors)]
public abstract class PerformanceHighlightingBase(ITreeNode element, string format, RichText description)
  : IRichTextToolTipHighlighting
{
  public bool IsValid() => element.IsValid();

  public DocumentRange CalculateRange()
  {
    if (element is ICSharpExpression expression)
      return expression.GetExpressionRange();

    return element.GetDocumentRange();
  }

  public RichText RichToolTip { get; } = RichText.Format(format, description);
  public string ToolTip => RichToolTip.Text;
  public string ErrorStripeToolTip => ToolTip;

  public RichTextBlock? TryGetTooltip(HighlighterTooltipKind where)
  {
    return RichTextToolTipHighlighting.CreateBlock(RichToolTip, CSharpLanguage.Instance!, where);
  }
}