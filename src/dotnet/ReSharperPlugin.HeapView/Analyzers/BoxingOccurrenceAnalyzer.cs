using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers
{
  // note: boxing in foreach (object x in new[] { 1, 2, 3 }) { } is not detected,
  // because of another C# highlighting (iteration var can be made of more specific type)

  // todo: possible boxing allocation

  [ElementProblemAnalyzer(typeof(ICSharpExpression), HighlightingTypes = new[] { typeof(BoxingAllocationHighlighting) })]
  public sealed class BoxingOccurrenceAnalyzer : ElementProblemAnalyzer<ICSharpExpression>
  {
    protected override void Run(ICSharpExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      switch (expression)
      {
        case IInvocationExpression invocationExpression:
          CheckInvocationExpression(invocationExpression, consumer);
          break;

        case IReferenceExpression referenceExpression:
          CheckReferenceExpression(referenceExpression, consumer);
          break;
      }

      CheckExpression(expression, consumer);
    }

    // todo: ?. invocations?
    private static void CheckInvocationExpression([NotNull] IInvocationExpression invocationExpression, [NotNull] IHighlightingConsumer consumer)
    {
      var invokedReference = invocationExpression.InvokedExpression as IReferenceExpression;
      if (invokedReference == null) return;

      var resolveResult = invocationExpression.InvocationExpressionReference.Resolve();
      if (!resolveResult.ResolveErrorType.IsAcceptable) return;

      var method = resolveResult.DeclaredElement as IMethod;
      if (method == null || method.IsStatic) return;

      var classType = method.GetContainingType() as IClass;
      if (classType == null) return;

      var qualifierType = GetQualifierExpressionType(invokedReference.QualifierExpression, invokedReference);

      const string description = "inherited 'System.Object' virtual method call on value type instance";

      if (qualifierType.IsValueType())
      {
        consumer.AddHighlighting(
          new BoxingAllocationHighlighting(invocationExpression, description),
          invokedReference.NameIdentifier.GetDocumentRange());
      }
      else if (qualifierType.IsUnconstrainedGenericType())
      {
        //consumer.AddHighlighting(
        //  new BoxingAllocationPossibleHighlighting(invocationExpression, description),
        //  invokedReference.NameIdentifier.GetDocumentRange());
      }
    }

    private static void CheckReferenceExpression([NotNull] IReferenceExpression referenceExpression, [NotNull] IHighlightingConsumer consumer)
    {
      var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(referenceExpression);
      if (invocationExpression != null) return;

      var (declaredElement, _, resolveErrorType) = referenceExpression.Reference.Resolve();
      if (!resolveErrorType.IsAcceptable) return;

      var method = declaredElement as IMethod;
      if (method == null || method.IsStatic || method.IsExtensionMethod) return;

      var qualifierType = GetQualifierExpressionType(referenceExpression.QualifierExpression, referenceExpression);
      if (qualifierType == null) return;

      var isValueType = qualifierType.IsValueType();
      if (!isValueType && !qualifierType.IsUnconstrainedGenericType()) return;

      var targetType = referenceExpression.GetImplicitlyConvertedTo(); // delayed
      if (!targetType.IsDelegateType()) return;

      var description = BakeDescriptionWithTypes(
        "conversion of value type '{0}' instance method to '{1}' delegate type", qualifierType, targetType);

      var nameIdentifier = referenceExpression.NameIdentifier;

      if (isValueType)
      {
        consumer.AddHighlighting(
          new BoxingAllocationHighlighting(nameIdentifier, description), nameIdentifier.GetDocumentRange());
      }
      else
      {
        //consumer.AddHighlighting(
        //  new BoxingAllocationPossibleHighlighting(nameIdentifier, description), nameIdentifier.GetDocumentRange());
      }
    }

    [CanBeNull, Pure]
    private static IType GetQualifierExpressionType([CanBeNull] ICSharpExpression qualifierExpression, [NotNull] ICSharpTreeNode context)
    {
      if (qualifierExpression.IsThisOrBaseOrNull())
      {
        var structDeclaration = context.GetContainingTypeDeclaration() as IStructDeclaration;

        var structTypeElement = structDeclaration?.DeclaredElement;
        if (structTypeElement == null) return null;

        return TypeFactory.CreateType(structTypeElement);
      }

      var expressionType = qualifierExpression.GetExpressionType();
      return expressionType.ToIType();
    }

    private static void CheckExpression([NotNull] ICSharpExpression expression, [NotNull] IHighlightingConsumer consumer)
    {
      var expressionType = expression.GetExpressionType();
      if (expressionType.IsUnknown) return;

      var sourceType = expressionType.ToIType();
      if (sourceType != null)
      {
        if (!sourceType.IsValueType() &&
            !sourceType.IsUnconstrainedGenericType()) return;
      }

      var targetType = expression.GetImplicitlyConvertedTo();
      if (targetType.IsUnknown) return;



      if (!targetType.IsReferenceType()) // todo: not only?
        return;



      
      //if (expressionType.IsUnknown) return;

      /*{
        if (targetType.IsUnknown || targetType.Equals(expressionType)) return;

        var conversionRule = expression.GetTypeConversionRule();
        if (conversionRule.IsBoxingConversion(expressionType, targetType))
        {
          // there is no boxing conversion here: using (new DisposableStruct()) { }
          var usingStatement = UsingStatementNavigator.GetByExpression(expression.GetContainingParenthesizedExpression());
          if (usingStatement != null) return;

          if (HeapAllocationAnalyzer.IsIgnoredContext(expression)) return;

          var description = BakeDescriptionWithTypes("conversion from value type '{0}' to reference type '{1}'", expressionType, targetType);

          consumer.AddHighlighting(
            new BoxingAllocationHighlighting(expression, description), expression.GetExpressionRange());
        }
      }*/
    }

    [NotNull, StringFormatMethod("format")]
    private static string BakeDescriptionWithTypes([NotNull] string format, [NotNull] params IType[] types)
    {
      var args = Array.ConvertAll(types, type => (object) type.GetPresentableName(CSharpLanguage.Instance));
      return string.Format(format, args);
    }
  }
}