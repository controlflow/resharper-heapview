#nullable enable
using System;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Conversions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Types;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.CSharp.Util.NullChecks;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Managed;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.HeapView.Highlightings;
using ReSharperPlugin.HeapView.Settings;

namespace ReSharperPlugin.HeapView.Analyzers;

// todo: [ReSharper] disable method group natural types under nameof() expression
// todo: [ReSharper] no implictly converted to 'object' under __arglist() expression

[ElementProblemAnalyzer(
  ElementTypes: new[]
  {
    typeof(ICSharpExpression),
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
      // structWithNoToString.ToString()
      case IInvocationExpression invocationExpression:
        CheckInheritedMethodInvocationOverValueType(invocationExpression, data, consumer);
        break;

      // Action a = structValue.InstanceMethod;
      case IReferenceExpression referenceExpression:
        CheckStructMethodConversionToDelegateInstance(referenceExpression, consumer);
        break;

      // var obj = (object) intValue;
      case ICastExpression castExpression:
        CheckExpressionExplicitConversion(castExpression, data, consumer);
        break;

      // foreach (object o in arrayOfInts) { }
      case IForeachStatement foreachStatement:
        CheckForeachImplicitConversions(foreachStatement, data, consumer);
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

  #region Struct inherited instance method invocation

  private static void CheckInheritedMethodInvocationOverValueType(
    IInvocationExpression invocationExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var invokedReferenceExpression = invocationExpression.InvokedExpression.GetOperandThroughParenthesis() as IReferenceExpression;
    if (invokedReferenceExpression == null) return;

    var (declaredElement, _, resolveErrorType) = invocationExpression.InvocationExpressionReference.Resolve();
    if (!resolveErrorType.IsAcceptable) return;

    var method = declaredElement as IMethod;
    if (method == null) return;

    if (method.IsStatic) return; // we are only insterested in instance methods

    var containingType = method.ContainingType;

    switch (method.ShortName)
    {
      case nameof(GetHashCode):
      case nameof(Equals):
      case nameof(ToString):
      {
        CheckValueTypeVirtualMethodInvocation();
        return;
      }

      case nameof(GetType) when containingType.IsSystemObject():
      {
        CheckGetTypeMethodInvocation();
        return;
      }
    }

    void CheckGetTypeMethodInvocation()
    {
      var qualifierType = TryGetQualifierExpressionType(invokedReferenceExpression);
      if (qualifierType == null)
        return;

      if (!qualifierType.IsTypeBoxable())
        return; // errorneous invocation

      if (invokedReferenceExpression.IsInTheContextWhereAllocationsAreNotImportant())
        return;

      if (data.AnalyzeCodeLikeIfOptimizationsAreEnabled())
        return; // only allocates in DEBUG builds

      if (qualifierType.IsValueType())
      {
        consumer.AddHighlighting(new BoxingAllocationHighlighting(
          invokedReferenceExpression.NameIdentifier,
          "special 'Object.GetType()' method invocation over the value type instance"));
      }
      else if (qualifierType.IsUnconstrainedGenericType(out var typeParameter))
      {
        consumer.AddHighlighting(new PossibleBoxingAllocationHighlighting(
          invokedReferenceExpression.NameIdentifier,
          "special 'Object.GetType()' method may be invoked over the value type instance "
          + $"if '{typeParameter.ShortName}' type parameter will be substituted with the value type"));
      }
    }

    void CheckValueTypeVirtualMethodInvocation()
    {
      bool mustCheckOverride;

      switch (containingType)
      {
        // we've found non-overriden Equals/GetHashCode/ToString invoked over something
        case IClass classType when classType.IsSystemValueTypeClass()
                                   || classType.IsSystemEnumClass()
                                   || classType.IsSystemObject():
        {
          mustCheckOverride = false;
          break;
        }

        // Nullable<T> overrides Equals/GetHashCode/ToString, but invokes the corresponding methods on T
        case IStruct structType when structType.IsNullableOfT():
        {
          mustCheckOverride = true;
          break;
        }

        default: return;
      }

      var qualifierType = TryGetQualifierExpressionType(invokedReferenceExpression).Unlift();
      if (qualifierType == null) return;

      if (!qualifierType.IsTypeBoxable())
        return; // errorneous invocation

      if (mustCheckOverride && CheckHasVirtualMethodOverride(qualifierType.GetTypeElement()))
        return;

      if (invokedReferenceExpression.IsInTheContextWhereAllocationsAreNotImportant())
        return;

      if (IsStructVirtualMethodInvocationOptimizedAtRuntime(method, qualifierType, data))
        return;

      if (qualifierType.IsTypeParameterType(out var typeParameter))
      {
        if (!qualifierType.IsReferenceType())
        {
          consumer.AddHighlighting(
            new PossibleBoxingAllocationHighlighting(
              invokedReferenceExpression.NameIdentifier,
              $"inherited 'Object.{method.ShortName}()' virtual method invocation over the value type instance "
              + $"if '{typeParameter.ShortName}' type parameter will be substituted with the value type "
              + $"that do not overrides '{method.ShortName}' virtual method"));
        }
      }
      else if (qualifierType.IsValueType())
      {
        consumer.AddHighlighting(
          new BoxingAllocationHighlighting(
            invokedReferenceExpression.NameIdentifier,
            $"inherited 'Object.{method.ShortName}()' virtual method invocation over the value type instance"));
      }

      [Pure]
      bool CheckHasVirtualMethodOverride(ITypeElement? typeElement)
      {
        switch (typeElement)
        {
          // Nullable<T> overrides won't help us to detect if the corresponding method in T
          // is overriden or not, so we have to do the override check manually
          case IStruct structType:
            return StructOverridesChecker.IsMethodOverridenInStruct(structType, method, data);
          case IEnum:
            return false; // enums do not have virtual method overrides
          case ITypeParameter:
            return false; // in generic code we are not assuming any overrides
          default:
            return true; // somewthing weird is found under Nullable<T>
        }
      }
    }
  }

  [Pure]
  private static bool IsStructVirtualMethodInvocationOptimizedAtRuntime(
    IMethod method, IType qualifierType, ElementProblemAnalyzerData data)
  {
    if (method.ShortName == nameof(GetHashCode)
        && qualifierType.IsEnumType()
        && data.GetTargetRuntime() == TargetRuntime.NetCore)
    {
      // .NET Core optimizes 'someEnum.GetHashCode()' at runtime
      return true;
    }

    return false;
  }

  #endregion
  #region Struct method group delegate creation

  private static void CheckStructMethodConversionToDelegateInstance(
    IReferenceExpression referenceExpression, IHighlightingConsumer consumer)
  {
    var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(referenceExpression.GetContainingParenthesizedExpression());
    if (invocationExpression != null) return;

    if (referenceExpression.IsNameofOperatorTopArgument()) return;

    var (declaredElement, _, resolveErrorType) = referenceExpression.Reference.Resolve();
    if (!resolveErrorType.IsAcceptable) return;

    var method = declaredElement as IMethod;
    if (method == null) return;

    if (method.IsStatic) return;

    var qualifierType = TryGetQualifierExpressionType(referenceExpression);
    if (qualifierType == null) return;

    if (qualifierType.IsReferenceType()) return;

    var delegateType = TryFindTargetDelegateType(referenceExpression);
    if (delegateType == null) return;

    if (referenceExpression.IsInTheContextWhereAllocationsAreNotImportant())
      return;

    var language = referenceExpression.Language;
    var sourceTypeText = qualifierType.GetPresentableName(language);
    var delegateTypeText = delegateType.GetPresentableName(language);

    if (qualifierType.IsUnconstrainedGenericType(out var typeParameter))
    {
      consumer.AddHighlighting(
        new PossibleBoxingAllocationHighlighting(
          referenceExpression.NameIdentifier,
          $"conversion of value type '{sourceTypeText}' instance method to '{delegateTypeText}' delegate type"
          + $" if '{typeParameter.ShortName}' type parameter will be substituted with the value type"));
    }
    else
    {
      consumer.AddHighlighting(
        new BoxingAllocationHighlighting(
          referenceExpression.NameIdentifier,
          $"conversion of value type '{sourceTypeText}' instance method to '{delegateTypeText}' delegate type"));
    }

    [Pure]
    static IType? TryFindTargetDelegateType(IReferenceExpression methodGroupExpression)
    {
      var targetType = methodGroupExpression.GetImplicitlyConvertedTo();
      if (targetType.IsDelegateType())
      {
        return targetType;
      }

      var naturalType = methodGroupExpression.GetExpressionType().ToIType();
      if (naturalType != null && naturalType.IsDelegateType())
      {
        return naturalType;
      }

      return null;
    }
  }

  [Pure]
  private static IType? TryGetQualifierExpressionType(IReferenceExpression referenceExpression)
  {
    var qualifierExpression = referenceExpression.QualifierExpression.GetOperandThroughParenthesis();
    if (qualifierExpression.IsThisOrBaseOrNull())
    {
      var typeDeclaration = referenceExpression.GetContainingTypeDeclaration();
      if (typeDeclaration is { DeclaredElement: IStruct structTypeElement })
      {
        return TypeFactory.CreateType(structTypeElement);
      }

      return null;
    }

    var expressionType = qualifierExpression.GetExpressionType();
    return expressionType.ToIType();
  }

  #endregion
  #region Explicit casts

  private static void CheckExpressionExplicitConversion(
    ICastExpression castExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var castOperand = castExpression.Op;
    if (castOperand == null) return;

    var sourceExpressionType = castOperand.GetExpressionType();

    var targetType = castExpression.GetExpressionType().ToIType();
    if (targetType == null) return;

    // todo: test this
    if (IsBoxingEliminatedAtRuntime(castExpression, data)) return;

    if (IsBoxingEliminatedByTheCompiler(castExpression, data)) return;

    // todo: test this
    if (IsBoxingEliminatedAtRuntimeForCast(castExpression, targetType, data)) return;

    CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, castExpression.TargetType,
      static (rule, source, target) => rule.ClassifyConversionFromExpression(source, target),
      data, consumer);
  }

  #endregion
  #region Implicit conversions in foreach

  private static void CheckForeachImplicitConversions(
    IForeachStatement foreachStatement, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
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
            sourceExpressionType: elementType, targetType: declarationExpression.Type(), explicitTypeUsage,
            static (conversionRule, source, target) => conversionRule.ClassifyImplicitConversionFromExpression(source, target),
            data, consumer);
        }

        break;
      }
    }
  }

  #endregion
  #region Implicit conversions in expressions

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
    // note: unfortunately, because of tuple conversions, we can't cut-off some types before full classification

    // todo: if source is reference type - can't be boxing?
    // todo: if target is value type and not ValueTuple - can't be boxing, right?

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

  #endregion
  #region Compiler optimizations

  [Pure]
  private static bool IsBoxingEliminatedByTheCompiler(ICSharpExpression boxedExpression, ElementProblemAnalyzerData data)
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

  #endregion
  #region Runtime optimizations

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

    return false;
  }

  // todo: not tested - boxing and immediate use
  [Pure]
  private static bool IsBoxingEliminatedAtRuntimeForCast(
    ICastExpression castExpression, IType targetType, ElementProblemAnalyzerData data)
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

  #endregion
}