#nullable enable
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
using JetBrains.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(ICSharpArgumentsOwner), typeof(ICollectionElementInitializer) },
  HighlightingTypes = new[] { typeof(ObjectAllocationHighlighting) })]
public class AllocationOfParamsArrayAnalyzer : HeapAllocationAnalyzerBase<ITreeNode>
{
  protected override void Run(
    ITreeNode invocationNode, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    switch (invocationNode)
    {
      case IAttribute:
        break;

      // case ICollectionElementInitializer collectionElementInitializer:
      //   CheckInvocationInfo(collectionElementInitializer, invocationAnchor: null, data, consumer);
      //   break;

      case ICSharpArgumentsOwner argumentsOwner:
        CheckInvocationInfo(argumentsOwner, invocationAnchor: null, data, consumer);
        break;
    }
  }

  private void CheckInvocationInfo(
    ICSharpInvocationInfo invocationInfo, ITreeNode? invocationAnchor,
    ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var invocationReference = invocationInfo.Reference;
    if (invocationReference == null) return;

    var (declaredElement, substitution, resolveErrorType) = invocationReference.Resolve();
    if (resolveErrorType != ResolveErrorType.OK) return;

    var parametersOwner = declaredElement as IParametersOwner;
    if (parametersOwner == null) return;

    var parameters = parametersOwner.Parameters;
    if (parameters.Count == 0) return;

    var lastParameter = parameters[^1];
    if (!lastParameter.IsParameterArray) return;

    if (!InvocationHasParamsArgumentsInExpandedFormOrNoCorresponindArguments(invocationInfo, lastParameter, out var paramsArgument))
      return; // explicit array passed

    var paramsParameterType = substitution[lastParameter.Type];

    ITreeNode? anchor;
    if (paramsArgument == null)
    {
      if (IsArrayEmptyMemberOptimizationAvailable(data, paramsParameterType))
        return;

      anchor = invocationInfo switch
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
        ICSharpExpression expr => expr,
        _ => null
      };
    }
    else
    {
      // highlight arg

      anchor = paramsArgument switch
      {
        //ICSharpArgument { Mode: { } mode } => mode,
        ICSharpArgument { Value: { } value } => value,
        _ => null
      };
    }

    if (anchor == null) return;

    if (anchor.IsInTheContextWhereAllocationsAreNotImportant())
    {
      return;
    }

    var arrayType = paramsParameterType.GetPresentableName(anchor.Language, CommonUtils.DefaultTypePresentationStyle);
    var description = $"new '{arrayType}' array instance creation for params parameter '{lastParameter.ShortName}'";
    consumer.AddHighlighting(new ObjectAllocationHighlighting(anchor, description));
  }

  [Pure]
  private static bool InvocationHasParamsArgumentsInExpandedFormOrNoCorresponindArguments(
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