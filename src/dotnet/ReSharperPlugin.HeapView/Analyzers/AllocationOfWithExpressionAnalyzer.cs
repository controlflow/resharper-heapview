using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.TestRunner.Abstractions.Extensions;
using JetBrains.UI.RichText;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: [ typeof(IWithExpression) ],
  HighlightingTypes = [ typeof(ObjectAllocationHighlighting) ])]
public class AllocationOfWithExpressionAnalyzer : HeapAllocationAnalyzerBase<IWithExpression>
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

    var richText = new RichText()
      .Append('\'')
      .Append("with", DeclaredElementPresenterTextStyles.Generic[DeclaredElementPresentationPartKind.Keyword])
      .Append("' expression cloning of ");

    if (clonedType is IAnonymousType)
    {
      richText.Append("anonymous object instance");
    }
    else
    {
      richText
        .Append('\'')
        .Append("record class", DeclaredElementPresenterTextStyles.Generic[DeclaredElementPresentationPartKind.Keyword])
        .Append("' type instance");
    }

    consumer.AddHighlighting(new ObjectAllocationHighlighting(withKeyword, richText));
  }
}