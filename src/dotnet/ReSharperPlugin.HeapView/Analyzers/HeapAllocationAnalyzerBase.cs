#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Settings;

namespace ReSharperPlugin.HeapView.Analyzers;

public abstract class HeapAllocationAnalyzerBase<TTreeNode> : IConditionalElementProblemAnalyzer
  where TTreeNode : class, ITreeNode
{
  protected abstract void Run(TTreeNode element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer);

  void IElementProblemAnalyzer.Run(ITreeNode element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    Run((TTreeNode)element, data, consumer);
  }

  protected virtual bool ShouldRun(IFile file, ElementProblemAnalyzerData data) => true;

  bool IConditionalElementProblemAnalyzer.ShouldRun(IFile file, ElementProblemAnalyzerData data)
  {
    return data.IsAllocationAnalysisEnabled() && ShouldRun(file, data);
  }
}