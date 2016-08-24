using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  public abstract class PerformanceHighlightingBase : IHighlighting
  {
    [NotNull] private readonly ITreeNode myElement;

    protected PerformanceHighlightingBase([NotNull] ITreeNode element, [NotNull] string format, [NotNull] string description)
    {
      myElement = element;
      ToolTip = string.Format(format, description);
    }

    public bool IsValid() { return myElement.IsValid(); }

    public DocumentRange CalculateRange() => myElement.GetDocumentRange();

    [NotNull] public string ToolTip { get; }
    [NotNull] public string ErrorStripeToolTip => ToolTip;
    public int NavigationOffsetPatch => 0;
  }
}