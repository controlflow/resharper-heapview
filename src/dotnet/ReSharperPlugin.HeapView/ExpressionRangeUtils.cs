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

[ShellComponent(Instantiation.ContainerSyncPrimaryThread)]
public class ConfigurableSeverityHacks
{
  private static readonly Severity[] Severities =
  [
    Severity.HINT,
    Severity.WARNING
  ];

  private static readonly string[] HighlightingIds =
  [
    HeapViewAttributeIds.BOXING_HIGHLIGHTING_ID,
    HeapViewAttributeIds.ALLOCATION_HIGHLIGHTING_ID
  ];

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