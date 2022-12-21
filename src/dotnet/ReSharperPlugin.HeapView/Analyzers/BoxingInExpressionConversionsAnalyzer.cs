#nullable enable
using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Conversions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Types;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.CSharp.Util.NullChecks;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.HeapView.Highlightings;
using ReSharperPlugin.HeapView.Settings;

namespace ReSharperPlugin.HeapView.Analyzers;

// todo: default implementation boxing

// todo: [ReSharper] disable method group natural types under nameof() expression
// todo: [ReSharper] no implictly converted to 'object' under __arglist() expression

[ElementProblemAnalyzer(
  ElementTypes: new[]
  {
    typeof(ICSharpExpression)
  },
  HighlightingTypes = new[]
  {
    typeof(BoxingAllocationHighlighting),
    typeof(PossibleBoxingAllocationHighlighting)
  })]
public sealed class BoxingInExpressionConversionsAnalyzer : HeapAllocationAnalyzerBase<ICSharpExpression>
{
  protected override void Run(ICSharpExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    switch (expression)
    {
      // var obj = (object) intValue;
      case ICastExpression castExpression:
        CheckExpressionExplicitConversion(castExpression, data, consumer);
        break;

      case IParenthesizedExpression:
      case ICheckedExpression:
      case IUncheckedExpression:
      case ISuppressNullableWarningExpression:
        return; // do not analyze dummy conversion
    }

    CheckExpressionImplicitConversion(expression, data, consumer);
  }

