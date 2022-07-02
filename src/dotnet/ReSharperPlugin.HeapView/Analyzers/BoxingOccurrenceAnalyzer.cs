using System;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.CSharp.Util.NullChecks;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Managed;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

// todo: extension deconstruction method invocation boxing
// todo: extension GetEnumerator() boxing, extension .Add() for collection initializers
// todo: if designation exists, but not used - C# eliminates boxing in Release mode
// todo: boxing elimiation by the compiler under string concatenation
// todo: do string interpolation optimized? in C# 10 only?
// todo: C# 8 causes boxing by invoimg the non-overriden .ToString under string concatenation?

[ElementProblemAnalyzer(
  ElementTypes: new[]
  {
    typeof(ICSharpExpression),
    typeof(IPatternWithTypeUsage),
    typeof(IForeachStatement)
  },
  HighlightingTypes = new[]
  {
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

      // var obj = (object) intValue;
      case ICastExpression castExpression:
        CheckExpressionExplicitConversion(castExpression, data, consumer);
        break;

      // foreach (object o in arrayOfInts) { }
      // foreach ((object o, _) in arrayOfIntIntTuples) { }
      case IForeachStatement foreachStatement:
        CheckForeachImplicitConversions(foreachStatement, data, consumer);
        break;

      // (object o, objVariable) = intIntTuple;
      case IAssignmentExpression assignmentExpression:
        CheckDeconstructingAssignmentImplicitConversions(assignmentExpression, data, consumer);
        break;

      case IPatternWithTypeUsage typeCheckPattern:
        CheckPatternMatchingConversion(typeCheckPattern, data, consumer);
        break;

      case IIsExpression isExpression:
        CheckTypeCheckBoxing(isExpression, data, consumer);
        break;

      case IParenthesizedExpression:
      case ICheckedExpression:
      case IUncheckedExpression:
      case ISuppressNullableWarningExpression:
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

    var methodClassType = method.ContainingType as IClass;
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

  private enum BoxingClassification
  {
    Definitely,
    Possibly,
    Not
  }

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
    [NotNull] ICSharpExpression expression,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    if (!IsImplicitConversionActuallyHappens(expression)) return;

    var sourceExpressionType = expression.GetExpressionType();
    if (sourceExpressionType.IsUnknown) return;

    var targetType = expression.GetImplicitlyConvertedTo();
    if (targetType.IsUnknown) return;

    if (IsBoxingEliminatedAtRuntime(expression)) return;
    if (IsBoxingEliminatedByTheCompiler(expression, data)) return;

    CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, expression, isExplicitCast: false, data, consumer);
  }

  [Pure]
  private static bool IsImplicitConversionActuallyHappens([NotNull] ICSharpExpression expression)
  {
    switch (expression)
    {
      // (int a, int b) = t; - here the tuple is not actually a tuple construction, it's in LValue position
      case ITupleExpression tupleExpression when tupleExpression.IsLValueTupleExpression():
      // is not a subject for implicit conversions for now
      case IDeclarationExpression:
      case IRefExpression:
      {
        return false;
      }
    }

    var unwrappedExpression = expression.GetContainingParenthesizedExpression();

    var castExpression = CastExpressionNavigator.GetByOp(unwrappedExpression);
    if (castExpression != null)
    {
      return false; // filter out explicit casts
    }

    var tupleComponent = TupleComponentNavigator.GetByValue(unwrappedExpression);
    if (tupleComponent != null)
    {
      return false; // check the whole tuple expression conversion instead
    }

    var assignmentExpression = AssignmentExpressionNavigator.GetBySource(unwrappedExpression);
    if (assignmentExpression != null)
    {
      var assignmentKind = assignmentExpression.GetAssignmentKind();
      if (assignmentKind != AssignmentKind.OrdinaryAssignment)
      {
        // tuple deconstrutions do not have a "target type" for the assignment source,
        // so we have to handle conversions in deconstructions separately (ad-hoc)
        return false;
      }
    }

    return true;
  }

  private static void CheckExpressionExplicitConversion(
    [NotNull] ICastExpression castExpression,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    var castOperand = castExpression.Op;

    var sourceExpressionType = castOperand?.GetExpressionType();
    if (sourceExpressionType == null) return;

    var targetType = castExpression.GetExpressionType().ToIType();
    if (targetType == null) return;

    if (IsBoxingEliminatedAtRuntime(castExpression)) return;
    if (IsBoxingEliminatedByTheCompiler(castExpression, data)) return;
    if (IsBoxingEliminatedAtRuntimeForCast(castExpression, targetType, data)) return;

    CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, castExpression.TargetType, isExplicitCast: true, data, consumer);
  }

  private static void CheckDeconstructingAssignmentImplicitConversions(
    [NotNull] IAssignmentExpression assignmentExpression,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    switch (assignmentExpression.GetAssignmentKind())
    {
      case AssignmentKind.OrdinaryAssignment:
        return;

      // all kinds of deconstructions
      case AssignmentKind.DeconstructingAssignment:
      case AssignmentKind.DeconstructingDeclaration:
      case AssignmentKind.DeconstructionMixed:
        break;

      default:
        throw new ArgumentOutOfRangeException();
    }

    var targetTupleExpression = assignmentExpression.Dest as ITupleExpression;
    if (targetTupleExpression == null) return;

    UniversalContext resolveContext = null;
    CheckImplicitConversionsInDeconstruction(targetTupleExpression, ref resolveContext, data, consumer);
  }

  private static void CheckForeachImplicitConversions(
    [NotNull] IForeachStatement foreachStatement,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    var foreachHeader = foreachStatement.ForeachHeader;
    if (foreachHeader == null) return;

    switch (foreachStatement.ForeachHeader)
    {
      // foreach (object o in xs) { }
      case {
             DeclarationExpression: { TypeUsage: { } explicitTypeUsage, Designation: ISingleVariableDesignation } declarationExpression,
             Collection: { } collection
           }:
      {
        var collectionType = collection.Type();

        var elementType = CollectionTypeUtil.ElementTypeByCollectionType(collectionType, foreachStatement, foreachStatement.IsAwait);
        if (elementType != null)
        {
          CheckConversionRequiresBoxing(
            elementType, declarationExpression.Type(), explicitTypeUsage, isExplicitCast: false, data, consumer);
        }

        break;
      }

      // foreach ((object o, _) in xs) { }
      case { DeconstructionTuple: { } targetTupleExpression }:
      {
        UniversalContext resolveContext = null;
        CheckImplicitConversionsInDeconstruction(targetTupleExpression, ref resolveContext, data, consumer);
        break;
      }
    }
  }

  private static void CheckImplicitConversionsInDeconstruction(
    [NotNull] ITupleExpression targetTupleExpression,
    [CanBeNull] ref UniversalContext universalContext,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    foreach (var tupleComponent in targetTupleExpression.ComponentsEnumerable)
    {
      switch (tupleComponent.Value)
      {
        // ((a, b), _) = e;
        case ITupleExpression innerTupleExpression:
        {
          CheckImplicitConversionsInDeconstruction(innerTupleExpression, ref universalContext, data, consumer);
          break;
        }

        // (_, _) = e;           - discards elimiate access to component
        // (object _, _) = e;    - discard designations elimiate access as well
        // (var a, _) = e;       - source type captured, no conversion
        // (var (a, b), _) = e;  - source type deconstructed, no conversion
        case IReferenceExpression discardReferenceExpression when discardReferenceExpression.IsDiscardReferenceExpression():
        case IDeclarationExpression { Designation: IDiscardDesignation }:
        case IDeclarationExpression { TypeUsage: null }:
        {
          break;
        }

        // (a, _) = e;
        // (object o, _) = e;
        case { IsLValue: true } lValueExpression:
        {
          var targetComponentType = lValueExpression.GetExpressionType().ToIType();
          if (targetComponentType == null) continue;

          universalContext ??= new UniversalContext(targetTupleExpression);

          var sourceExpressionType = targetTupleExpression.GetComponentSourceExpressionType(tupleComponent, universalContext);

          ITreeNode correspondingNode = lValueExpression is IDeclarationExpression declarationExpression
            ? declarationExpression.TypeUsage.NotNull()
            : lValueExpression;

          CheckConversionRequiresBoxing(
            sourceExpressionType, targetComponentType, correspondingNode, isExplicitCast: false, data, consumer);
          break;
        }
      }
    }
  }

  private static void CheckPatternMatchingConversion(
    [NotNull] IPatternWithTypeUsage typeCheckPattern,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    var typeCheckTypeUsage = typeCheckPattern.TypeUsage;
    if (typeCheckTypeUsage == null) return;

    var dispatchType = typeCheckPattern.GetDispatchType();
    var targetType = CSharpTypeFactory.CreateType(typeCheckTypeUsage);

    var classification = CanTypeCheckIntroduceBoxing(dispatchType, targetType, data);
    if (classification == BoxingClassification.Not)
    {
      if (IsVariableOrTemporaryForBoxedValueRequired())
      {
        classification = ClassifyBoxingInTypeCheckPattern(dispatchType, targetType);
      }
    }

    ReportBoxingAllocation(
      dispatchType, targetType, typeCheckTypeUsage, classification, consumer,
      action: "type testing '{0}' value for '{1}' type");

    bool IsVariableOrTemporaryForBoxedValueRequired()
    {
      switch (typeCheckPattern.Designation)
      {
        case ISingleVariableDesignation:
        case IParenthesizedVariableDesignation:
          return true;
      }

      if (typeCheckPattern is IRecursivePattern recursivePattern)
      {
        return recursivePattern.HasSubpatterns();
      }

      return false;
    }
  }

  private static void CheckTypeCheckBoxing(
    [NotNull] IIsExpression isExpression,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    var isExpressionKind = isExpression.GetKind(unresolvedIsTypeCheck: false);
    if (isExpressionKind != IsExpressionKind.TypeCheck) return;

    var dispatchType = isExpression.Operand?.GetExpressionType().ToIType();
    if (dispatchType == null) return;

    var targetType = isExpression.IsType;

    var typeCheckTypeUsageNode = isExpression.GetTypeCheckTypeUsageNode();
    if (typeCheckTypeUsageNode == null) return;

    var classification = CanTypeCheckIntroduceBoxing(dispatchType, targetType, data);

    ReportBoxingAllocation(
      dispatchType, targetType, typeCheckTypeUsageNode, classification, consumer,
      action: "type testing '{0}' value for '{1}' type");
  }

  [Pure]
  private static BoxingClassification CanTypeCheckIntroduceBoxing(
    [NotNull] IType dispatchType, [NotNull] IType targetType, [NotNull] ElementProblemAnalyzerData data)
  {
    // only in generic code, statically known type checks are optimized by C# compiler
    if (!dispatchType.IsTypeParameterType()) return BoxingClassification.Not;

    // .NET Framework only
    var runtime = data.GetTargetRuntime();
    if (runtime != TargetRuntime.NetFramework) return BoxingClassification.Not;

    if (targetType.IsValueType()
        || targetType.IsInterfaceType()
        // unconstrainedT is System.ValueType
        || (targetType.IsSystemValueType() && dispatchType.Classify == TypeClassification.UNKNOWN))
    {
      return dispatchType.Classify == TypeClassification.VALUE_TYPE
        ? BoxingClassification.Definitely
        : BoxingClassification.Possibly;
    }

    return BoxingClassification.Not;
  }

  [Pure]
  private static BoxingClassification ClassifyBoxingInTypeCheckPattern(
    [NotNull] IType dispatchType, [NotNull] IType targetType)
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
    [NotNull] IExpressionType sourceExpressionType,
    [NotNull] IType targetType,
    [NotNull] ITreeNode correspondingNode,
    bool isExplicitCast,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    // note: unfortunately, because of tuple conversions, we can't cut-off some types before full classification

    // todo: if source is reference type - can't be boxing?
    // todo: if target is value type and not ValueTuple - can't be boxing, right?

    var conversionRule = data.GetTypeConversionRule();
    var conversion = isExplicitCast
      ? conversionRule.ClassifyConversionFromExpression(sourceExpressionType, targetType)
      : conversionRule.ClassifyImplicitConversionFromExpression(sourceExpressionType, targetType);

    var boxing = Boxing.TryFind(conversion, sourceExpressionType, targetType, correspondingNode);
    if (boxing != null)
    {
      if (!correspondingNode.IsInTheContextWhereAllocationsAreNotImportant())
      {
        boxing.Report(consumer);
      }
    }
  }

  private static void ReportBoxingAllocation(
    [NotNull] IExpressionType sourceExpressionType,
    [NotNull] IType targetType,
    [NotNull] ITreeNode nodeToHighlight,
    BoxingClassification boxingClassification,
    [NotNull] IHighlightingConsumer consumer,
    string action = "conversion from '{0}' to '{1}'")
  {
    if (boxingClassification == BoxingClassification.Not) return;

    if (nodeToHighlight.IsInTheContextWhereAllocationsAreNotImportant()) return;

    var range = nodeToHighlight is ICSharpExpression expression
      ? expression.GetExpressionRange()
      : nodeToHighlight.GetDocumentRange();

    if (boxingClassification == BoxingClassification.Definitely)
    {
      var description = BakeDescriptionWithTypes(
        action + " requires boxing of the value type", sourceExpressionType, targetType);

      consumer.AddHighlighting(new BoxingAllocationHighlighting(nodeToHighlight, description), range);
    }
    else
    {
      var description = BakeDescriptionWithTypes(
        action + " possibly requires boxing of the value type", sourceExpressionType, targetType);

      consumer.AddHighlighting(new PossibleBoxingAllocationHighlighting(nodeToHighlight, description), range);
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

  [Pure]
  private static bool IsBoxingEliminatedByTheCompiler([NotNull] ICSharpExpression boxedExpression, [NotNull] ElementProblemAnalyzerData data)
  {
    var containingParenthesized = boxedExpression.GetContainingParenthesizedExpression();

    if (data.IsCSharp8Supported())
    {
      // C# 8.0 eliminates boxing in string concatenation by invoking the .ToString() method
      // note: this works for all types, not only BCL ones (including unconstrained types)

      if (BinaryExpressionNavigator.GetByAnyOperand(containingParenthesized) is IAdditiveExpression additiveExpression
          && additiveExpression.OperatorReference.IsStringConcatOperatorReference())
      {
        return true;
      }

      if (AssignmentExpressionNavigator.GetBySource(containingParenthesized) is { AssignmentType: AssignmentType.PLUSEQ } additiveAssignmentExpression
          && additiveAssignmentExpression.OperatorReference.IsStringConcatOperatorReference())
      {
        return true;
      }
    }

    return false;
  }

  [Pure]
  private static bool IsBoxingEliminatedAtRuntime([NotNull] ICSharpExpression expression)
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