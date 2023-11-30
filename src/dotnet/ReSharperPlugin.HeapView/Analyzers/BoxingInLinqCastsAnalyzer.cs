using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve.ExtensionMethods;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: [
    typeof(IInvocationExpression),
    typeof(IQueryCastReferenceProvider)
  ],
  HighlightingTypes = [
    typeof(BoxingAllocationHighlighting),
    typeof(PossibleBoxingAllocationHighlighting)
  ])]
public class BoxingInLinqCastsAnalyzer : HeapAllocationAnalyzerBase<ITreeNode>
{
  protected override void Run(ITreeNode element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    switch (element)
    {
      // intArray.Cast<object>();
      case IInvocationExpression invocationExpression:
        CheckLinqEnumerableCastConversion(invocationExpression, data, consumer);
        break;

      // from object o in intArray
      case IQueryCastReferenceProvider queryCastReferenceProvider:
        CheckLinqQueryCastConversion(queryCastReferenceProvider, data, consumer);
        break;
    }
  }

  private static void CheckLinqQueryCastConversion(
    IQueryCastReferenceProvider queryCastReferenceProvider, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var castReference = queryCastReferenceProvider.CastReference;
    if (castReference == null) return;

    var collection = queryCastReferenceProvider.Expression;
    if (collection == null) return;

    var targetTypeUsage = GetCastNode(queryCastReferenceProvider);
    if (targetTypeUsage == null) return;

    var resolveResult = castReference.Resolve();
    if (!resolveResult.ResolveErrorType.IsAcceptable) return;

    if (resolveResult.DeclaredElement is not IMethod { ShortName: nameof(Enumerable.Cast) } castMethod) return;

    var targetTypeParameter = castMethod.TypeParameters.SingleItem();
    if (targetTypeParameter == null) return;

    var castTargetType = resolveResult.Substitution[targetTypeParameter];

    var sourceType = TryGetEnumerableCollectionType(collection.Type());
    if (sourceType == null) return;

    BoxingInExpressionConversionsAnalyzer.CheckConversionRequiresBoxing(
      sourceType, castTargetType, targetTypeUsage,
      static (rule, source, target) => rule.ClassifyConversionFromExpression(source, target),
      data, consumer);

    return;

    static ITypeUsage? GetCastNode(IQueryCastReferenceProvider queryCastReferenceProvider)
    {
      return queryCastReferenceProvider switch
      {
        IQueryFirstFrom queryFirstFrom => queryFirstFrom.TypeUsage,
        IQueryFromClause queryFromClause => queryFromClause.TypeUsage,
        IQueryJoinClause queryJoinClause => queryJoinClause.TypeUsage,
        _ => throw new ArgumentOutOfRangeException(nameof(queryCastReferenceProvider))
      };
    }
  }

  private static readonly ClrTypeName SystemEnumerableClassTypeName = new("System.Linq.Enumerable");

  private static void CheckLinqEnumerableCastConversion(
    IInvocationExpression invocationExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (invocationExpression.InvokedExpression is not IReferenceExpression invokedReferenceExpression) return;

    var typeArgumentList = invokedReferenceExpression.TypeArgumentList;
    if (typeArgumentList == null) return;

    if (!typeArgumentList.CommasEnumerable.IsEmpty()) return; // single type argument expected

    var resolveResult = invocationExpression.Reference.Resolve();
    if (!resolveResult.ResolveErrorType.IsAcceptable) return;

    if (resolveResult.DeclaredElement is not IMethod
        {
          ShortName: nameof(Enumerable.Cast),
          ContainingType: { ShortName: nameof(Enumerable) } containingType
        } castMethod)
    {
      return;
    }

    if (!containingType.GetClrName().Equals(SystemEnumerableClassTypeName)) return;

    var collectionExpression = resolveResult.Result.IsExtensionMethodInvocation()
      ? invokedReferenceExpression.QualifierExpression
      : invocationExpression.ArgumentsEnumerable.SingleItem?.Value;

    if (collectionExpression == null) return;

    var targetTypeUsage = typeArgumentList.TypeArgumentNodes.SingleItem();
    if (targetTypeUsage == null) return;

    var targetTypeParameter = castMethod.TypeParameters.SingleItem();
    if (targetTypeParameter == null) return;

    var castTargetType = resolveResult.Substitution[targetTypeParameter];

    var sourceType = TryGetEnumerableCollectionType(collectionExpression.Type());
    if (sourceType == null) return;

    BoxingInExpressionConversionsAnalyzer.CheckConversionRequiresBoxing(
      sourceType, castTargetType, targetTypeUsage,
      static (rule, source, target) => rule.ClassifyConversionFromExpression(source, target),
      data, consumer);
  }

  [Pure]
  private static IType? TryGetEnumerableCollectionType(IType sourceType)
  {
    switch (sourceType)
    {
      case IArrayType arrayType:
      {
        return arrayType.ElementType;
      }

      case IDeclaredType ({ } typeElement, var substitution):
      {
        var predefinedType = sourceType.Module.GetPredefinedType();

        var iEnumerable = predefinedType.GenericIEnumerable.GetTypeElement();
        if (iEnumerable is { TypeParameters: { Count: 1 } typeParameters })
        {
          var singleImplementation = typeElement.GetAncestorSubstitution(iEnumerable).SingleItem;
          if (singleImplementation != null)
          {
            return substitution.Apply(singleImplementation).Apply(typeParameters[0]);
          }
        }

        break;
      }
    }

    return null;
  }
}