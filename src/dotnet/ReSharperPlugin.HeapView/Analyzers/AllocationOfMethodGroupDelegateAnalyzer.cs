using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.DeclaredElements;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(IReferenceExpression) },
  HighlightingTypes = new[] { typeof(DelegateAllocationHighlighting) })]
public class AllocationOfMethodGroupDelegateAnalyzer : HeapAllocationAnalyzerBase<IReferenceExpression>
{
  protected override void Run(
    IReferenceExpression referenceExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(referenceExpression.GetContainingParenthesizedExpression());
    if (invocationExpression != null) return;

    if (referenceExpression.IsNameofOperatorTopArgument()) return;

    var (declaredElement, _, resolveErrorType) = referenceExpression.Reference.Resolve();
    if (!resolveErrorType.IsAcceptable) return;

    var parametersOwner = declaredElement as IParametersOwner;
    if (parametersOwner is not (IMethod or ILocalFunction)) return;

    var delegateType = referenceExpression.TryFindTargetDelegateType();
    if (delegateType == null) return;

    if (referenceExpression.IsInTheContextWhereAllocationsAreNotImportant())
      return;

    var isExplicitDelegateCreation = IsSurroundedByExplicitDelegateCreationExpression(referenceExpression, out var explicitCreationNode);

    if (parametersOwner is IMethod { IsStatic: true } or ILocalFunction { IsStatic: true }
        && data.GetLanguageLevel() >= CSharpLanguageLevel.CSharp110
        // check for explicit delegate allocation: `new Action(StaticMethod)`
        // see https://github.com/dotnet/roslyn/issues/62832#issuecomment-1201144122
        && !isExplicitDelegateCreation)
    {
      // C# 11 caches delegate instances from static methods and static local functions
      return;
    }

    var delegateTypeText = delegateType.GetPresentableName(referenceExpression.Language, CommonUtils.DefaultTypePresentationStyle);
    consumer.AddHighlighting(new DelegateAllocationHighlighting(
      explicitCreationNode ?? referenceExpression.NameIdentifier,
      $"new '{delegateTypeText}' instance creation"));
  }

  [Pure]
  private static bool IsSurroundedByExplicitDelegateCreationExpression(IReferenceExpression methodGroupReference, out ITreeNode? nodeToHighlight)
  {
    var containingExpression = methodGroupReference.GetContainingParenthesizedExpression();
    var argument = CSharpArgumentNavigator.GetByValue(containingExpression);

    var objectCreationExpression = ObjectCreationExpressionNavigator.GetByArgument(argument);
    if (objectCreationExpression != null
        && objectCreationExpression.ArgumentsEnumerable.SingleItem == argument
        && objectCreationExpression.Type() is IDeclaredType (IDelegate))
    {
      nodeToHighlight = objectCreationExpression.NewKeyword;
      return true;
    }

    nodeToHighlight = null;
    return false;
  }
}