using System;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Conversions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers
{
  // note: boxing in foreach (object x in new[] { 1, 2, 3 }) { } is not detected,
  // because of another C# highlighting (iteration var can be made of more specific type)

  [ElementProblemAnalyzer(
    ElementTypes: typeof(ICSharpExpression),
    HighlightingTypes = new[] {
      typeof(BoxingAllocationHighlighting),
      typeof(PossibleBoxingAllocationHighlighting)
    })]
  public sealed class BoxingOccurrenceAnalyzer : ElementProblemAnalyzer<ICSharpExpression>
  {
    protected override void Run(ICSharpExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      switch (expression)
      {
        case IInvocationExpression invocationExpression:
          CheckInheritedVirtualMethodInvocationOverValueType(invocationExpression, consumer);
          break;

        case IReferenceExpression referenceExpression:
          CheckStructMethodConversionToDelegateInstance(referenceExpression, consumer);
          break;

        case IParenthesizedExpression _:
        case ICheckedExpression _:
        case IUncheckedExpression _:
        case ISuppressNullableWarningExpression _:
          return; // do not analyze
      }

      CheckExpressionImplicitConversion(expression, consumer);
    }

    private static void CheckInheritedVirtualMethodInvocationOverValueType(
      [NotNull] IInvocationExpression invocationExpression, [NotNull] IHighlightingConsumer consumer)
    {
      var invokedReferenceExpression = invocationExpression.InvokedExpression.GetOperandThroughParenthesis() as IReferenceExpression;
      if (invokedReferenceExpression == null) return;

      var (declaredElement, _, resolveErrorType) = invocationExpression.InvocationExpressionReference.Resolve();
      if (!resolveErrorType.IsAcceptable) return;

      var method = declaredElement as IMethod;
      if (method == null) return; // we are not interested in local functions or anything else
      if (method.IsStatic) return;

      var methodClassType = method.GetContainingType() as IClass;
      if (methodClassType == null) return;

      var qualifierType = TryGetQualifierExpressionType(invokedReferenceExpression);
      if (qualifierType == null) return;

      const string description = "inherited 'System.Object' virtual method call on value type instance";

      var notNullableType = qualifierType.Unlift(); // Nullable<T> overrides everything, but invokes the same methods on T

      var qualifierTypeKind = ClassifyQualifierType(notNullableType, includeStructTypeParameters: false);
      if (qualifierTypeKind == TypeKind.NotValueType) return;

      if (qualifierTypeKind == TypeKind.DefinitelyValueType)
      {
        consumer.AddHighlighting(
          new BoxingAllocationHighlighting(invokedReferenceExpression.NameIdentifier, description));
      }
      else
      {
        consumer.AddHighlighting(
          new PossibleBoxingAllocationHighlighting(invokedReferenceExpression.NameIdentifier, description));
      }
    }

    private static void CheckStructMethodConversionToDelegateInstance(
      [NotNull] IReferenceExpression referenceExpression, [NotNull] IHighlightingConsumer consumer)
    {
      var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(referenceExpression);
      if (invocationExpression != null) return; // also filters out 'nameof(o.M)'

      var (declaredElement, _, resolveErrorType) = referenceExpression.Reference.Resolve();
      if (!resolveErrorType.IsAcceptable) return;

      var method = declaredElement as IMethod;
      if (method == null || method.IsStatic) return;

      var qualifierType = TryGetQualifierExpressionType(referenceExpression);
      if (qualifierType == null) return;

      var targetType = referenceExpression.GetImplicitlyConvertedTo();
      if (!targetType.IsDelegateType()) return;

      var qualifierTypeKind = ClassifyQualifierType(qualifierType, includeStructTypeParameters: true);
      if (qualifierTypeKind == TypeKind.NotValueType) return;

      var description = BakeDescriptionWithTypes(
        "conversion of value type '{0}' instance method to '{1}' delegate type", qualifierType, targetType);

      if (qualifierTypeKind == TypeKind.DefinitelyValueType)
      {
        consumer.AddHighlighting(
          new BoxingAllocationHighlighting(referenceExpression.NameIdentifier, description));
      }
      else
      {
        consumer.AddHighlighting(
          new PossibleBoxingAllocationHighlighting(referenceExpression.NameIdentifier, description));
      }
    }

    [CanBeNull, Pure]
    private static IType TryGetQualifierExpressionType([NotNull] IReferenceExpression referenceExpression)
    {
      var qualifierExpression = referenceExpression.QualifierExpression;
      if (qualifierExpression.IsThisOrBaseOrNull())
      {
        if (referenceExpression.GetContainingTypeDeclaration()
              is IStructDeclaration { DeclaredElement: { } structTypeElement })
        {
          return TypeFactory.CreateType(structTypeElement);
        }

        return null;
      }

      var expressionType = qualifierExpression.GetExpressionType();
      return expressionType.ToIType();
    }

    private enum TypeKind { DefinitelyValueType, CanBeValueType, NotValueType }

    [Pure]
    private static TypeKind ClassifyQualifierType([NotNull] IType type, bool includeStructTypeParameters)
    {
      if (type.IsTypeParameterType())
      {
        return type.Classify switch
        {
          TypeClassification.REFERENCE_TYPE => TypeKind.NotValueType,
          TypeClassification.VALUE_TYPE when includeStructTypeParameters => TypeKind.DefinitelyValueType,
          _ => TypeKind.CanBeValueType
        };
      }

      if (type.IsValueType())
      {
        return TypeKind.DefinitelyValueType;
      }

      return TypeKind.NotValueType;
    }

    private static void CheckExpressionImplicitConversion([NotNull] ICSharpExpression expression, [NotNull] IHighlightingConsumer consumer)
    {
      // var referenceExpression = ReferenceExpressionNavigator.GetByQualifierExpression(expression);
      // if (InvocationExpressionNavigator.GetByInvokedExpression(referenceExpression) != null)
      // {
      //   GC.KeepAlive(expression);
      // }

      var expressionType = expression.GetExpressionType();
      if (expressionType.IsUnknown) return;

      var targetType = expression.GetImplicitlyConvertedTo();
      if (targetType.IsUnknown) return;

      var conversionRule = expression.GetTypeConversionRule();
      var conversion = conversionRule.ClassifyImplicitConversionFromExpression(expressionType, targetType);



      AnalyzeConversion(conversion, expressionType, targetType, expression, consumer);
    }

    private static void AnalyzeConversion(
      Conversion conversion, [NotNull] IExpressionType sourceType, [NotNull] IType targetType,
      [NotNull] ICSharpExpression expression, [NotNull] IHighlightingConsumer consumer)
    {
      // todo: that's a bit too much, some boxings are "possible"
      if (conversion.Kind == ConversionKind.Boxing)
      {
        if (HeapAllocationAnalyzer.IsIgnoredContext(expression)) return;


        var description = BakeDescriptionWithTypes(
          "conversion from value type '{0}' to reference type '{1}'", sourceType, targetType);

        consumer.AddHighlighting(
          new BoxingAllocationHighlighting(expression, description), expression.GetExpressionRange());
      }

      if (conversion.Kind == ConversionKind.ImplicitTuple || conversion.Kind == ConversionKind.ImplicitTupleLiteral)
      {
        foreach (var typeConversionInfo in conversion.GetNestedConversionsWithTypeInfo())
        {
        }
      }

      if (conversion.Kind == ConversionKind.ImplicitUserDefined)
      {
        var analysis = conversion.UserDefinedConversionAnalysis;
        if (analysis != null)
        {
        }
      }
    }

    [NotNull, StringFormatMethod("format")]
    private static string BakeDescriptionWithTypes([NotNull] string format, [NotNull] params IExpressionType[] types)
    {
      var args = Array.ConvertAll(types, expressionType =>
      {
        if (expressionType is IType type)
          return (object) type.GetPresentableName(CSharpLanguage.Instance.NotNull());

        return expressionType.GetLongPresentableName(CSharpLanguage.Instance.NotNull());
      });

      return string.Format(format, args);
    }
  }
}