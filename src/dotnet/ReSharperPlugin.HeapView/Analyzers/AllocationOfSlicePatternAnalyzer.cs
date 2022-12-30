using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(IListPattern) },
  HighlightingTypes = new[] { typeof(ObjectAllocationHighlighting) })]
public class AllocationOfSlicePatternAnalyzer : HeapAllocationAnalyzerBase<IListPattern>
{
  protected override void Run(
    IListPattern listPattern, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (listPattern.RBracket == null) return;

    ISlicePattern? singleSlicePattern = null;

    foreach (var pattern in listPattern.PatternsEnumerable)
    {
      if (pattern is ISlicePattern slicePattern)
      {
        if (singleSlicePattern != null) return; // two slices

        singleSlicePattern = slicePattern;
      }
    }

    if (singleSlicePattern == null) return; // no slicing

    var sliceDestination = singleSlicePattern.Pattern;
    if (sliceDestination == null || sliceDestination.IsDiscardOrDiscardedVarPattern()) return; // slicing not performed

    var slicedType = AllocationOfRangeExpressionAnalyzer.TryGetPatternBasedSlicedType(singleSlicePattern.RangeIndexerReference);
    if (slicedType == null) return;

    if (slicedType.IsString() || slicedType is IArrayType)
    {
      if (listPattern.IsInTheContextWhereAllocationsAreNotImportant()) return;

      var typeKind = slicedType.IsString() ? "string" : "array";
      consumer.AddHighlighting(
        new ObjectAllocationHighlighting(singleSlicePattern.OperatorSign,  $"slicing of the {typeKind} creates new {typeKind} instance"));
    }
  }
}