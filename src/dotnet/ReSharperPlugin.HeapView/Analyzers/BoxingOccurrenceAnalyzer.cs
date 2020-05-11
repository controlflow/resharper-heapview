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
using JetBrains.ReSharper.Psi.CSharp.Util.NullChecks;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Managed;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers
{
  // todo: compiler optimizations
  // todo: 's is I' boxes in .NET Framework
  // todo: constant contexts? throw contexts?

  [ElementProblemAnalyzer(
    ElementTypes: new[] { typeof(ICSharpExpression), typeof(ITypeCheckPattern) },
    HighlightingTypes = new[] {
      typeof(BoxingAllocationHighlighting),
      typeof(PossibleBoxingAllocationHighlighting)
    })]
  public sealed class BoxingOccurrenceAnalyzer : IElementProblemAnalyzer
  {
    public void Run(ITreeNode element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      switch (element)
      {
        case IInvocationExpression invocationExpression:
          CheckInheritedVirtualMethodInvocationOverValueType(invocationExpression, consumer);
          break;

        case IReferenceExpression referenceExpression:
          CheckStructMethodConversionToDelegateInstance(referenceExpression, consumer);
          break;

        case ICastExpression castExpression:
          CheckExpressionExplicitConversion(castExpression, data, consumer);
          break;

        case IDeclarationExpression declarationExpression:
          CheckDeclarationExpressionConversion(declarationExpression, data, consumer);
          break;

        case IAssignmentExpression assignmentExpression:
          CheckDeconstructingAssignmentConversions(assignmentExpression, data, consumer);
          break;

        case ITypeCheckPattern typeCheckPattern:
          CheckPatternMatchingConversion(typeCheckPattern, consumer);
          break;

        case IParenthesizedExpression _:
        case ICheckedExpression _:
        case IUncheckedExpression _:
        case ISuppressNullableWarningExpression _:
          return; // do not analyze implicit conversion
      }

      if (element is ICSharpExpression expression)
      {
        CheckExpressionImplicitConversion(expression, data, consumer);
      }
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
      if (qualifierTypeKind == BoxingClassification.Not) return;

      if (qualifierTypeKind == BoxingClassification.Definitely)
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
      if (qualifierTypeKind == BoxingClassification.Not) return;

      var description = BakeDescriptionWithTypes(
        "conversion of value type '{0}' instance method to '{1}' delegate type", qualifierType, targetType);

      if (qualifierTypeKind == BoxingClassification.Definitely)
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

    private enum BoxingClassification { Definitely, Possibly, Not }

    [Pure]
    private static BoxingClassification IsQualifierOfValueType([NotNull] IType type, bool includeStructTypeParameters)
    {
      if (type.IsTypeParameterType())
      {
        return type.Classify switch
        {
          TypeClassification.REFERENCE_TYPE => BoxingClassification.Not,
          TypeClassification.VALUE_TYPE when includeStructTypeParameters => BoxingClassification.Definitely,
          _ => BoxingClassification.Possibly
        };
      }

      if (type.IsValueType())
      {
        return BoxingClassification.Definitely;
      }

      return BoxingClassification.Not;
    }

    private static void CheckExpressionImplicitConversion(
      [NotNull] ICSharpExpression expression, [NotNull] ElementProblemAnalyzerData data,
      [NotNull] IHighlightingConsumer consumer)
    {
      if (!IsImplicitConversionActuallyHappens(expression)) return;

      var sourceExpressionType = expression.GetExpressionType();
      if (sourceExpressionType.IsUnknown) return;

      var targetType = expression.GetImplicitlyConvertedTo();
      if (targetType.IsUnknown) return;

      if (IsBoxingEliminatedAtRuntime(expression, data)) return;

      CheckConversionRequiresBoxing(
        sourceExpressionType, targetType, expression, isExplicitCast: false, data, consumer);
    }

    [Pure]
    private static bool IsImplicitConversionActuallyHappens([NotNull] ICSharpExpression expression)
    {
      var containingParenthesized = expression.GetContainingParenthesizedExpression();

      var castExpression = CastExpressionNavigator.GetByOp(containingParenthesized);
      if (castExpression != null) return false; // filter out explicit casts

      var tupleComponent = TupleComponentNavigator.GetByValue(containingParenthesized);
      if (tupleComponent != null) return false; // report whole tuple instead

      var assignmentExpression = AssignmentExpressionNavigator.GetBySource(containingParenthesized);
      if (assignmentExpression != null)
      {
        var assignmentKind = assignmentExpression.GetAssignmentKind();
        if (assignmentKind != AssignmentKind.OrdinaryAssignment)
          return false; // deconstructions are analyzed not as tuple conversion
      }

      return true;
    }

    private static void CheckExpressionExplicitConversion(
      [NotNull] ICastExpression castExpression, [NotNull] ElementProblemAnalyzerData data,
      [NotNull] IHighlightingConsumer consumer)
    {
      var castOperand = castExpression.Op;

      var sourceExpressionType = castOperand?.GetExpressionType();
      if (sourceExpressionType == null) return;

      var targetType = castExpression.GetExpressionType().ToIType();
      if (targetType == null) return;

      if (IsBoxingEliminatedAtRuntime(castExpression, data)) return;
      if (IsBoxingEliminatedAtRuntimeForCast(castExpression, targetType, data)) return;

      CheckConversionRequiresBoxing(
        sourceExpressionType, targetType, castExpression.TargetType, isExplicitCast: true, data, consumer);
    }

    private static void CheckDeclarationExpressionConversion(
      [NotNull] IDeclarationExpression declarationExpression, [NotNull] ElementProblemAnalyzerData data,
      [NotNull] IHighlightingConsumer consumer)
    {
      var declarationTypeUsage = declarationExpression.TypeUsage;
      if (declarationTypeUsage == null) return;

      // note: compiler do not emits boxing when component is discarded: (object _, var y) = t;
      if (!(declarationExpression.Designation is ISingleVariableDesignation)) return;

      var sourceExpressionType = TryGetInferredSourceExpressionType(declarationExpression);
      if (sourceExpressionType == null) return;

      var targetType = CSharpTypeFactory.CreateType(declarationTypeUsage);

      CheckConversionRequiresBoxing(
        sourceExpressionType, targetType, declarationTypeUsage, isExplicitCast: false, data, consumer);
    }

    private static void CheckDeconstructingAssignmentConversions(
      [NotNull] IAssignmentExpression assignmentExpression, [NotNull] ElementProblemAnalyzerData data,
      [NotNull] IHighlightingConsumer consumer)
    {
      var assignmentKind = assignmentExpression.GetAssignmentKind();
      if (assignmentKind != AssignmentKind.DeconstructingAssignment) return;

      var tupleExpression = assignmentExpression.Dest as ITupleExpression;
      if (tupleExpression == null) return;

      UniversalContext resolveContext = null;

      foreach (var tupleComponent in tupleExpression.ComponentsEnumerable)
      {
        var targetType = tupleComponent.Value?.GetExpressionType().ToIType();
        if (targetType == null) continue;

        resolveContext ??= new UniversalContext(tupleExpression);
        var sourceExpressionType = tupleExpression.GetComponentSourceExpressionType(tupleComponent, resolveContext);

        CheckConversionRequiresBoxing(
          sourceExpressionType, targetType, tupleComponent, isExplicitCast: false, data, consumer);
      }
    }

    private static void CheckPatternMatchingConversion(
      [NotNull] ITypeCheckPattern typeCheckPattern, [NotNull] IHighlightingConsumer consumer)
    {
      var typeCheckTypeUsage = typeCheckPattern.TypeUsage;
      if (typeCheckTypeUsage == null) return;

      var designation = typeCheckPattern.Designation;
      if (!(designation is ISingleVariableDesignation)
          && !(designation is IParenthesizedVariableDesignation)
          && !(typeCheckPattern is IRecursivePattern recursivePattern && recursivePattern.HasSubpatterns()))
      {
        return; // there is no need to have a variable with boxed value
      }

      var dispatchType = typeCheckPattern.GetDispatchType();
      var targetType = CSharpTypeFactory.CreateType(typeCheckTypeUsage);

      var classification = ClassifyBoxingInTypeCheckPattern(dispatchType, targetType);
      if (classification == BoxingClassification.Not) return;

      ReportBoxingAllocation(
        dispatchType, targetType, typeCheckTypeUsage,
        isDefinitelyBoxing: classification == BoxingClassification.Definitely,
        consumer);
    }

    [Pure]
    private static BoxingClassification ClassifyBoxingInTypeCheckPattern([NotNull] IType dispatchType, [NotNull] IType targetType)
    {
      var sourceClassification = dispatchType.Classify;
      if (sourceClassification == TypeClassification.REFERENCE_TYPE) return BoxingClassification.Not;

      if (!targetType.IsReferenceType()) return BoxingClassification.Not;

      if (targetType.IsObject()
          || targetType.IsSystemValueType()
          || targetType.IsSystemEnum()
          || targetType.IsInterfaceType())
      {
        return sourceClassification == TypeClassification.VALUE_TYPE
          ? BoxingClassification.Definitely
          : BoxingClassification.Possibly;
      }

      return BoxingClassification.Not;
    }

    private static void CheckConversionRequiresBoxing(
      [NotNull] IExpressionType sourceExpressionType, [NotNull] IType targetType,
      [NotNull] ITreeNode nodeToHighlight, bool isExplicitCast,
      [NotNull] ElementProblemAnalyzerData data, [NotNull] IHighlightingConsumer consumer)
    {
      // note: unfortunately, because of tuple conversions, we can't cut-off some types before full classification

      var conversionRule = data.GetTypeConversionRule();
      var conversion = isExplicitCast
        ? conversionRule.ClassifyConversionFromExpression(sourceExpressionType, targetType)
        : conversionRule.ClassifyImplicitConversionFromExpression(sourceExpressionType, targetType);

      var classification = CheckConversionRequiresBoxing(conversion, sourceExpressionType, targetType);
      if (classification == BoxingClassification.Not) return;

      ReportBoxingAllocation(
        sourceExpressionType, targetType, nodeToHighlight,
        isDefinitelyBoxing: classification == BoxingClassification.Definitely,
        consumer);
    }

    private static void ReportBoxingAllocation(
      [NotNull] IExpressionType sourceExpressionType, [NotNull] IType targetType,
      [NotNull] ITreeNode nodeToHighlight, bool isDefinitelyBoxing, [NotNull] IHighlightingConsumer consumer)
    {
      if (HeapAllocationAnalyzer.IsIgnoredContext(nodeToHighlight)) return;

      var range = nodeToHighlight is ICSharpExpression expression
        ? expression.GetExpressionRange()
        : nodeToHighlight.GetDocumentRange();

      if (isDefinitelyBoxing)
      {
        var description = BakeDescriptionWithTypes(
          "conversion from '{0}' to '{1}' requires boxing of value type", sourceExpressionType, targetType);

        consumer.AddHighlighting(new BoxingAllocationHighlighting(nodeToHighlight, description), range);
      }
      else
      {
        var description = BakeDescriptionWithTypes(
          "conversion from '{0}' to '{1}' possibly requires boxing of value type", sourceExpressionType, targetType);

        consumer.AddHighlighting(new PossibleBoxingAllocationHighlighting(nodeToHighlight, description), range);
      }
    }

    [Pure]
    private static BoxingClassification CheckConversionRequiresBoxing(
      Conversion conversion, [NotNull] IExpressionType sourceType, [NotNull] IType targetType)
    {
      if (conversion.Kind == ConversionKind.Boxing)
      {
        return RefineResult(sourceType, targetType);
      }

      if (conversion.Kind == ConversionKind.Unboxing)
      {
        return RefineUnboxingResult(sourceType.ToIType(), targetType);
      }

      var current = BoxingClassification.Not;

      foreach (var nestedInfo in conversion.GetNestedConversionsWithTypeInfo())
      {
        var newValue = CheckConversionRequiresBoxing(
          nestedInfo.Conversion, nestedInfo.SourceType, nestedInfo.TargetType);

        switch (newValue)
        {
          case BoxingClassification.Definitely:
            return BoxingClassification.Definitely;

          case BoxingClassification.Possibly when current == BoxingClassification.Not:
            current = newValue;
            break;
        }
      }

      return current;

      static BoxingClassification RefineResult(IExpressionType sourceType, IType targetType)
      {
        var type = sourceType.ToIType();
        if (type is IDeclaredType (ITypeParameter _, _) fromTypeParameterType)
        {
          Assertion.Assert(!fromTypeParameterType.IsReferenceType(), "!fromTypeParameterType.IsReferenceType()");

          if (targetType.IsTypeParameterType())
          {
            return fromTypeParameterType.IsValueType()
              ? BoxingClassification.Possibly
              : BoxingClassification.Not; // very unlikely
          }

          if (!fromTypeParameterType.IsValueType())
          {
            return BoxingClassification.Possibly;
          }
        }

        return BoxingClassification.Definitely;
      }

      static BoxingClassification RefineUnboxingResult(IType sourceType, IType targetType)
      {
        // yep, some "unboxing" conversions do actually cause boxing at runtime
        if (targetType.Classify == TypeClassification.REFERENCE_TYPE && sourceType != null)
        {
          return (sourceType.Classify == TypeClassification.VALUE_TYPE)
            ? BoxingClassification.Definitely
            : BoxingClassification.Possibly;
        }

        return BoxingClassification.Not;
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

    [CanBeNull]
    private static IExpressionType TryGetInferredSourceExpressionType([NotNull] IDeclarationExpression declarationExpression)
    {
      // (var a, _) = e; - 'var a' is typed by asking tuple's component source type
      var tupleComponent = TupleComponentNavigator.GetByValue(declarationExpression);
      if (tupleComponent != null)
      {
        var tupleExpression = TupleExpressionNavigator.GetByComponent(tupleComponent);
        Assertion.AssertNotNull(tupleExpression, "tupleExpression != null");

        return tupleExpression.GetComponentSourceExpressionType(tupleComponent, new UniversalContext(tupleComponent));
      }

      // foreach (var a in xs) - 'var a' is typed by 'xs' collection element type
      var foreachHeader = ForeachHeaderNavigator.GetByDeclarationExpression(declarationExpression);
      var foreachStatement = ForeachStatementNavigator.GetByForeachHeader(foreachHeader);
      var collection = foreachStatement?.Collection;
      if (collection != null)
      {
        var collectionType = collection.Type();

        var elementType = CollectionTypeUtil.ElementTypeByCollectionType(
          collectionType, foreachStatement, foreachStatement.IsAwait);
        if (elementType != null) return elementType;
      }

      return null;
    }

    [Pure]
    private static bool IsBoxingEliminatedAtRuntime([NotNull] ICSharpExpression expression, [NotNull] ElementProblemAnalyzerData data)
    {
      var containingParenthesized = expression.GetContainingParenthesizedExpression();

      // t != null, ReferenceEquals(t, null)
      var nullCheckData = NullCheckUtil.GetNullCheckByCheckedExpression(
        containingParenthesized, out _, allowUserDefinedAndUnresolvedChecks: false);
      if (nullCheckData != null)
      {
        switch (nullCheckData.Kind)
        {
          case NullCheckKind.EqualityExpression:
          case NullCheckKind.StaticReferenceEqualsNull:
          case NullCheckKind.NullPattern:
            return true; // optimized in all modern runtimes
        }
      }

      // C# 8.0 eliminates boxing in string concatenation
      var additiveExpression = AdditiveExpressionNavigator.GetByLeftOperand(containingParenthesized)
                               ?? AdditiveExpressionNavigator.GetByRightOperand(containingParenthesized);
      if (additiveExpression != null
          && data.IsCSharp8Supported()
          && additiveExpression.OperatorReference.IsStringConcatOperatorReference())
      {
        return true;
      }

      var assignmentExpression = AssignmentExpressionNavigator.GetBySource(containingParenthesized);
      if (assignmentExpression is { AssignmentType: AssignmentType.PLUSEQ }
          && data.IsCSharp8Supported()
          && assignmentExpression.OperatorReference.IsStringConcatOperatorReference())
      {
        return true;
      }

      return false;
    }

    [Pure]
    private static bool IsBoxingEliminatedAtRuntimeForCast(
      [NotNull] ICastExpression castExpression, [NotNull] IType targetType, [NotNull] ElementProblemAnalyzerData data)
    {
      var containingParenthesized = castExpression.GetContainingParenthesizedExpression();

      // if (typeof(T) == typeof(int) { var i = (int) (object) t; }
      var containingCastExpression = CastExpressionNavigator.GetByOp(containingParenthesized);
      if (containingCastExpression != null)
      {
        if (targetType.IsObject())
        {
          var unBoxingType = containingCastExpression.Type();
          switch (unBoxingType.Classify)
          {
            case TypeClassification.UNKNOWN:
            case TypeClassification.VALUE_TYPE:
              return true; // optimized in all modern runtimes
          }
        }
      }

      // ((I) s).P, ((I) s).M();
      var conditionalAccessExpression = ConditionalAccessExpressionNavigator.GetByQualifier(containingParenthesized);
      if (conditionalAccessExpression != null && targetType.IsInterfaceType())
      {
        var targetRuntime = data.GetTargetRuntime();
        if (targetRuntime == TargetRuntime.NetCore) return true;
      }

      return false;
    }
  }
}