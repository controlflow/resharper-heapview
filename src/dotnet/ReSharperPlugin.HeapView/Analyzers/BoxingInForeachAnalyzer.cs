#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[]
  {
    typeof(IForeachStatement)
  },
  HighlightingTypes = new[]
  {
    typeof(BoxingAllocationHighlighting),
    typeof(PossibleBoxingAllocationHighlighting)
  })]
public class BoxingInForeachAnalyzer : ElementProblemAnalyzer<IForeachStatement>
{
  protected override void Run(IForeachStatement foreachStatement, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    // foreach (object o in arrayOfInts) { }
    if (foreachStatement.ForeachHeader is
        {
          DeclarationExpression: { TypeUsage: { } explicitTypeUsage, Designation: ISingleVariableDesignation } declarationExpression,
          Collection: { } collection
        })
    {
      var collectionType = collection.Type();

      var elementType = CollectionTypeUtil.ElementTypeByCollectionType(collectionType, foreachStatement, foreachStatement.IsAwait);
      if (elementType != null)
      {
        BoxingOccurrenceAnalyzer.CheckConversionRequiresBoxing(
          sourceExpressionType: elementType, targetType: declarationExpression.Type(), explicitTypeUsage,
          static (conversionRule, source, target) => conversionRule.ClassifyImplicitConversionFromExpression(source, target),
          data, consumer);
      }
    }
  }
}