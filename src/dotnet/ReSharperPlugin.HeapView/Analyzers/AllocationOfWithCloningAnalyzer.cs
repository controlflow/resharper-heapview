#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.TestRunner.Abstractions.Extensions;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(IWithExpression) },
  HighlightingTypes = new[] { typeof(ObjectAllocationHighlighting) })]
public class AllocationOfWithCloningAnalyzer : HeapAllocationAnalyzerBase<IWithExpression>
{
  protected override void Run(
    IWithExpression withExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (withExpression.RBrace == null) return;

    if (withExpression.IsInTheContextWhereAllocationsAreNotImportant()) return;

    var clonedType = withExpression.GetExpressionType().ToIType();
    if (clonedType is not { Classify: TypeClassification.REFERENCE_TYPE })
      return; // 'structValue with { }' or unfinished code

    var withKeyword = withExpression.WithKeyword.NotNull();
    var typeKind = clonedType is IAnonymousType ? "anonymous object instance" : "'record class' type instance";

    consumer.AddHighlighting(
      new ObjectAllocationHighlighting(withKeyword, "'with' expression cloning of " + typeKind));
  }
}