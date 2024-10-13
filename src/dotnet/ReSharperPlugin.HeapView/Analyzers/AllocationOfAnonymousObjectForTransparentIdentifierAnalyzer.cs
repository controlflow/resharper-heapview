using System.Linq;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl.Query;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Tree.Query;
using JetBrains.UI.RichText;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: [ typeof(IQueryRangeVariableDeclaration) ],
  HighlightingTypes = [ typeof(ObjectAllocationHighlighting) ])]
public class AllocationOfAnonymousObjectForTransparentIdentifierAnalyzer : HeapAllocationAnalyzerBase<IQueryRangeVariableDeclaration>
{
  protected override void Run(
    IQueryRangeVariableDeclaration queryRangeVariableDeclaration, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var relatedDeclaredElements = queryRangeVariableDeclaration.DeclaredElement.RelatedDeclaredElements;
    if (relatedDeclaredElements.OfType<IQueryAnonymousTypeProperty>().SingleItem() is { ContainingType: IQueryAnonymousType queryAnonymousType }
        && QueryContinuationNavigator.GetByDeclaration(queryRangeVariableDeclaration) == null
        && QueryFirstFromNavigator.GetByDeclaration(queryRangeVariableDeclaration) == null)
    {
      ReportAllocation(queryRangeVariableDeclaration, queryAnonymousType, consumer);
    }
  }

  private static void ReportAllocation(
    IQueryRangeVariableDeclaration queryRangeVariableDeclaration, IQueryAnonymousType queryAnonymousType, IHighlightingConsumer consumer)
  {
    if (queryRangeVariableDeclaration.IsInTheContextWhereAllocationsAreNotImportant()) return;

    var richText = new RichText(
      "new anonymous type instance creation for range variables ");

    PresentTransparentIdentifierAnonymousType(queryAnonymousType);

    consumer.AddHighlighting(new ObjectAllocationHighlighting(
      queryRangeVariableDeclaration.NameIdentifier, richText));
    return;

    void PresentTransparentIdentifierAnonymousType(IQueryAnonymousType anonymousType)
    {
      richText.Append('{');
      var first = true;

      foreach (var queryProperty in anonymousType.QueryProperties)
      {
        if (first)
          first = false;
        else
          richText.Append(", ");

        if (queryProperty.Type is IQueryAnonymousType innerAnonymousType)
          PresentTransparentIdentifierAnonymousType(innerAnonymousType);
        else
          richText.Append(queryProperty.ShortName, DeclaredElementPresenterTextStyles.Generic[DeclaredElementPresentationPartKind.Property]);
      }

      richText.Append('}');
    }
  }
}