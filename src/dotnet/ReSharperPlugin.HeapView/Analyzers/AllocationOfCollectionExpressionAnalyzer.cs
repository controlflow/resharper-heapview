using System;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.UI.RichText;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

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

    var targetTypeInfo = collectionExpression.GetTargetTypeInfo();

    switch (targetTypeInfo.Kind)
    {
      case CollectionExpressionKind.None:
      {
        break; // error code
      }

      case CollectionExpressionKind.Array:
      {
        if (collectionExpression.CollectionElementsEnumerable.IsEmpty()
            && data.IsSystemArrayEmptyMemberAvailable(targetTypeInfo.TargetType))
        {
          return; // Array.Empty<T>() is used
        }

        ReportArrayAndPossibleTemporaryListAllocation(targetTypeInfo.TargetType);
        return;
      }

      case CollectionExpressionKind.Span:
      case CollectionExpressionKind.ReadOnlySpan:
      {
        if (collectionExpression.CollectionElementsEnumerable.IsEmpty())
        {
          return; // Span<T>.Empty
        }

        if (targetTypeInfo.Kind == CollectionExpressionKind.ReadOnlySpan
            && collectionExpression.CanBeLoweredToRuntimeHelpersCreateSpan())
        {
          return; // inline data or RuntimeHelpers.CreateSpan
        }

        if (collectionExpression.CanBeLoweredToInlineArray())
        {
          return; // stack allocated inline array
        }

        var arrayType = TypeFactory.CreateArrayType(targetTypeInfo.ElementType.NotNull(), rank: 1);
        ReportArrayAndPossibleTemporaryListAllocation(arrayType);
        return;
      }

      case CollectionExpressionKind.CollectionBuilder when targetTypeInfo.TargetType.IsGenericImmutableArray(out _):
      {
        // the array is constructed and moved into the ImmutableArray<T> struct

        if (collectionExpression.CollectionElementsEnumerable.IsEmpty())
        {
          return; // ImmutableCollectionsMarshal.AsImmutableArray<T>(Array.Empty<T>())
        }

        var arrayType = TypeFactory.CreateArrayType(targetTypeInfo.ElementType.NotNull(), rank: 1);
        ReportArrayAndPossibleTemporaryListAllocation(arrayType);
        return;
      }

      case CollectionExpressionKind.CollectionBuilder:
      {
        // we need to always construct the ReadOnlySpan<T> to call the factory method

        if (collectionExpression.CollectionElementsEnumerable.Any()
            && !collectionExpression.CanBeLoweredToRuntimeHelpersCreateSpan()
            && !collectionExpression.CanBeLoweredToInlineArray())
        {
          // we need heap array

          var arrayType = TypeFactory.CreateArrayType(targetTypeInfo.ElementType.NotNull(), rank: 1);
          ReportArrayAndPossibleTemporaryListAllocation(
            arrayType, additionalAllocation: new RichText(
              $"new '{PresentTypeName(targetTypeInfo.TargetType)}' collection creation"));
        }
        else // span is static (empty or not)
        {
          consumer.AddHighlighting(
            new ObjectAllocationPossibleHighlighting(
              collectionExpression.LBracket,
              new RichText($"new '{PresentTypeName(targetTypeInfo.TargetType)}' collection creation")),
            GetCollectionExpressionRangeToHighlight());
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
              consumer.AddHighlighting(
                new ObjectAllocationHighlighting(
                  collectionExpression.LBracket, new RichText(
                    $"new '{PresentTypeName(targetTypeInfo.TargetType)}' instance creation")),
                GetCollectionExpressionRangeToHighlight());
              break;
            }

            case TypeClassification.UNKNOWN when createdType.IsTypeParameterType():
            {
              consumer.AddHighlighting(
                new ObjectAllocationPossibleHighlighting(
                  collectionExpression.LBracket, new RichText(
                    $"new instance creation if '{PresentTypeName(targetTypeInfo.TargetType)}' " +
                    $"type parameter will be substituted with the reference type")),
                GetCollectionExpressionRangeToHighlight());
              break;
            }
          }
        }

        break;
      }

      case CollectionExpressionKind.ArrayInterface:
      {
        var targetType = targetTypeInfo.TargetType;
        if (targetType.IsGenericIList() || targetType.IsGenericICollection())
        {
          var genericListTypeElement = collectionExpression.GetPredefinedType().GenericList.GetTypeElement();
          if (genericListTypeElement == null) return;

          var genericListOfElementType = TypeFactory.CreateType(genericListTypeElement, [targetTypeInfo.ElementType]);

          consumer.AddHighlighting(
            new ObjectAllocationHighlighting(
              collectionExpression.LBracket,
              new RichText($"new '{PresentTypeName(genericListOfElementType)}' instance creation")),
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
              collectionExpression.LBracket, new RichText(
                $"new {storageKind} and '{PresentTypeName(targetTypeInfo.TargetType)}' implementation instance creation")),
            GetCollectionExpressionRangeToHighlight());
        }

        break;
      }

      default:
        throw new ArgumentOutOfRangeException();
    }

    return;

    [Pure]
    RichText PresentTypeName(IType type)
    {
      return type.GetPresentableName(
        collectionExpression.Language, CommonUtils.DefaultTypePresentationStyle);
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

    void ReportArrayAndPossibleTemporaryListAllocation(IType arrayType, RichText? additionalAllocation = null)
    {
      var typeName = PresentTypeName(arrayType);

      if (additionalAllocation != null)
      {
        additionalAllocation = " and " + additionalAllocation;
      }

      if (HasSpreadsOfUnknownLength(collectionExpression))
      {
        // temporary list is required, array can be conditionally allocated
        var firstSeparator = additionalAllocation == null ? " and" : ",";

        var message = CanBeEvaluatedToBeEmpty(collectionExpression)
          ? new RichText($"new temporary list{firstSeparator} possible (if not empty) '{typeName}' array instance creation")
          : new RichText($"new temporary list{firstSeparator} '{typeName}' array instance creation");

        consumer.AddHighlighting(
          new ObjectAllocationHighlighting(collectionExpression.LBracket, message + additionalAllocation),
          GetCollectionExpressionRangeToHighlight());
      }
      else
      {
        consumer.AddHighlighting(
          new ObjectAllocationHighlighting(
            collectionExpression.LBracket, new RichText(
              $"new '{typeName}' array instance creation{additionalAllocation}")),
          GetCollectionExpressionRangeToHighlight());
      }
    }
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