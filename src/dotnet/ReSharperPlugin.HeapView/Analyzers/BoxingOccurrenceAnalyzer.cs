using System;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Conversions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
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
    protected override void Run(
      ICSharpExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      switch (expression)
      {
        case IInvocationExpression invocationExpression:
          CheckInheritedVirtualMethodInvocationOverValueType(invocationExpression, consumer);
          break;

        case IReferenceExpression referenceExpression:
          CheckStructMethodConversionToDelegateInstance(referenceExpression, consumer);
          break;

        case ICastExpression castExpression:
          CheckExpressionExplicitConversion(castExpression, data.GetTypeConversionRule(), consumer);
          break;

        case IParenthesizedExpression _:
        case ICheckedExpression _:
        case IUncheckedExpression _:
        case ISuppressNullableWarningExpression _:
          return; // do not analyze
      }

      CheckExpressionImplicitConversion(expression, expression.GetTypeConversionRule(), consumer);
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

      var qualifierTypeKind = IsQualifierOfValueType(notNullableType, includeStructTypeParameters: false);
      if (qualifierTypeKind == Classification.Not) return;

      if (qualifierTypeKind == Classification.Definitely)
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

      var qualifierTypeKind = IsQualifierOfValueType(qualifierType, includeStructTypeParameters: true);
      if (qualifierTypeKind == Classification.Not) return;

      var description = BakeDescriptionWithTypes(
        "conversion of value type '{0}' instance method to '{1}' delegate type", qualifierType, targetType);

      if (qualifierTypeKind == Classification.Definitely)
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
        var typeDeclaration = referenceExpression.GetContainingTypeDeclaration();
        if (typeDeclaration is IStructDeclaration { DeclaredElement: { } structTypeElement })
        {
          return TypeFactory.CreateType(structTypeElement);
        }

        return null;
      }

      var expressionType = qualifierExpression.GetExpressionType();
      return expressionType.ToIType();
    }

    private enum Classification { Definitely, Possibly, Not }

    [Pure]
    private static Classification IsQualifierOfValueType([NotNull] IType type, bool includeStructTypeParameters)
    {
      if (type.IsTypeParameterType())
      {
        return type.Classify switch
        {
          TypeClassification.REFERENCE_TYPE => Classification.Not,
          TypeClassification.VALUE_TYPE when includeStructTypeParameters => Classification.Definitely,
          _ => Classification.Possibly
        };
      }

      if (type.IsValueType())
      {
        return Classification.Definitely;
      }

      return Classification.Not;
    }

    private static void CheckExpressionImplicitConversion(
      [NotNull] ICSharpExpression expression, [NotNull] ICSharpTypeConversionRule conversionRule,
      [NotNull] IHighlightingConsumer consumer)
    {
      var sourceExpressionType = expression.GetExpressionType();
      if (sourceExpressionType.IsUnknown) return;

      var targetType = expression.GetImplicitlyConvertedTo();
      if (targetType.IsUnknown) return;

      var conversion = conversionRule.ClassifyImplicitConversionFromExpression(sourceExpressionType, targetType);

      var classification = CheckConversionInvolvesBoxing(conversion, sourceExpressionType, targetType);
      if (classification == Classification.Not) return;

      if (HeapAllocationAnalyzer.IsIgnoredContext(expression)) return;

      if (classification == Classification.Definitely)
      {
        var description = BakeDescriptionWithTypes(
          "conversion from '{0}' to '{1}' involves boxing of value type", sourceExpressionType, targetType);

        consumer.AddHighlighting(
          new BoxingAllocationHighlighting(expression, description),
          expression.GetExpressionRange());
      }
      else
      {
        var description = BakeDescriptionWithTypes(
          "conversion from '{0}' to '{1}' possibly involves boxing of value type", sourceExpressionType, targetType);

        consumer.AddHighlighting(
          new PossibleBoxingAllocationHighlighting(expression, description),
          expression.GetExpressionRange());
      }
    }

    private static void CheckExpressionExplicitConversion(
      [NotNull] ICastExpression castExpression, [NotNull] ICSharpTypeConversionRule conversionRule,
      [NotNull] IHighlightingConsumer consumer)
    {
      var castOperand = castExpression.Op;

      var sourceExpressionType = castOperand?.GetExpressionType();
      if (sourceExpressionType == null) return;

      var targetType = castExpression.GetExpressionType().ToIType();
      if (targetType == null) return;

      var conversion = conversionRule.ClassifyImplicitConversionFromExpression(sourceExpressionType, targetType);

      var classification = CheckConversionInvolvesBoxing(conversion, sourceExpressionType, targetType);
      if (classification == Classification.Not) return;

      if (HeapAllocationAnalyzer.IsIgnoredContext(castExpression)) return;

      if (classification == Classification.Definitely)
      {
        var description = BakeDescriptionWithTypes(
          "conversion from '{0}' to '{1}' involves boxing of value type", sourceExpressionType, targetType);

        consumer.AddHighlighting(
          new BoxingAllocationHighlighting(castExpression, description),
          castExpression.TargetType.GetDocumentRange());
      }
      else
      {
        var description = BakeDescriptionWithTypes(
          "conversion from '{0}' to '{1}' possibly involves boxing of value type", sourceExpressionType, targetType);

        consumer.AddHighlighting(
          new PossibleBoxingAllocationHighlighting(castExpression, description),
          castExpression.TargetType.GetDocumentRange());
      }
    }

    [Pure]
    private static Classification CheckConversionInvolvesBoxing(
      Conversion conversion, [NotNull] IExpressionType sourceType, [NotNull] IType targetType)
    {
      if (conversion.Kind == ConversionKind.Boxing)
      {
        return IsDefinitelyBoxing(sourceType, targetType) ? Classification.Definitely : Classification.Possibly;
      }

      var current = Classification.Not;

      foreach (var nestedInfo in conversion.GetNestedConversionsWithTypeInfo())
      {
        var newValue = CheckConversionInvolvesBoxing(
          nestedInfo.Conversion, nestedInfo.SourceType, nestedInfo.TargetType);

        switch (newValue)
        {
          case Classification.Definitely:
          case Classification.Possibly when current == Classification.Not:
            current = newValue;
            break;
        }
      }

      return current;
    }

    [Pure]
    private static bool IsDefinitelyBoxing([NotNull] IExpressionType sourceType, [NotNull] IType targetType)
    {
      var type = sourceType.ToIType();
      if (type is IDeclaredType (ITypeParameter _, _) from)
      {
        Assertion.Assert(!from.IsReferenceType(), "!from.IsReferenceType()");

        if (!from.IsValueType())
        {
          return false;
        }
        
        
      }

      return true;
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