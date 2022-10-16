#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Impl.Query;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Tree.Query;
using JetBrains.Util.DataStructures.Collections;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

// TODO: finish me
// TODO: LINQ method calls in query syntax, similat to ordinary invocation

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(IQueryRangeVariableDeclaration) },
  HighlightingTypes = new[] { typeof(ObjectAllocationHighlighting) })]
public class AllocationOfAnonymousObjectForTransparentIdentifierAnalyzer : HeapAllocationAnalyzerBase<IQueryRangeVariableDeclaration>
{
  protected override void Run(
    IQueryRangeVariableDeclaration queryRangeVariableDeclaration, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var relatedDeclaredElements = queryRangeVariableDeclaration.DeclaredElement.RelatedDeclaredElements;
    if (relatedDeclaredElements.Count != 1) return;

    if (relatedDeclaredElements[0] is IQueryAnonymousTypeProperty { ContainingType: IQueryAnonymousType queryAnonymousType })
    {
      ReportAllocation(queryRangeVariableDeclaration, queryAnonymousType, consumer);
    }
  }

  private static void ReportAllocation(
    IQueryRangeVariableDeclaration queryRangeVariableDeclaration, IQueryAnonymousType queryAnonymousType, IHighlightingConsumer consumer)
  {
    if (queryRangeVariableDeclaration.IsInTheContextWhereAllocationsAreNotImportant()) return;

    using var stringBuilder = PooledStringBuilder.GetInstance();
    var builder = stringBuilder.Builder;

    void PresentTransparentIdentifierAnonymousType(IQueryAnonymousType anonymousType)
    {
      builder.Append('{');
      var first = true;

      foreach (var queryProperty in anonymousType.QueryProperties)
      {
        if (first)
          first = false;
        else
          builder.Append(", ");

        if (queryProperty.Type is IQueryAnonymousType innerAnonymousType)
          PresentTransparentIdentifierAnonymousType(innerAnonymousType);
        else
          builder.Append(queryProperty.ShortName);
      }

      builder.Append('}');
    }

    PresentTransparentIdentifierAnonymousType(queryAnonymousType);

    consumer.AddHighlighting(new ObjectAllocationHighlighting(
      queryRangeVariableDeclaration.NameIdentifier, "new anonymous type instance creation for range variables " + builder));
  }
}