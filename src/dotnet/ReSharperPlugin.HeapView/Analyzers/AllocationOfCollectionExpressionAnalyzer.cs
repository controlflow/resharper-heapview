using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(ICollectionExpression) },
  HighlightingTypes = new[]
  {
    typeof(ObjectAllocationHighlighting),
    typeof(ObjectAllocationPossibleHighlighting) // todo: ???
  })]
public class AllocationOfCollectionExpressionAnalyzer : HeapAllocationAnalyzerBase<ICollectionExpression>
{
  protected override void Run(
    ICollectionExpression collectionExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var typeInfo = collectionExpression.GetTargetTypeInfo(collectionExpression.Type());

    switch (typeInfo.Kind)
    {
      case CollectionExpressionKind.None:
      {
        break; // error code
      }

      case CollectionExpressionKind.Array:
      {
        if (collectionExpression.CollectionElementsEnumerable.IsEmpty())
          return; // Array.Empty<T>() is used

        if (HasSpreadsOfUnknownLength(collectionExpression))
        {
          // temporary list is required, array can be conditionally allocated
          var message = CanBeEvaluatedToBeEmpty(collectionExpression)
            ? $"new temporary list and possible (if not empty) '{TargetTypeName()}' array instance creation"
            : $"new temporary list and '{TargetTypeName()}' array instance creation";

          consumer.AddHighlighting(new ObjectAllocationHighlighting(collectionExpression.LBracket, message));
        }
        else
        {
          consumer.AddHighlighting(new ObjectAllocationHighlighting(
            collectionExpression.LBracket,
            $"new '{TargetTypeName()}' array instance creation"));
        }

        return;
      }

      case CollectionExpressionKind.Span:
      case CollectionExpressionKind.ReadOnlySpan:
      {
        // when stackalloc?
        // todo: blittable data

        break;
      }

      case CollectionExpressionKind.ImmutableArray:
      case CollectionExpressionKind.CollectionBuilder:
      {
        // assume works like ctor?
        // where temporary span is stored?

        break;
      }

      case CollectionExpressionKind.List:
      case CollectionExpressionKind.ImplementsGenericIEnumerable:
      case CollectionExpressionKind.ImplementsIEnumerable:
      {
        // check ctor, reference type or not?

        break;
      }

      case CollectionExpressionKind.ArrayInterface:
      {
        // temporary list

        break;
      }

      default:
        throw new ArgumentOutOfRangeException();
    }

    string TargetTypeName()
    {
      return typeInfo.TargetType.GetPresentableName(
        collectionExpression.Language, CommonUtils.DefaultTypePresentationStyle).Text;
    }
  }

  [Pure]
  private static ElementsClassification ClassifyElements(ICollectionExpression collectionExpression)
  {
    var hasAnyElements = false;
    var hasSpreads = false;
    var hasExpressions = false;

    foreach (var element in collectionExpression.CollectionElementsEnumerable)
    {
      hasAnyElements = true;

      if (element is ISpreadElement)
      {
        hasSpreads = true;
      }
      else
      {
        hasExpressions = true;
      }
    }

    if (!hasAnyElements)
      return ElementsClassification.Empty;

    if (hasSpreads && !hasExpressions)
      return ElementsClassification.OnlySpreads;

    if (hasSpreads)
      return ElementsClassification.NonEmptyHasSpreads;

    return ElementsClassification.NonEmpty;
  }

  private enum ElementsClassification
  {
    // []
    Empty = 0,
    // [..xs, ..ys] - may be evaluated to be empty
    OnlySpreads,
    // [..xs], [x, ..xs]
    NonEmptyHasSpreads,
    // [x, y]
    NonEmpty
  }

  [Pure]
  private static bool CanBeEvaluatedToBeEmpty(ICollectionExpression collectionExpression)
  {
    foreach (var element in collectionExpression.CollectionElementsEnumerable)
    {
      switch (element)
      {
        case IExpressionElement:
          return false; // non-empty element
        case ISpreadElement:
          continue; // can be empty sequence
      }
    }

    return true;
  }

  [Pure]
  private static bool HasSpreadsOfUnknownLength(ICollectionExpression collectionExpression)
  {
    foreach (var element in collectionExpression.CollectionElementsEnumerable)
    {
      if (element is ISpreadElement spreadElement)
      {
        var lengthCountResolveResult = spreadElement.CountOrLengthReference.Resolve();
        if (lengthCountResolveResult.ResolveErrorType != ResolveErrorType.OK)
          return true;
      }
    }

    return false;
  }
}