using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.UI.RichText;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: [ typeof(IObjectCreationExpression) ],
  HighlightingTypes = [
    typeof(ObjectAllocationEvidentHighlighting),
    typeof(ObjectAllocationPossibleHighlighting)
  ])]
public class AllocationOfObjectCreationAnalyzer : HeapAllocationAnalyzerBase<IObjectCreationExpression>
{
  protected override void Run(IObjectCreationExpression objectCreationExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var createdType = objectCreationExpression.Type() as IDeclaredType;
    if (createdType == null) return;

    switch (createdType.Classify)
    {
      case TypeClassification.REFERENCE_TYPE:
      case TypeClassification.UNKNOWN when createdType.IsTypeParameterType():
      {
        ReportObjectCreation(objectCreationExpression, createdType, consumer);
        break;
      }
    }
  }

  private static void ReportObjectCreation(
    IObjectCreationExpression objectCreationExpression, IDeclaredType createdType, IHighlightingConsumer consumer)
  {
    if (objectCreationExpression.IsInTheContextWhereAllocationsAreNotImportant()) return;

    if (createdType.IsDynamicType() || createdType is (IDelegate, _)) return;

    var newKeyword = objectCreationExpression.NewKeyword.NotNull();
    var typeName = createdType.GetPresentableName(objectCreationExpression.Language, CommonUtils.DefaultTypePresentationStyle);

    if (createdType.Classify == TypeClassification.REFERENCE_TYPE)
    {
      consumer.AddHighlighting(new ObjectAllocationEvidentHighlighting(
        newKeyword, new RichText(
          $"new '{typeName}' instance creation")));
    }
    else
    {
      consumer.AddHighlighting(new ObjectAllocationPossibleHighlighting(
        newKeyword, new RichText(
          $"new instance creation if '{typeName}' type parameter will be substituted with the reference type")));
    }
  }
}