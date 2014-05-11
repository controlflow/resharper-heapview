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
#if RESHARPER8
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Daemon.Stages;
#elif RESHARPER9
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
#endif

namespace JetBrains.ReSharper.HeapView.Analyzers
{
  // note: boxing in foreach (object x in new[] { 1, 2, 3 }) { } is not detected,
  // because of another C# highlighting (iteration var can be made of more specific type)

  [ElementProblemAnalyzer(
    elementTypes: new[] {
      typeof(ICSharpExpression)
    },
    HighlightingTypes = new[] {
      typeof(BoxingAllocationHighlighting)
    })]
  public sealed class BoxingOccuranceAnalyzer : ElementProblemAnalyzer<ICSharpExpression>
  {
    protected override void Run(
      ICSharpExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      var invocationExpression = expression as IInvocationExpression;
      if (invocationExpression != null)
      {
        CheckInvocation(invocationExpression, consumer);
      }

      CheckExpression(expression, consumer);
    }

    private static void CheckInvocation(
      [NotNull] IInvocationExpression invocationExpression, [NotNull] IHighlightingConsumer consumer)
    {
      var expressionReference = invocationExpression.InvocationExpressionReference;
      if (expressionReference == null) return;

      var method = expressionReference.Resolve().DeclaredElement as IMethod;
      if (method == null || method.IsExtensionMethod) return;

      var referenceExpression = invocationExpression.InvokedExpression as IReferenceExpression;
      if (referenceExpression == null) return;

      bool isValueType, typeParameterType = false;
      var qualifierExpression = referenceExpression.QualifierExpression;
      if (qualifierExpression == null || qualifierExpression is IBaseExpression)
      {
        var declaration = invocationExpression.GetContainingTypeDeclaration();
        isValueType = (declaration != null && declaration.DeclaredElement is IStruct);
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
        if (!typeParameterType || string.Equals(
          method.ShortName, "GetType", StringComparison.Ordinal))
        {
          consumer.AddHighlighting(
            new BoxingAllocationHighlighting(invocationExpression,
              "inherited System.Object virtual method call on value type instance"),
            referenceExpression.NameIdentifier.GetDocumentRange());
        }
      }
    }

    private static void CheckExpression(
      [NotNull] ICSharpExpression expression, [NotNull] IHighlightingConsumer consumer)
    {
      var targetType = expression.GetImplicitlyConvertedTo();
      if (!targetType.IsReferenceType()) return;

      var expressionType = expression.Type();
      if (expressionType.IsUnknown) return;

      if (targetType.IsDelegateType())
      {
        var referenceExpression = expression as IReferenceExpression;
        if (referenceExpression == null) return;

        var method = referenceExpression.Reference.Resolve().DeclaredElement as IMethod;
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
          var type = qualifierExpression.Type() as IDeclaredType;
          if (type != null) valueType = type.GetTypeElement();
        }

        if (valueType != null)
        {
          var sourceType = TypeFactory.CreateType(valueType);
          if (sourceType.IsValueType())
          {
            var description = BakeDescription(
              "conversion of value type '{0}' instance method to '{1}' delegate type",
              sourceType, targetType);

            consumer.AddHighlighting(
              new BoxingAllocationHighlighting(expression, description),
              referenceExpression.NameIdentifier.GetDocumentRange());
          }
        }
      }
      else
      {
        if (targetType.IsUnknown || targetType.Equals(expressionType)) return;

        var rule = expression.GetTypeConversionRule();
        if (rule.IsBoxingConversion(expressionType, targetType))
        {
          // there is no boxing conversion here: using (new DisposableStruct()) { }
          var usingStatement = UsingStatementNavigator.GetByExpression(
            expression.GetContainingParenthesizedExpression());
          if (usingStatement != null) return;

          var description = BakeDescription(
            "implicit conversion from value type '{0}' to reference type '{1}'",
            expressionType, targetType);

          consumer.AddHighlighting(
            new BoxingAllocationHighlighting(expression, description),
            expression.GetExpressionRange());
        }
      }
    }

    private static string BakeDescription(string format, params IType[] types)
    {
      return string.Format(format, Array.ConvertAll(
        types, t => (object)t.GetPresentableName(CSharpLanguage.Instance)));
    }
  }
}