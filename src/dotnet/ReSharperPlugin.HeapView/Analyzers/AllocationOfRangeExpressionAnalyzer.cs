using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
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

    var slicedType = TryGetPatternBasedSlicedType(elementAccessExpression.Reference);
    if (slicedType == null) return;

    if (slicedType.IsString() || slicedType is IArrayType)
    {
      if (elementAccessExpression.IsInTheContextWhereAllocationsAreNotImportant()) return;

      var typeKind = slicedType.IsString() ? "string" : "array";
      ITreeNode nodeToHighlight = singleArgument is IRangeExpression rangeExpression ? rangeExpression.OperatorSign : singleArgument;
      consumer.AddHighlighting(
        new ObjectAllocationHighlighting(nodeToHighlight,  $"slicing of the {typeKind} creates new {typeKind} instance"));
    }
  }

  [Pure]
  public static IType? TryGetPatternBasedSlicedType(IReference rangeIndexerReference)
  {
    var resolveResult = rangeIndexerReference.Resolve();
    if (!resolveResult.ResolveErrorType.IsAcceptable) return null;

    if (resolveResult.DeclaredElement is CSharpByRangeIndexer { SliceMethod: null } byRangeIndexer)
    {
      return byRangeIndexer.ReturnType;
    }

    return null;
  }
}