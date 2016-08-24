using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.HeapView
{
  public static class ExpressionRangeUtils
  {
    public static DocumentRange GetExpressionRange([NotNull] this ICSharpExpression expression)
    {
      var reference = expression as IReferenceExpression;
      if (reference != null)
      {
        return reference.NameIdentifier.GetDocumentRange();
      }

      var parenthesized = expression as IParenthesizedExpression;
      if (parenthesized != null)
      {
        return parenthesized.Expression.GetExpressionRange();
      }

      return expression.GetDocumentRange();
    }
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
          ICollection<string> collection;
          if (!severityIds.TryGetValue(severity, out collection)) continue;

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
}