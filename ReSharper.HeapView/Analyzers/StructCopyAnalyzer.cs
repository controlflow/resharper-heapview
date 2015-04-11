using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
#if RESHARPER8
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Daemon.Stages;
#elif RESHARPER9
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
#endif

namespace JetBrains.ReSharper.HeapView.Analyzers
{
  //[ElementProblemAnalyzer(typeof(ICSharpExpression), HighlightingTypes = new []{ typeof(StructCopyHighlighting) })]
  public sealed class StructCopyAnalyzer : ElementProblemAnalyzer<ICSharpExpression>
  {
    protected override void Run(ICSharpExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      if (expression is IParenthesizedExpression) return;
      if (expression is IUncheckedExpression) return;
      if (expression is ICheckedExpression) return;

      var declaredType = expression.Type() as IDeclaredType;
      if (declaredType != null)
      {
        if (declaredType.Classify == TypeClassification.VALUE_TYPE)
        {
          if (declaredType.IsVoid()) return;

          var referenceExpression = ReferenceExpressionNavigator.GetByQualifierExpression(expression);
          if (referenceExpression != null)
          {
            var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(referenceExpression);
            if (invocationExpression != null)
            {
              if (!expression.IsClassifiedAsVariable)
              {
                //consumer.AddHighlighting(
                //  new StructCopyHighlighting(expression, "method invocation"),
                //  referenceExpression.NameIdentifier.GetDocumentRange());
                return;
              }
            }
          }

          var elementAccessExpression = ElementAccessExpressionNavigator.GetByOperand(expression);
          if (elementAccessExpression != null)
          {
            if (!expression.IsClassifiedAsVariable)
            {
              //consumer.AddHighlighting(
              //  new StructCopyHighlighting(expression, "element access"),
              //  elementAccessExpression.LBracket.GetDocumentRange());
              return;
            }
          }

          //consumer.AddHighlighting(
          //  new StructCopyHighlighting(expression, "of type " + declaredType.GetLongPresentableName(expression.Language)),
          //  expression.GetExpressionRange());
        }
      }
    }
  }
}