#nullable enable
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Feature.Services.Daemon;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[]
  {
    // initializer scope
    typeof(IClassLikeDeclaration),
    // methods, properties, indexers
    typeof(ICSharpFunctionDeclaration),
    typeof(IExpressionBodyOwnerDeclaration),
    // top-level code
    typeof(ITopLevelCode)
  },
  HighlightingTypes = new[]
  {
    typeof(ClosureAllocationHighlighting)
  })]
public class AllocationOfClosuresAnalyzer : HeapAllocationAnalyzerBase<ITreeNode>
{
  protected override void Run(
    ITreeNode treeNode, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    using var structure = DisplayClassStructure.Build(treeNode);
    if (structure == null) return;

    structure.ReportDisplayClasses(consumer, static (node, description, consumer) =>
    {
      consumer.AddHighlighting(new ClosureAllocationHighlighting(node, description));
    });
  }
}