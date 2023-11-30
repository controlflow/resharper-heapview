using System;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

// todo: spread is allocating the enumerator

[ElementProblemAnalyzer(
  ElementTypes: [ typeof(ICollectionExpression) ],
  HighlightingTypes = [
    typeof(ObjectAllocationHighlighting),
    typeof(ObjectAllocationPossibleHighlighting)
  ])]
public class AllocationOfCollectionExpressionAnalyzer : HeapAllocationAnalyzerBase<ICollectionExpression>
{
  protected override void Run(
    ICollectionExpression collectionExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (collectionExpression.IsInTheContextWhereAllocationsAreNotImportant())
      return;

    var typeInfo = collectionExpression.GetTargetTypeInfo();

    switch (typeInfo.Kind)
    {
      case CollectionExpressionKind.None:
      {
        break; // error code
      }

      case CollectionExpressionKind.Array:
      {
        if (collectionExpression.CollectionElementsEnumerable.IsEmpty())
        {
          return; // Array.Empty<T>() is used
        }

        if (HasSpreadsOfUnknownLength(collectionExpression))
        {
          // temporary list is required, array can be conditionally allocated
          var message = CanBeEvaluatedToBeEmpty(collectionExpression)
            ? $"new temporary list and possible (if not empty) '{PresentTypeName(typeInfo.TargetType)}' array instance creation"
            : $"new temporary list and '{PresentTypeName(typeInfo.TargetType)}' array instance creation";

          consumer.AddHighlighting(
            new ObjectAllocationHighlighting(collectionExpression.LBracket, message),
            GetCollectionExpressionRangeToHighlight());
        }
        else
        {
          consumer.AddHighlighting(
            new ObjectAllocationHighlighting(
              collectionExpression.LBracket,
              $"new '{PresentTypeName(typeInfo.TargetType)}' array instance creation"),
            GetCollectionExpressionRangeToHighlight());
        }

        return;
      }

      case CollectionExpressionKind.Span:
      case CollectionExpressionKind.ReadOnlySpan:
      {
        if (collectionExpression.CollectionElementsEnumerable.IsEmpty())
        {
          return; // Span<T>.Empty
        }

        if (typeInfo.Kind == CollectionExpressionKind.ReadOnlySpan
            && collectionExpression.CanBeLoweredToRuntimeHelpersCreateSpan())
        {
          return; // inline data or RuntimeHelpers.CreateSpan
        }

        // todo: wait for final APIs from RC
        // todo: no allocation if inline arrays can be used, heap array otherwise

        return;
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
        if (typeInfo.TargetType is IDeclaredType createdType)
        {
          switch (createdType.Classify)
          {
            case TypeClassification.REFERENCE_TYPE:
            {
              consumer.AddHighlighting(
                new ObjectAllocationHighlighting(
                  collectionExpression.LBracket,
                  $"new '{PresentTypeName(typeInfo.TargetType)}' instance creation"),
                GetCollectionExpressionRangeToHighlight());
              break;
            }

            case TypeClassification.UNKNOWN when createdType.IsTypeParameterType():
            {
              consumer.AddHighlighting(
                new ObjectAllocationPossibleHighlighting(
                  collectionExpression.LBracket,
                  $"new instance creation if '{PresentTypeName(typeInfo.TargetType)}' type parameter " +
                  $"will be substituted with the reference type"),
                GetCollectionExpressionRangeToHighlight());
              break;
            }
          }
        }

        break;
      }

      case CollectionExpressionKind.ArrayInterface:
      {
        var targetType = typeInfo.TargetType;
        if (targetType.IsGenericIList() || targetType.IsGenericICollection())
        {
          var genericListTypeElement = collectionExpression.GetPredefinedType().GenericList.GetTypeElement();
          if (genericListTypeElement == null) return;

          var genericListOfElementType = TypeFactory.CreateType(genericListTypeElement, [typeInfo.ElementType]);

          consumer.AddHighlighting(
            new ObjectAllocationHighlighting(
              collectionExpression.LBracket,
              $"new '{PresentTypeName(genericListOfElementType)}' instance creation"),
            GetCollectionExpressionRangeToHighlight());
        }
        else
        {
          if (collectionExpression.CollectionElementsEnumerable.IsEmpty())
          {
            return; // Array<T>.Empty()
          }

          var storageKind = HasSpreadsOfUnknownLength(collectionExpression) ? "temporary list" : "array";

          consumer.AddHighlighting(
            new ObjectAllocationHighlighting(
              collectionExpression.LBracket,
              $"new {storageKind} and '{PresentTypeName(typeInfo.TargetType)}' implementation instance creation"),
            GetCollectionExpressionRangeToHighlight());
        }

        break;
      }

      default:
        throw new ArgumentOutOfRangeException();
    }

    return;

    [Pure]
    string PresentTypeName(IType type)
    {
      return type.GetPresentableName(
        collectionExpression.Language, CommonUtils.DefaultTypePresentationStyle).Text;
    }

    [Pure]
    DocumentRange GetCollectionExpressionRangeToHighlight()
    {
      var lBracket = collectionExpression.LBracket;
      if (lBracket == null) return DocumentRange.InvalidRange;

      var nextToken = lBracket.GetNextToken();
      if (nextToken != null && nextToken.NodeType != CSharpTokenType.NEW_LINE)
      {
        // []
        // ^^
        if (nextToken == collectionExpression.RBracket)
        {
          return collectionExpression.GetDocumentRange();
        }

        const int hintMaxLength = 2;

        // [123]
        // ^^^
        if (nextToken.GetTextLength() > hintMaxLength)
        {
          return lBracket.GetDocumentRange().ExtendRight(hintMaxLength);
        }

        // [12]
        // ^^^^
        if (nextToken.GetNextToken() == collectionExpression.RBracket)
        {
          return collectionExpression.GetDocumentRange();
        }
      }

      // [1, 2, 3]
      // ^
      return lBracket.GetDocumentRange();
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