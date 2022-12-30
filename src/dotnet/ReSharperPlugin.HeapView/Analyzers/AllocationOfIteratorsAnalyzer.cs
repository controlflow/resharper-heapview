#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.DeclaredElements;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(IReferenceExpression), typeof(IInvocationExpression) },
  HighlightingTypes = new[] { typeof(ObjectAllocationHighlighting) })]
public class AllocationOfIteratorsAnalyzer : HeapAllocationAnalyzerBase<ICSharpExpression>
{
  protected override void Run(
    ICSharpExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    switch (expression)
    {
      case IReferenceExpression referenceExpression
        when !referenceExpression.IsNameofOperatorArgumentPart():
      {
        var resolveResult = referenceExpression.Reference.Resolve();
        if (resolveResult.ResolveErrorType.IsAcceptable
            && resolveResult.DeclaredElement is IProperty property
            && (referenceExpression.GetAccessType() & ExpressionAccessType.Read) != 0
            && property.Getter is { IsIterator: true }
            && !referenceExpression.IsInTheContextWhereAllocationsAreNotImportant())
        {
          var iteratorType = resolveResult.Substitution[property.Type];
          var iteratorTypeName = iteratorType.GetPresentableName(expression.Language, CommonUtils.DefaultTypePresentationStyle);

          consumer.AddHighlighting(new ObjectAllocationHighlighting(
            referenceExpression, $"new '{iteratorTypeName}' instance creation on iterator property access"));
        }

        break;
      }

      case IInvocationExpression invocationExpression:
      {
        var resolveResult = invocationExpression.Reference.Resolve();
        if (resolveResult.ResolveErrorType.IsAcceptable
            && resolveResult.DeclaredElement is IParametersOwner parametersOwner and (IMethod { IsIterator: true } or ILocalFunction { IsIterator: true })
            && !invocationExpression.IsInTheContextWhereAllocationsAreNotImportant())
        {
          var iteratorType = resolveResult.Substitution[parametersOwner.ReturnType];
          var iteratorTypeName = iteratorType.GetPresentableName(expression.Language, CommonUtils.DefaultTypePresentationStyle);
          var iteratorKind = DeclaredElementPresenter.Format(expression.Language, DeclaredElementPresenter.KIND_PRESENTER, parametersOwner);

          consumer.AddHighlighting(new ObjectAllocationHighlighting(
            invocationExpression.InvokedExpression, $"new '{iteratorTypeName}' instance creation on iterator {iteratorKind} invocation"));
        }

        break;
      }
    }
  }
}