  private static void CheckExpressionExplicitConversion(
    ICastExpression castExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var castOperand = castExpression.Op;
    if (castOperand == null) return;

    var sourceExpressionType = castOperand.GetExpressionType();

    var targetType = castExpression.GetExpressionType().ToIType();
    if (targetType == null) return;

    if (IsBoxingEliminatedAtRuntime(castExpression, data)) return;

    if (IsBoxingEliminatedByTheCompiler(castExpression, data)) return;

    if (IsBoxingEliminatedAtRuntimeForConstrainedOperationOverCast(castExpression, targetType, data)) return;

    CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, castExpression.TargetType,
      static (rule, source, target) => rule.ClassifyConversionFromExpression(source, target),
      data, consumer);
  }

  private static void CheckExpressionImplicitConversion(
    ICSharpExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (!IsImplicitValueConversionActuallyHappensInRuntime(expression)) return;

    var sourceExpressionType = expression.GetExpressionType();
    if (sourceExpressionType.IsUnknown) return;

    var targetType = expression.GetImplicitlyConvertedTo();
    if (targetType.IsUnknown) return;

    if (IsBoxingEliminatedAtRuntime(expression, data)) return;
    if (IsBoxingEliminatedByTheCompiler(expression, data)) return;

    CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, expression,
      static (rule, source, target) => rule.ClassifyImplicitConversionFromExpression(source, target),
      data, consumer);
  }

  public static void CheckConversionRequiresBoxing(
    IExpressionType sourceExpressionType, IType targetType, ITreeNode correspondingNode,
    [RequireStaticDelegate] Func<ICSharpTypeConversionRule, IExpressionType, IType, Conversion> getConversion,
    ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    // shortcut: do not check for boxing when source is a pointer already
    // note: boxing may be hidden in user-defined + tuple conversions
    if (sourceExpressionType is IType { Classify: TypeClassification.REFERENCE_TYPE } && targetType is not ITupleType)
      return;

    // shortcut: if target type is a value type other than ValueTuple - it can't be boxing
    // note: boxing may be hidden in user-defined + tuple conversions
    if (targetType.Classify == TypeClassification.VALUE_TYPE && targetType is not ITupleType && sourceExpressionType is not ITupleType)
      return;

    if (sourceExpressionType is IAnonymousFunctionType)
      return; // nothing to box and classifying a conversion might be expensive

    var conversionRule = data.GetTypeConversionRule();
    var conversion = getConversion(conversionRule, sourceExpressionType, targetType);

    var boxing = Boxing.TryFind(conversion, sourceExpressionType, targetType, correspondingNode);
    if (boxing == null) return;

    if (correspondingNode.IsInTheContextWhereAllocationsAreNotImportant()) return;

    boxing.Report(consumer);
  }

  [Pure]
  private static bool IsImplicitValueConversionActuallyHappensInRuntime(ICSharpExpression expression)
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
      return false; // filter out expressions under explicit casts
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

    var argument = CSharpArgumentNavigator.GetByValue(unwrappedExpression);
    if (argument != null)
    {
      // __arglist(42, true)
      if (ArglistExpressionNavigator.GetByArgument(argument) != null)
        return false;
    }

    // obj is 42
    // obj is > 42
    if (ConstantOrTypePatternNavigator.GetByExpression(unwrappedExpression) != null
        || RelationalPatternNavigator.GetByOperand(unwrappedExpression) != null)
    {
      return false;
    }

    return true;
  }

  [Pure]
  private static bool IsBoxingEliminatedByTheCompiler(ICSharpExpression boxedExpression, ElementProblemAnalyzerData data)
  {
    var containingParenthesized = boxedExpression.GetContainingParenthesizedExpression();

    if (data.IsCSharp8Supported())
    {
      // C# 8.0 eliminates boxing in string concatenation by invoking the .ToString() method
      // note: this works for all types, not only BCL ones (including unconstrained types)

      if (BinaryExpressionNavigator.GetByAnyOperand(containingParenthesized) is IAdditiveExpression additiveExpression
          && additiveExpression.OperatorReference.IsStringConcatOperator())
      {
        return true;
      }

      if (AssignmentExpressionNavigator.GetBySource(containingParenthesized) is { AssignmentType: AssignmentType.PLUSEQ } additiveAssignmentExpression
          && additiveAssignmentExpression.OperatorReference.IsStringConcatOperator())
      {
        return true;
      }
    }

    return false;
  }

  [Pure]
  private static bool IsBoxingEliminatedAtRuntime(ICSharpExpression expression, ElementProblemAnalyzerData data)
  {
    var containingParenthesized = expression.GetContainingParenthesizedExpression();

    var nullCheckData = NullCheckUtil.GetNullCheckByCheckedExpression(
      containingParenthesized, out _, allowUserDefinedAndUnresolvedChecks: false);
    if (nullCheckData != null)
    {
      switch (nullCheckData.Kind)
      {
        case NullCheckKind.EqualityExpression: // t != null
        case NullCheckKind.StaticReferenceEqualsNull: // ReferenceEquals(t, null)
        case NullCheckKind.NullPattern: // t is null
        {
          // T! boxing + null check is optimized in all runtimes
          if (data.AnalyzeCodeLikeIfOptimizationsAreEnabled())
            return true;

          break;
        }
      }
    }

    // enumValue.HasFlag(enumValue2) is optimized in .NET Core in Release builds
    var argument = CSharpArgumentNavigator.GetByValue(containingParenthesized);
    if (argument != null)
    {
      var invocationExpression = InvocationExpressionNavigator.GetByArgument(argument);
      if (invocationExpression != null)
      {
        if (invocationExpression.InvokedExpression.GetOperandThroughParenthesis()
              is IReferenceExpression { NameIdentifier.Name: { } methodName } invokedReferenceExpression)
        {
          switch (methodName)
          {
            case nameof(Enum.HasFlag) when IsOptimizedEnumHasFlagsInvocation():
              return true;
          }

          bool IsOptimizedEnumHasFlagsInvocation()
          {
            var invocationResolveResult = invocationExpression.Reference.Resolve();
            return invocationResolveResult.ResolveErrorType.IsAcceptable
                   && invocationResolveResult.DeclaredElement is IMethod method
                   && method.ContainingType.IsSystemEnumClass()
                   && invokedReferenceExpression.QualifierExpression is { } qualifierExpression
                   && BoxingInStructInvocationsAnalyzer.IsOptimizedEnumHasFlagsInvocation(
                        invocationExpression, qualifierExpression.Type(), data);
          }
        }
      }
    }



    return false;
  }

  [Pure]
  private static bool IsBoxingEliminatedAtRuntimeForConstrainedOperationOverCast(
    ICastExpression castExpression, IType targetType, ElementProblemAnalyzerData data)
  {
    var containingParenthesized = castExpression.GetContainingParenthesizedExpression();

    // if (typeof(T) == typeof(int)) { var i = (int) (object) t; }
    var containingCastExpression = CastExpressionNavigator.GetByOp(containingParenthesized);
    if (containingCastExpression != null)
    {
      if (targetType.IsObject())
      {
        var unBoxingType = containingCastExpression.Type();
        if (!unBoxingType.IsReferenceType())
        {
          return true; // optimized in all runtimes, even in Debug builds
        }
      }
    }

    // ((IFoo) s).Property
    // ((IFoo) s).Method();
    var conditionalAccessExpression = ConditionalAccessExpressionNavigator.GetByQualifier(containingParenthesized);
    if (conditionalAccessExpression != null && targetType.IsInterfaceType())
    {
      // todo: [BIG] will not be optimized for default members

      var targetRuntime = data.GetTargetRuntime();
      if (targetRuntime == TargetRuntime.NetCore)
      {
        if (data.AnalyzeCodeLikeIfOptimizationsAreEnabled())
        {
          return true; // optimized in .NET Core
        }
      }
    }

    return false;
  }
}