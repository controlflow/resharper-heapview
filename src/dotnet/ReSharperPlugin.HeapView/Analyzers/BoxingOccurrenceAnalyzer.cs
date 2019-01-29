using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;

using JetBrains.ReSharper.Psi.CSharp.Conversions;

namespace JetBrains.ReSharper.HeapView.Analyzers
{
  // note: boxing in foreach (object x in new[] { 1, 2, 3 }) { } is not detected,
  // because of another C# highlighting (iteration var can be made of more specific type)

  [ElementProblemAnalyzer(typeof(ICSharpExpression), HighlightingTypes = new[] { typeof(BoxingAllocationHighlighting) })]
  public sealed class BoxingOccurrenceAnalyzer : ElementProblemAnalyzer<ICSharpExpression>
  {
    protected override void Run(ICSharpExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      var invocationExpression = expression as IInvocationExpression;
      if (invocationExpression != null)
      {
        CheckInvocation(invocationExpression, consumer);
      }

      CheckExpression(expression, consumer);
    }

    private static void CheckInvocation([NotNull] IInvocationExpression invocationExpression, [NotNull] IHighlightingConsumer consumer)
    {
      var expressionReference = invocationExpression.InvocationExpressionReference;

      var method = expressionReference.Resolve().DeclaredElement as IMethod;
      if (method == null || method.IsExtensionMethod) return;

      var referenceExpression = invocationExpression.InvokedExpression as IReferenceExpression;
      if (referenceExpression == null) return;

      bool isValueType, typeParameterType = false;
      var qualifierExpression = referenceExpression.QualifierExpression;
      if (qualifierExpression == null || qualifierExpression is IBaseExpression)
      {
        var declaration = invocationExpression.GetContainingTypeDeclaration();
        isValueType = declaration?.DeclaredElement is IStruct;
      }
      else
      {
        var type = qualifierExpression.Type();
        typeParameterType = type.IsTypeParameterType();
        isValueType = type.IsValueType();
      }

      if (isValueType && method.GetContainingType() is IClass && !method.IsStatic)
      {
        // do not produce possible false positive when type is type parameter type
        if (!typeParameterType || string.Equals(method.ShortName, "GetType", StringComparison.Ordinal))
        {
          consumer.AddHighlighting(
            new BoxingAllocationHighlighting(
              invocationExpression, "inherited System.Object virtual method call on value type instance"),
            referenceExpression.NameIdentifier.GetDocumentRange());
        }
      }
    }

    private static void CheckExpression([NotNull] ICSharpExpression expression, [NotNull] IHighlightingConsumer consumer)
    {
      var targetType = expression.GetImplicitlyConvertedTo();
      if (!targetType.IsReferenceType()) return;

      var expressionType = expression.Type();
      if (expressionType.IsUnknown) return;

      if (targetType.IsDelegateType())
      {
        var referenceExpression = expression as IReferenceExpression;

        var method = referenceExpression?.Reference.Resolve().DeclaredElement as IMethod;
        if (method == null || method.IsStatic || method.IsExtensionMethod) return;

        ITypeElement valueType = null;
        var qualifierExpression = referenceExpression.QualifierExpression;
        if (qualifierExpression == null || qualifierExpression is IBaseExpression)
        {
          var declaration = expression.GetContainingTypeDeclaration();
          if (declaration != null)
            valueType = declaration.DeclaredElement as IStruct;
        }
        else
        {
          if (qualifierExpression.Type() is IDeclaredType declaredType)
          {
            valueType = declaredType.GetTypeElement();
          }
        }

        if (valueType != null)
        {
          var sourceType = TypeFactory.CreateType(valueType);
          if (sourceType.IsValueType())
          {
            var description = BakeDescription(
              "conversion of value type '{0}' instance method to '{1}' delegate type", sourceType, targetType);

            consumer.AddHighlighting(
              new BoxingAllocationHighlighting(expression, description),
              referenceExpression.NameIdentifier.GetDocumentRange());
          }
        }
      }
      else
      {
        if (targetType.IsUnknown || targetType.Equals(expressionType)) return;

        var conversionRule = expression.GetTypeConversionRule();
        if (conversionRule.IsBoxingConversion(expressionType, targetType))
        {
          // there is no boxing conversion here: using (new DisposableStruct()) { }
          var usingStatement = UsingStatementNavigator.GetByExpression(expression.GetContainingParenthesizedExpression());
          if (usingStatement != null) return;

          if (HeapAllocationAnalyzer.IsIgnoredContext(expression)) return;

          var description = BakeDescription("conversion from value type '{0}' to reference type '{1}'", expressionType, targetType);

          consumer.AddHighlighting(
            new BoxingAllocationHighlighting(expression, description), expression.GetExpressionRange());
        }
      }
    }

    [NotNull, StringFormatMethod("format")]
    private static string BakeDescription([NotNull] string format, [NotNull] params IType[] types)
    {
      var args = Array.ConvertAll(types, t => (object) t.GetPresentableName(CSharpLanguage.Instance));
      return string.Format(format, args);
    }
  }
}