using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.DeclaredElements;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.UI.RichText;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: [ typeof(IForeachStatement), typeof(ISpreadElement) ],
  HighlightingTypes = [ typeof(ObjectAllocationPossibleHighlighting) ])]
public class AllocationOfForeachEnumeratorAnalyzer : HeapAllocationAnalyzerBase<IForeachReferencesOwner>
{
  protected override void Run(
    IForeachReferencesOwner foreachReferencesOwner, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (foreachReferencesOwner.IsInTheContextWhereAllocationsAreNotImportant()) return;

    var (collectionExpression, tokenToHighlight) = foreachReferencesOwner switch
    {
      IForeachStatement foreachStatement => (foreachStatement.Collection, foreachStatement.ForeachHeader?.InKeyword),
      ISpreadElement spreadElement => (spreadElement.Collection, spreadElement.OperatorSign),
      _ => (null, null)
    };

    if (collectionExpression == null || tokenToHighlight == null) return;

    var collectionType = collectionExpression.GetExpressionType().ToIType();
    if (collectionType is not { IsResolved: true }) return;

    var resolveResult = foreachReferencesOwner.GetEnumeratorReference.Resolve();
    if (resolveResult.ResolveErrorType.IsAcceptable
        && resolveResult.DeclaredElement is IMethod getEnumeratorMethod)
    {
      var enumeratorType = resolveResult.Substitution[getEnumeratorMethod.ReturnType];
      if (enumeratorType.IsReferenceType()
          && !IsIteratorMemberAccess(collectionExpression)
          && !IsOptimizedCollectionType(collectionType))
      {
        var richText = new RichText()
          .Append("new '")
          .Append(enumeratorType.GetPresentableName(
            foreachReferencesOwner.Language, CommonUtils.DefaultTypePresentationStyle))
          .Append("' instance creation on '")
          .Append(DeclaredElementPresenter.Format(
            CSharpLanguage.Instance!, DeclaredElementPresenter.NAME_PRESENTER, getEnumeratorMethod))
          .Append("()' call (except when it's cached by the implementation)");

        consumer.AddHighlighting(new ObjectAllocationPossibleHighlighting(tokenToHighlight, richText));
      }
    }
  }

  [Pure]
  private static bool IsIteratorMemberAccess(ICSharpExpression collectionExpression)
  {
    switch (collectionExpression.GetOperandThroughParenthesis())
    {
      case IReferenceExpression referenceExpression
        when referenceExpression.Reference.Resolve().DeclaredElement
          is IProperty { Getter.IsIterator: true }:
      case IInvocationExpression invocationExpression
        when invocationExpression.Reference.Resolve().DeclaredElement
          is IMethod { IsIterator: true } or ILocalFunction { IsIterator: true }:
      {
        return true; // less false positives
      }

      default:
      {
        return false;
      }
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