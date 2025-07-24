using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Resolve;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.UI.RichText;
using JetBrains.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: [ typeof(ICSharpArgumentsOwner) ],
  HighlightingTypes = [
    typeof(ObjectAllocationHighlighting),
    typeof(ObjectAllocationPossibleHighlighting)
  ])]
public class AllocationOfParamsArrayAnalyzer : HeapAllocationAnalyzerBase<ICSharpArgumentsOwner>
{
  protected override void Run(
    ICSharpArgumentsOwner argumentsOwner, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (argumentsOwner is IAttribute) return;

    var invocationReference = TryGetInvocationReference(argumentsOwner);
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
      AnalyzeParameterAllayAllocation(
        argumentsOwner,
        new DeclaredElementInstanceSlim<IParameter>(lastParameter, substitution),
        data, consumer);
    }
    else if (lastParameter.IsParameterCollection
             && lastParameter.IsParameterArrayLike(data.GetLanguageLevel()))
    {
      AnalyzeParamsCollectionAllocation(
        argumentsOwner,
        new DeclaredElementInstanceSlim<IParameter>(lastParameter, substitution),
        data, consumer);
    }
  }

  private IReference? TryGetInvocationReference(ICSharpArgumentsOwner argumentsOwner)
  {
    var invocationReference = argumentsOwner.Reference;
    if (invocationReference != null) return invocationReference;

    // `class C : BaseTypeWithParamsCtor;`
    if (argumentsOwner is IExtendedType { ArgumentList: null } extendedType
        && extendedType.TypeReference?.Resolve() is (IClass baseClass, _))
    {
      return new CSharpImplicitBaseConstructorInvocationReference(extendedType, baseClass);
    }

    return null;
  }

  private static void AnalyzeParameterAllayAllocation(
    ICSharpArgumentsOwner argumentsOwner, DeclaredElementInstanceSlim<IParameter> paramsParameter,
    ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (argumentsOwner.IsInTheContextWhereAllocationsAreNotImportant())
      return;

    if (!InvocationHasParamsArgumentsInExpandedFormOrNoCorrespondingArguments(
          argumentsOwner, paramsParameter.Element, out var firstExpandedArgument))
      return;

    var paramsArrayType = paramsParameter.Substitution[paramsParameter.Element.Type] as IArrayType;
    if (paramsArrayType == null)
      return;

    if (firstExpandedArgument is null
        && data.IsSystemArrayEmptyMemberAvailable(paramsArrayType.ElementType))
      return;

    ReportParamsAllocation(
      argumentsOwner, firstExpandedArgument, paramsParameter.Element,
      state: paramsArrayType,
      allocationReasonMessageFactory: static (context, paramsParameterType) =>
      {
        var parameterTypeText = paramsParameterType.GetPresentableName(context.Language, CommonUtils.DefaultTypePresentationStyle);
        return new RichText($"new '{parameterTypeText}' array instance creation");
      },
      consumer);
  }

  private static void AnalyzeParamsCollectionAllocation(
    ICSharpArgumentsOwner argumentsOwner, DeclaredElementInstanceSlim<IParameter> paramsParameter,
    ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (!InvocationHasParamsArgumentsInExpandedFormOrNoCorrespondingArguments(
          argumentsOwner, paramsParameter.Element, out var firstExpandedArgument))
      return;

    var paramsParameterType = paramsParameter.Substitution[paramsParameter.Element.Type];
    var targetTypeInfo = CSharpCollectionTypeUtil.GetCollectionExpressionTargetTypeInfo(paramsParameterType, argumentsOwner);

    switch (targetTypeInfo.Kind)
    {
      case CollectionExpressionKind.None:
      case CollectionExpressionKind.Array: // should never happen
      {
        return;
      }

      case CollectionExpressionKind.Span:
      case CollectionExpressionKind.ReadOnlySpan:
      {
        if (firstExpandedArgument is null)
        {
          return;
        }

        if (targetTypeInfo.Kind == CollectionExpressionKind.ReadOnlySpan
            && argumentsOwner.CanBeLoweredToRuntimeHelpersCreateSpan(paramsParameter.Element, targetTypeInfo.ElementType))
        {
          return;
        }

        if (argumentsOwner.GetPsiModule().RuntimeSupportsInlineArrayTypes())
        {
          return;
        }

        goto ReportArrayAllocation;
      }

      case CollectionExpressionKind.CollectionBuilder when targetTypeInfo.TargetType.IsGenericImmutableArray(out _):
      {
        // the array is constructed and moved into the ImmutableArray<T> struct

        if (firstExpandedArgument is null)
        {
          return; // ImmutableCollectionsMarshal.AsImmutableArray<T>(Array.Empty<T>())
        }

        goto ReportArrayAllocation;
      }

      case CollectionExpressionKind.CollectionBuilder:
      {
        // we need to always construct the ReadOnlySpan<T> to call the factory method

        if (firstExpandedArgument != null
            && !argumentsOwner.CanBeLoweredToRuntimeHelpersCreateSpan(paramsParameter.Element, targetTypeInfo.ElementType)
            && !argumentsOwner.GetPsiModule().RuntimeSupportsInlineArrayTypes())
        {
          // we need heap array

          ReportParamsAllocation(
            argumentsOwner, firstExpandedArgument, paramsParameter.Element,
            state: targetTypeInfo,
            allocationReasonMessageFactory: static (context, typeInfo) =>
            {
              var arrayType = TypeFactory.CreateArrayType(typeInfo.ElementType!, rank: 1);
              var arrayTypeText = arrayType.GetPresentableName(
                context.Language, CommonUtils.DefaultTypePresentationStyle);
              var collectionTypeText = typeInfo.TargetType.GetPresentableName(
                context.Language, CommonUtils.DefaultTypePresentationStyle);

              return new RichText($"new '{arrayTypeText}' array instance creation and new '{collectionTypeText}' collection creation");
            },
            consumer);
        }
        else // span is not on the heap (empty or not)
        {
          ReportParamsAllocation(
            argumentsOwner, firstExpandedArgument, paramsParameter.Element,
            state: targetTypeInfo.TargetType,
            allocationReasonMessageFactory: static (context, targetType) =>
            {
              var collectionTypeText = targetType.GetPresentableName(context.Language, CommonUtils.DefaultTypePresentationStyle);
              return new RichText($"new '{collectionTypeText}' collection creation");
            },
            consumer, possibleAllocation: true);
        }

        break;
      }

      case CollectionExpressionKind.ImplementsIEnumerable:
      {
        if (targetTypeInfo.TargetType is IDeclaredType createdType)
        {
          switch (createdType.Classify)
          {
            case TypeClassification.REFERENCE_TYPE:
            {
              ReportParamsAllocation(
                argumentsOwner, firstExpandedArgument, paramsParameter.Element,
                state: targetTypeInfo.TargetType,
                allocationReasonMessageFactory: static (context, targetType) =>
                {
                  var collectionTypeText = targetType.GetPresentableName(context.Language, CommonUtils.DefaultTypePresentationStyle);
                  return new RichText($"new '{collectionTypeText}' instance creation");
                },
                consumer);
              break;
            }

            case TypeClassification.UNKNOWN when createdType.IsTypeParameterType():
            {
              ReportParamsAllocation(
                argumentsOwner, firstExpandedArgument, paramsParameter.Element,
                state: targetTypeInfo.TargetType,
                allocationReasonMessageFactory: static (context, targetType) =>
                {
                  var collectionTypeText = targetType.GetPresentableName(context.Language, CommonUtils.DefaultTypePresentationStyle);
                  return new RichText(
                    $"new instance creation if '{collectionTypeText}' " +
                    $"type parameter will be substituted with the reference type,");
                },
                consumer, possibleAllocation: true);
              break;
            }
          }
        }

        break;
      }

      case CollectionExpressionKind.ArrayInterface:
      {
        var targetType = targetTypeInfo.TargetType;
        if (targetType.IsGenericIList() || targetType.IsGenericICollection()) // mutable interface - List<T>
        {
          var genericListTypeElement = argumentsOwner.GetPredefinedType().GenericList.GetTypeElement();
          if (genericListTypeElement == null) return;

          var genericListOfElementType = TypeFactory.CreateType(genericListTypeElement, [targetTypeInfo.ElementType]);

          ReportParamsAllocation(
            argumentsOwner, firstExpandedArgument, paramsParameter.Element,
            state: genericListOfElementType,
            allocationReasonMessageFactory: static (context, collectionType) =>
            {
              var collectionTypeText = collectionType.GetPresentableName(context.Language, CommonUtils.DefaultTypePresentationStyle);
              return new RichText($"new '{collectionTypeText}' instance creation if");
            },
            consumer);
        }
        else
        {
          if (firstExpandedArgument is null
              && data.IsSystemArrayEmptyMemberAvailable(targetTypeInfo.ElementType!))
          {
            return; // Array<T>.Empty()
          }

          var targetInterfaceTypeText = targetTypeInfo.TargetType.GetPresentableName(
            argumentsOwner.Language, CommonUtils.DefaultTypePresentationStyle);

          RichText allocationMessage;
          if (argumentsOwner.EnumerateAllExpandedArguments(paramsParameter.Element).SingleItem() != null)
          {
            allocationMessage = new RichText(
              $"new '{targetInterfaceTypeText}' implementation instance creation");
          }
          else
          {
            allocationMessage = new RichText(
              $"new array and '{targetInterfaceTypeText}' implementation instance creation");
          }

          ReportParamsAllocation(
            argumentsOwner, firstExpandedArgument, paramsParameter.Element,
            state: allocationMessage,
            allocationReasonMessageFactory: static (_, message) => message,
            consumer);
        }

        break;
      }

      default:
        throw new ArgumentOutOfRangeException();
    }

    return;

    ReportArrayAllocation:
    ReportParamsAllocation(
      argumentsOwner, firstExpandedArgument, paramsParameter.Element,
      state: targetTypeInfo.ElementType!,
      allocationReasonMessageFactory: static (context, elementType) =>
      {
        var arrayType = TypeFactory.CreateArrayType(elementType, rank: 1);
        var parameterTypeText = arrayType.GetPresentableName(context.Language, CommonUtils.DefaultTypePresentationStyle);
        return new RichText($"new '{parameterTypeText}' array instance creation");
      },
      consumer);
  }

  private static void ReportParamsAllocation<TState>(
    ICSharpArgumentsOwner argumentsOwner,
    ICSharpArgumentInfo? firstExpandedArgument,
    IParameter paramsParameter,
    TState state,
    [RequireStaticDelegate] Func<ITreeNode, TState, RichText> allocationReasonMessageFactory,
    IHighlightingConsumer consumer,
    bool possibleAllocation = false)
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
      return; // normally should not happen

    var allocationReason = allocationReasonMessageFactory(nodeToHighlight, state);

    var parameterName = DeclaredElementPresenter.Format(
      CSharpLanguage.Instance!, DeclaredElementPresenter.NAME_PRESENTER, paramsParameter);
    var paramsKeyword = "params".Colorize(DeclaredElementPresentationPartKind.Keyword);

    var description = new RichText($"{allocationReason} for {paramsKeyword} parameter '{parameterName}'");
    consumer.AddHighlighting(
      possibleAllocation
      ? new ObjectAllocationPossibleHighlighting(nodeToHighlight, description)
      : new ObjectAllocationHighlighting(nodeToHighlight, description));
  }

  [Pure]
  private static bool InvocationHasParamsArgumentsInExpandedFormOrNoCorrespondingArguments(
    ICSharpArgumentsOwner argumentsOwner, IParameter paramsParameter, out ICSharpArgument? firstExpandedArgument)
  {
    foreach (var argument in argumentsOwner.ArgumentsEnumerable)
    {
      var parameter = argument.MatchingParameter;
      if (parameter == null) continue;

      if (!Equals(parameter.Element, paramsParameter)) continue;

      switch (parameter.Expanded)
      {
        case ArgumentsUtil.ExpandedKind.None: // explicit array passing
          firstExpandedArgument = null;
          return false;

        case ArgumentsUtil.ExpandedKind.Expanded:
          firstExpandedArgument = argument;
          return true;
      }
    }

    firstExpandedArgument = null;
    return true;
  }
}