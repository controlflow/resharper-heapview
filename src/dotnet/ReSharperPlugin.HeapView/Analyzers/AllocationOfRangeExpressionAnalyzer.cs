#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(IElementAccessExpression) },
  HighlightingTypes = new[] { typeof(ObjectAllocationHighlighting) })]
public class AllocationOfRangeExpressionAnalyzer : HeapAllocationAnalyzerBase<IElementAccessExpression>
{
  protected override void Run(
    IElementAccessExpression elementAccessExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (elementAccessExpression.RBracket == null) return;

    var singleArgument = elementAccessExpression.ArgumentsEnumerable.SingleItem?.Value;
    if (singleArgument == null) return;

    var resolveResult = elementAccessExpression.Reference.Resolve();
    if (!resolveResult.ResolveErrorType.IsAcceptable) return;

    if (resolveResult.DeclaredElement is CSharpByRangeIndexer { SliceMethod: null } byRangeIndexer)
    {
      var isString = byRangeIndexer.ReturnType.IsString();
      if (isString || byRangeIndexer.ReturnType is IArrayType)
      {
        if (elementAccessExpression.IsInTheContextWhereAllocationsAreNotImportant()) return;

        var typeKind = isString ? "string" : "array";
        ITreeNode nodeToHighlight = singleArgument is IRangeExpression rangeExpression ? rangeExpression.OperatorSign : singleArgument;
        consumer.AddHighlighting(
          new ObjectAllocationHighlighting(nodeToHighlight,  $"slicing of the {typeKind} creates new {typeKind} instance"));
      }
    }
  }
}