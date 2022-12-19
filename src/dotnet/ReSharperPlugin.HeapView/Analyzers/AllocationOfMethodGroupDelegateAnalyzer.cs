#nullable enable
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.DeclaredElements;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
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
    if (parametersOwner == null) return;

    var delegateType = referenceExpression.TryFindTargetDelegateType();
    if (delegateType == null) return;

    if (referenceExpression.IsInTheContextWhereAllocationsAreNotImportant())
      return;

    if (parametersOwner is IMethod { IsStatic: true } or ILocalFunction { IsStatic: true }
        && data.GetLatestSupportedLanguageLevel() >= CSharpLanguageLevel.CSharp110)
    {
      // C# 11 caches delegate instances from static methods and static local functions
      return;
    }

    var delegateTypeText = delegateType.GetPresentableName(referenceExpression.Language, CommonUtils.DefaultTypePresentationStyle);
    consumer.AddHighlighting(new DelegateAllocationHighlighting(
      referenceExpression.NameIdentifier, $"new '{delegateTypeText}' instance creation"));
  }
}