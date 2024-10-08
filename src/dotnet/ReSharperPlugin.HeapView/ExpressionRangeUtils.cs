using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView;

public static class ExpressionRangeUtils
{
  [Pure]
  public static DocumentRange GetExpressionRange(this ICSharpExpression expression)
  {
    switch (expression)
    {
      case IReferenceExpression referenceExpression:
        return referenceExpression.NameIdentifier.GetDocumentRange();

      case IInvocationExpression { InvokedExpression: IReferenceExpression { QualifierExpression: not null } invokedExpression }:
        return invokedExpression.NameIdentifier.GetDocumentRange();

      case IParenthesizedExpression parenthesizedExpression:
        return parenthesizedExpression.Expression.GetExpressionRange();

      default:
        return expression.GetDocumentRange();
    }
  }
}

[ShellComponent(Instantiation.DemandAnyThreadSafe)]
public sealed class HeapViewSeverityPresentationsProvider : IHighlightingCustomPresentationsForSeverityProvider
{
  public IEnumerable<string> GetAttributeIdsForSeverity(Severity severity)
  {
    if (severity is Severity.HINT or Severity.WARNING)
    {
      return [
        HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID,
        HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID
      ];
    }

    return [];
  }
}