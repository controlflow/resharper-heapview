using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView;

public static class ExpressionRangeUtils
{
  [Pure]
  public static DocumentRange GetExpressionRange([NotNull] this ICSharpExpression expression)
  {
    switch (expression)
    {
      case IReferenceExpression referenceExpression:
        return referenceExpression.NameIdentifier.GetDocumentRange();

      case IInvocationExpression { InvokedExpression: IReferenceExpression { QualifierExpression: { } } invokedExpression }:
        // todo: too short?
        return invokedExpression.NameIdentifier.GetDocumentRange();

      case IParenthesizedExpression parenthesizedExpression:
        return parenthesizedExpression.Expression.GetExpressionRange();

      default:
        return expression.GetDocumentRange();
    }
  }

  // todo: IsUnconstrainedGenericType
}

[ShellComponent]
public class ConfigurableSeverityHacks
{
  [NotNull] private static readonly Severity[] Severities = {
    Severity.HINT,
    Severity.WARNING
  };

  [NotNull] private static readonly string[] HighlightingIds = {
    HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID,
    HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID,
    HeapViewAttributeIds.STRUCT_COPY_ID
  };

  public ConfigurableSeverityHacks()
  {
    var severityIds = HighlightingAttributeIds.ValidHighlightingsForSeverity;
    lock (severityIds)
    {
      foreach (var severity in Severities)
      {
        if (!severityIds.TryGetValue(severity, out var collection)) continue;

        foreach (var highlightingId in HighlightingIds)
        {
          if (!collection.Contains(highlightingId))
          {
            collection.Add(highlightingId);
          }
        }
      }
    }
  }
}