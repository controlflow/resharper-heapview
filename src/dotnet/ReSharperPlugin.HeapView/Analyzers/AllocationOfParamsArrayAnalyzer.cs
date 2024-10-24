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
      if (!InvocationHasParamsArgumentsInExpandedFormOrNoCorrespondingArguments(argumentsOwner, lastParameter, out var paramsArgument))
        return; // explicit array passed

      if (argumentsOwner.IsInTheContextWhereAllocationsAreNotImportant())
        return;

      var paramsParameterType = substitution[lastParameter.Type];
      ReportParamsAllocation(argumentsOwner, paramsArgument, lastParameter, paramsParameterType, data, consumer);
    }
    else if (lastParameter.IsParameterCollection
             && lastParameter.IsParameterArrayLike(data.GetLanguageLevel()))
    {
      if (!InvocationHasParamsArgumentsInExpandedFormOrNoCorrespondingArguments(argumentsOwner, lastParameter, out var paramsArgument))
        return; // explicit collection passed

      if (argumentsOwner.IsInTheContextWhereAllocationsAreNotImportant())
        return;

      // todo: classify params collection creation
      // todo: can be span/list<T>/collectionbuilder/ilist/etc
      // todo: reuse collection expressions code somehow...
      // todo: nested allocations from .ctor invocation?
    }
  }

  private static void ReportParamsAllocation(
    ICSharpArgumentsOwner argumentsOwner, ICSharpArgumentInfo? paramsArgument,
    IParameter paramsParameter, IType paramsParameterType,
    ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    ITreeNode? nodeToHighlight;
    if (paramsArgument == null)
    {
      if (IsArrayEmptyMemberOptimizationAvailable(data, paramsParameterType))
        return;

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
      nodeToHighlight = paramsArgument switch
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

    if (nodeToHighlight.IsInTheContextWhereAllocationsAreNotImportant())
      return;

    var arrayType = paramsParameterType.GetPresentableName(nodeToHighlight.Language, CommonUtils.DefaultTypePresentationStyle);
    var parameterName = DeclaredElementPresenter.Format(CSharpLanguage.Instance!, DeclaredElementPresenter.NAME_PRESENTER, paramsParameter);

    consumer.AddHighlighting(new ObjectAllocationHighlighting(
      nodeToHighlight, new RichText(
        $"new '{arrayType}' array instance creation for params parameter '{parameterName}'")));
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

  private static readonly Key<object> ArrayEmptyIsAvailableKey = new(nameof(ArrayEmptyIsAvailableKey));

  [Pure]
  private static bool IsArrayEmptyMemberOptimizationAvailable(ElementProblemAnalyzerData data, IType paramsParameterType)
  {
    var arrayType = paramsParameterType as IArrayType;
    if (arrayType == null) return true;

    if (arrayType.ElementType.IsPointerOrFunctionPointer())
      return false; // can't use unmanaged types as type arguments for Array.Empty<T>()

    return (bool)data.GetOrCreateDataUnderLock(ArrayEmptyIsAvailableKey, data, static data =>
    {
      // note: we do not check for C# 6 compiler, since it is minimal supported compiler now

      var predefinedType = data.GetPredefinedType();
      if (predefinedType.Array.GetTypeElement() is IClass systemArrayTypeElement)
      {
        foreach (var typeMember in systemArrayTypeElement.EnumerateMembers(nameof(Array.Empty), caseSensitive: true))
        {
          if (typeMember is IMethod { Parameters.Count: 0, TypeParameters.Count: 1 } method
              && method.GetAccessRights() == AccessRights.PUBLIC)
          {
            return BooleanBoxes.True;
          }
        }
      }

      return BooleanBoxes.False;
    });
  }
}