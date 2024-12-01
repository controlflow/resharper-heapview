using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.UI.RichText;
using JetBrains.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: [ typeof(ICSharpArgumentsOwner) ],
  HighlightingTypes = [ typeof(ObjectAllocationHighlighting) ])]
public class AllocationOfParamsArrayAnalyzer : HeapAllocationAnalyzerBase<ICSharpArgumentsOwner>
{
  protected override void Run(
    ICSharpArgumentsOwner argumentsOwner, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (argumentsOwner is IAttribute) return;

    var invocationReference = argumentsOwner.Reference;
    if (invocationReference == null) return;

    var (declaredElement, substitution, resolveErrorType) = invocationReference.Resolve();
    if (resolveErrorType != ResolveErrorType.OK) return;

    var parametersOwner = declaredElement as IParametersOwner;
    if (parametersOwner == null) return;

    var parameters = parametersOwner.Parameters;
    if (parameters.Count == 0) return;

    var lastParameter = parameters[^1];
    if (lastParameter.IsParameterArray)
    {
      if (argumentsOwner.IsInTheContextWhereAllocationsAreNotImportant())
        return;

      if (!InvocationHasParamsArgumentsInExpandedFormOrNoCorrespondingArguments(argumentsOwner, lastParameter, out var firstExpandedArgument))
        return; // explicit array passed

      var paramsParameterType = substitution[lastParameter.Type];
      if (firstExpandedArgument is null && data.IsSystemArrayEmptyMemberAvailable(paramsParameterType))
        return; // empty arguments sometimes do not allocate

      ReportParamsAllocation(
        argumentsOwner, firstExpandedArgument,
        lastParameter, paramsParameterType,
        allocationReasonMessageFactory: parameterTypeText => new RichText($"new '{parameterTypeText}' array instance creation"),
        consumer);
    }
    else if (lastParameter.IsParameterCollection
             && lastParameter.IsParameterArrayLike(data.GetLanguageLevel()))
    {
      if (!InvocationHasParamsArgumentsInExpandedFormOrNoCorrespondingArguments(argumentsOwner, lastParameter, out var paramsArgument))
        return; // explicit collection passed

      var paramsParameterType = substitution[lastParameter.Type];
      var targetTypeInfo = CSharpCollectionTypeUtil.GetCollectionExpressionTargetTypeInfo(paramsParameterType, argumentsOwner);

      switch (targetTypeInfo.Kind)
      {
        case CollectionExpressionKind.None:
        case CollectionExpressionKind.Array: // should never happen
        {
          break;
        }

        case CollectionExpressionKind.Span:
        case CollectionExpressionKind.ReadOnlySpan:
        {
          if (paramsArgument is null)
          {
            return; // Span<T>.Empty
          }

          // if (targetTypeInfo.Kind == CollectionExpressionKind.ReadOnlySpan
          //     && collectionExpression.CanBeLoweredToRuntimeHelpersCreateSpan())
          // {
          //   return; // inline data or RuntimeHelpers.CreateSpan
          // }
          //
          // if (collectionExpression.CanBeLoweredToInlineArray())
          // {
          //   return; // stack allocated inline array
          // }
          //
          // var arrayType = TypeFactory.CreateArrayType(targetTypeInfo.ElementType.NotNull(), rank: 1);
          // ReportArrayAndPossibleTemporaryListAllocation(arrayType);
          // return;

          break;
        }

        case CollectionExpressionKind.CollectionBuilder:
          break;
        case CollectionExpressionKind.ImplementsIEnumerable:
          break;
        case CollectionExpressionKind.ArrayInterface:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      // todo: classify params collection creation
      // todo: can be span/list<T>/collectionbuilder/ilist/etc
      // todo: reuse collection expressions code somehow...
      // todo: nested allocations from .ctor invocation?
    }
  }

  private static void ReportParamsAllocation(
    ICSharpArgumentsOwner argumentsOwner,
    ICSharpArgumentInfo? firstExpandedArgument,
    IParameter paramsParameter,
    IType paramsParameterType,
    Func<RichText, RichText> allocationReasonMessageFactory,
    IHighlightingConsumer consumer)
  {
    ITreeNode? nodeToHighlight;
    if (firstExpandedArgument == null)
    {
      nodeToHighlight = argumentsOwner switch
      {
        IInvocationExpression invocationExpression
          when invocationExpression.InvokedExpression.GetOperandThroughParenthesis() is IReferenceExpression referenceExpression
          => referenceExpression.NameIdentifier,
        IObjectCreationExpression { TypeName: { } createdTypeName } => createdTypeName.NameIdentifier,
        IObjectCreationExpression newExpression => newExpression.RPar ?? newExpression.NewKeyword,
        IConstructorInitializer constructorInitializer => constructorInitializer.Instance,
        IExtendedType { TypeUsage: IUserTypeUsage { ScalarTypeName: { } extendedType } } => extendedType.NameIdentifier,
        IExtendedType extendedType => extendedType.RPar,
        ICollectionElementInitializer { RBrace: { } rBrace } => rBrace,
        ICollectionElementInitializer { ArgumentsEnumerable.SingleItem: { } singleArgument } => singleArgument.Value,
        IElementAccessExpression elementAccessExpression => elementAccessExpression.RBracket,
        IIndexerInitializer indexerInitializer => indexerInitializer.RBracket,
        ICSharpExpression expression => expression,
        _ => null
      };
    }
    else
    {
      nodeToHighlight = firstExpandedArgument switch
      {
        ICSharpArgument { Value: { } value } => GetFirstToken(value),
        _ => null
      };

      ITreeNode GetFirstToken(ITreeNode expr)
      {
        var tokenNode = expr.FindFirstTokenIn();
        while (tokenNode != null && tokenNode.IsFiltered())
        {
          tokenNode = tokenNode.GetNextToken();
        }

        return tokenNode ?? expr;
      }
    }

    if (nodeToHighlight == null)
      return;

    var parameterTypeText = paramsParameterType.GetPresentableName(nodeToHighlight.Language, CommonUtils.DefaultTypePresentationStyle);
    var allocationReason = allocationReasonMessageFactory(parameterTypeText);

    var parameterName = DeclaredElementPresenter.Format(CSharpLanguage.Instance!, DeclaredElementPresenter.NAME_PRESENTER, paramsParameter);
    var paramsKeyword = "params".Colorize(DeclaredElementPresentationPartKind.Keyword);

    consumer.AddHighlighting(new ObjectAllocationHighlighting(
      nodeToHighlight, new RichText($"{allocationReason} for {paramsKeyword} parameter '{parameterName}'")));
  }

  [Pure]
  private static bool InvocationHasParamsArgumentsInExpandedFormOrNoCorrespondingArguments(
    ICSharpInvocationInfo invocationInfo, IParameter paramsParameter, out ICSharpArgumentInfo? firstExpandedArgument)
  {
    foreach (var argumentInfo in invocationInfo.Arguments)
    {
      var parameter = argumentInfo.MatchingParameter;
      if (parameter == null) continue;

      if (!Equals(parameter.Element, paramsParameter)) continue;

      switch (parameter.Expanded)
      {
        case ArgumentsUtil.ExpandedKind.None: // explicit array passing
          firstExpandedArgument = null;
          return false;

        case ArgumentsUtil.ExpandedKind.Expanded:
          firstExpandedArgument = argumentInfo;
          return true;
      }
    }

    firstExpandedArgument = null;
    return true;
  }
}