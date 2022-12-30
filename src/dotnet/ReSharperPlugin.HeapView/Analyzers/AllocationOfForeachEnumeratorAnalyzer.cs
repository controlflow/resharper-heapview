#nullable enable
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.DeclaredElements;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(IForeachStatement) },
  HighlightingTypes = new[] { typeof(ObjectAllocationPossibleHighlighting) })]
public class AllocationOfForeachEnumeratorAnalyzer : HeapAllocationAnalyzerBase<IForeachStatement>
{
  protected override void Run(
    IForeachStatement foreachStatement, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (foreachStatement is not { Collection: { } collectionExpression, ForeachHeader.InKeyword: { } inKeyword }) return;

    if (foreachStatement.IsInTheContextWhereAllocationsAreNotImportant()) return;

    var collectionType = collectionExpression.GetExpressionType().ToIType();
    if (collectionType is not { IsResolved: true }) return;

    var resolveResult = foreachStatement.GetEnumeratorReference.Resolve();
    if (resolveResult.ResolveErrorType.IsAcceptable
        && resolveResult.DeclaredElement is IMethod getEnumeratorMethod)
    {
      var enumeratorType = resolveResult.Substitution[getEnumeratorMethod.ReturnType];
      if (enumeratorType.IsReferenceType()
          && !IsIteratorMemberAccess(collectionExpression)
          && !IsOptimizedCollectionType(collectionType))
      {
        var enumeratorTypeName = enumeratorType.GetPresentableName(foreachStatement.Language, CommonUtils.DefaultTypePresentationStyle);

        consumer.AddHighlighting(new ObjectAllocationPossibleHighlighting(
          inKeyword, $"new '{enumeratorTypeName}' instance creation on '{getEnumeratorMethod.ShortName}()' call (except when it's cached by the implementation)"));
      }
    }
  }

  [Pure]
  private static bool IsIteratorMemberAccess(ICSharpExpression collectionExpression)
  {
    switch (collectionExpression.GetOperandThroughParenthesis())
    {
      case IReferenceExpression { Reference: (IProperty { Getter.IsIterator: true }, _) }:
      case IInvocationExpression { Reference: (IMethod { IsIterator: true } or ILocalFunction { IsIterator: true }, _) }:
        return true; // less false positives
      default:
        return false;
    }
  }

  [Pure]
  private static bool IsOptimizedCollectionType(IType collectionType)
  {
    if (collectionType.IsString()) return true;
    if (collectionType is IArrayType) return true;

    return false;
  }
}