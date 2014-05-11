using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.HeapView
{
  public static class ExpressionRangeUtils
  {
    public static DocumentRange GetExpressionRange([NotNull] this ICSharpExpression expression)
    {
      var reference = expression as IReferenceExpression;
      if (reference != null)
        return reference.NameIdentifier.GetDocumentRange();

      var parenthesized = expression as IParenthesizedExpression;
      if (parenthesized != null)
        return parenthesized.Expression.GetExpressionRange();

      return expression.GetDocumentRange();
    }
  }
}