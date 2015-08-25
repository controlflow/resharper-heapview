using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.HeapView.Highlightings
{
  public abstract class PerformanceHighlightingBase : IHighlighting
  {
    [NotNull] private readonly string myDescription;
    [NotNull] private readonly ITreeNode myElement;

    protected PerformanceHighlightingBase([NotNull] ITreeNode element, [NotNull] string format, [NotNull] string description)
    {
      myElement = element;
      myDescription = string.Format(format, description);
    }

    public bool IsValid() { return myElement.IsValid(); }

    public DocumentRange CalculateRange()
    {
      return myElement.GetDocumentRange();
    }

    public string ToolTip { get { return myDescription; } }
    public string ErrorStripeToolTip { get { return ToolTip; } }

    public int NavigationOffsetPatch { get { return 0; } }
  }
}