#nullable enable
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

// todo: array initializers

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(IArrayCreationExpression) },
  HighlightingTypes = new[]
  {
    typeof(ObjectAllocationEvidentHighlighting)
  })]
public class AllocationOfArrayCreationAnalyzer : HeapAllocationAnalyzerBase<IArrayCreationExpression>
{
  protected override void Run(
    IArrayCreationExpression arrayCreationExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (arrayCreationExpression.IsInTheContextWhereAllocationsAreNotImportant()) return;

    // todo: span of bytes

    var newKeyword = arrayCreationExpression.NewKeyword.NotNull();

    var createdArrayType = arrayCreationExpression.Type() as IArrayType;
    if (createdArrayType == null) return;

    var typeName = createdArrayType.GetPresentableName(arrayCreationExpression.Language, CommonUtils.DefaultTypePresentationStyle);

    consumer.AddHighlighting(
      new ObjectAllocationEvidentHighlighting(newKeyword, $"new '{typeName}' array instance creation"),
      newKeyword.GetDocumentRange());
  }
}