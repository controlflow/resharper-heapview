#nullable enable
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(IAnonymousObjectCreationExpression) },
  HighlightingTypes = new[] { typeof(ObjectAllocationEvidentHighlighting) })]
public class AllocationOfAnonymousObjectAnalyzer : HeapAllocationAnalyzerBase<IAnonymousObjectCreationExpression>
{
  protected override void Run(
    IAnonymousObjectCreationExpression anonymousObjectCreationExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (anonymousObjectCreationExpression.IsInTheContextWhereAllocationsAreNotImportant()) return;

    var newKeyword = anonymousObjectCreationExpression.NewKeyword.NotNull();

    consumer.AddHighlighting(
      new ObjectAllocationEvidentHighlighting(newKeyword, "new anonymous type instance creation"),
      newKeyword.GetDocumentRange());
  }
}