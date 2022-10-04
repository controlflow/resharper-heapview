using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Conversions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Types;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.CSharp.Util.NullChecks;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Managed;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve.ExtensionMethods;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using ReSharperPlugin.HeapView.Highlightings;
using ReSharperPlugin.HeapView.Settings;

namespace ReSharperPlugin.HeapView.Analyzers;

// todo: if designation exists, but not used - C# eliminates boxing in Release mode
// todo: do string interpolation optimized? in C# 10 only?

// todo: [ReSharper] disable method group natural types under nameof() expression
// todo: [ReSharper] no implictly converted to 'object' under __arglist() expression

[ElementProblemAnalyzer(
  ElementTypes: new[]
  {
    typeof(ICSharpExpression),
    typeof(IPatternWithTypeUsage),
    typeof(IForeachStatement),
    typeof(IDeconstructionPatternClause),
    typeof(IVarDeconstructionPattern),
    typeof(ICollectionElementInitializer),
    typeof(IQueryCastReferenceProvider)
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
      // intArray.Cast<object>();
      case IInvocationExpression invocationExpression:
        CheckInheritedMethodInvocationOverValueType(invocationExpression, data, consumer);
        CheckLinqEnumerableCastConversion(invocationExpression, data, consumer);
        break;

      // Action a = structValue.InstanceMethod;
      case IReferenceExpression referenceExpression:
        CheckStructMethodConversionToDelegateInstance(referenceExpression, consumer);
        break;

      // var obj = (object) intValue;
      case ICastExpression castExpression:
        CheckExpressionExplicitConversion(castExpression, data, consumer);
        break;

      // is StructType (_, _) + extension Deconstruct() this parameter boxing
      case IDeconstructionPatternClause deconstructionPatternClause:
        CheckExtensionDeconstructionInvocation(deconstructionPatternClause, data, consumer);
        break;

      // is var (_, _) + extension Deconstruct() this parameter boxing
      case IVarDeconstructionPattern varDeconstructionPattern:
        CheckExtensionDeconstructionInvocation(varDeconstructionPattern, data, consumer);
        break;

      // var (_, _) = e; + extension Deconstruct() this parameter boxing
      case IDeclarationExpression declarationExpression:
        CheckExtensionDeconstructionInvocation(declarationExpression, data, consumer);
        break;

      // (_, _) = e; + extension Deconstruct() this parameter boxing
      case ITupleExpression tupleExpression:
        CheckExtensionDeconstructionInvocation(tupleExpression, data, consumer);
        break;

      // new T { e } + extension Add() this parameter boxing
      case ICollectionElementInitializer collectionElementInitializer:
        CheckExtensionCollectionAddInvocation(collectionElementInitializer, data, consumer);
        break;

      // foreach (object o in arrayOfInts) { }
      // foreach ((object o, _) in arrayOfIntIntTuples) { }
      // foreach (var x in structTypeWithExtensionGetEnumerator) { }
      case IForeachStatement foreachStatement:
        CheckForeachImplicitConversions(foreachStatement, data, consumer);
        CheckExtensionGetEnumeratorInvocation(foreachStatement, data, consumer);
        break;

      // await e + extension GetAwaiter() method
      case IAwaitExpression awaitExpression:
        CheckExtensionGetAwaiterInvocation(awaitExpression, data, consumer);
        break;

      // (object o, objVariable) = intIntTuple;
      case IAssignmentExpression assignmentExpression:
        CheckDeconstructingAssignmentImplicitConversions(assignmentExpression, data, consumer);
        break;

      // from object o in intArray
      case IQueryCastReferenceProvider queryCastReferenceProvider:
        CheckLinqQueryCastConversion(queryCastReferenceProvider, data, consumer);
        break;

      //
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

  #region Struct inherited instance method invocation

  private static void CheckInheritedMethodInvocationOverValueType(
    [NotNull] IInvocationExpression invocationExpression,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
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
      bool CheckHasVirtualMethodOverride([CanBeNull] ITypeElement typeElement)
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
            return true; // corresponding method is overriden, no boxing inside Nullable<T>
        }
      }
    }
  }

  [Pure]
  private static bool IsStructVirtualMethodInvocationOptimizedAtRuntime(
    [NotNull] IMethod method, [NotNull] IType qualifierType, [NotNull] ElementProblemAnalyzerData data)
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
    [NotNull] IReferenceExpression referenceExpression,
    [NotNull] IHighlightingConsumer consumer)
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

    [CanBeNull, Pure]
    static IType TryFindTargetDelegateType([NotNull] IReferenceExpression methodGroupExpression)
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

  [CanBeNull, Pure]
  private static IType TryGetQualifierExpressionType([NotNull] IReferenceExpression referenceExpression)
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
    [NotNull] ICastExpression castExpression,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    var castOperand = castExpression.Op;
    if (castOperand == null) return;

    var sourceExpressionType = castOperand.GetExpressionType();

    var targetType = castExpression.GetExpressionType().ToIType();
    if (targetType == null) return;

    // todo: test this
    if (IsBoxingEliminatedAtRuntime(castExpression)) return;

    if (IsBoxingEliminatedByTheCompiler(castExpression, data)) return;

    // todo: test this
    if (IsBoxingEliminatedAtRuntimeForCast(castExpression, targetType, data)) return;

    CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, castExpression.TargetType,
      static (rule, source, target) => rule.ClassifyConversionFromExpression(source, target),
      data, consumer);
  }

  #endregion
  #region Extension method invocations

  private static void CheckExtensionDeconstructionInvocation(
    [NotNull] IDeconstructionPatternClause deconstructionPatternClause,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    var recursivePattern = RecursivePatternNavigator.GetByDeconstructionPatternClause(deconstructionPatternClause);
    if (recursivePattern == null) return;

    var targetType = FindExtensionMethodWithReferenceTypeThisParameter(deconstructionPatternClause.DeconstructionReference);
    if (targetType == null) return;

    var sourceExpressionType = recursivePattern.GetSourceExpressionType(new UniversalContext(recursivePattern));

    CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, deconstructionPatternClause,
      static (rule, source, target) => rule.ClassifyImplicitExtensionMethodThisArgumentConversion(source, target),
      data, consumer);
  }

  private static void CheckExtensionDeconstructionInvocation(
    [NotNull] IVarDeconstructionPattern varDeconstructionPattern,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    var designation = varDeconstructionPattern.Designation;
    if (designation == null) return;

    var targetType = FindExtensionMethodWithReferenceTypeThisParameter(designation.DeconstructionReference);
    if (targetType == null) return;

    var dispatchType = varDeconstructionPattern.GetDispatchType();

    CheckConversionRequiresBoxing(
      dispatchType, targetType, varDeconstructionPattern.VarKeyword,
      static (rule, source, target) => rule.ClassifyImplicitExtensionMethodThisArgumentConversion(source, target),
      data, consumer);
  }

  private static void CheckExtensionDeconstructionInvocation(
    [NotNull] IDeclarationExpression declarationExpression,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    var designation = declarationExpression.Designation as IParenthesizedVariableDesignation;
    if (designation == null) return;

    var targetType = FindExtensionMethodWithReferenceTypeThisParameter(designation.DeconstructionReference);
    if (targetType == null) return;

    var sourceExpressionType = declarationExpression.GetSourceExpressionType(new UniversalContext(declarationExpression));

    CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, declarationExpression.TypeDesignator,
      static (rule, source, target) => rule.ClassifyImplicitExtensionMethodThisArgumentConversion(source, target),
      data, consumer);
  }

  private static void CheckExtensionDeconstructionInvocation(
    [NotNull] ITupleExpression tupleExpression,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    if (!tupleExpression.IsLValueTupleExpression()) return;

    var targetType = FindExtensionMethodWithReferenceTypeThisParameter(tupleExpression.DeconstructionReference);
    if (targetType == null) return;

    var dispatchType = tupleExpression.GetSourceExpressionType(new UniversalContext(tupleExpression));

    CheckConversionRequiresBoxing(
      dispatchType, targetType, tupleExpression,
      static (rule, source, target) => rule.ClassifyImplicitExtensionMethodThisArgumentConversion(source, target),
      data, consumer);
  }

  private static void CheckExtensionCollectionAddInvocation(
    [NotNull] ICollectionElementInitializer collectionElementInitializer,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    var collectionInitializer = CollectionInitializerNavigator.GetByElementInitializer(collectionElementInitializer);
    if (collectionInitializer == null) return;

    var invocationReference = collectionElementInitializer.Reference;
    if (invocationReference == null) return;

    var targetType = FindExtensionMethodWithReferenceTypeThisParameter(invocationReference);
    if (targetType == null) return;

    var sourceExpressionType = collectionInitializer.GetConstructedType();

    CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, collectionElementInitializer,
      static (rule, source, target) => rule.ClassifyImplicitExtensionMethodThisArgumentConversion(source, target),
      data, consumer);
  }

  private static void CheckExtensionGetEnumeratorInvocation(
    [NotNull] IForeachStatement foreachStatement,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    var foreachHeader = foreachStatement.ForeachHeader;
    if (foreachHeader == null) return;

    var collection = foreachHeader.Collection;
    if (collection == null) return;

    var targetType = FindExtensionMethodWithReferenceTypeThisParameter(foreachStatement.GetEnumeratorReference);
    if (targetType == null) return;

    var sourceExpressionType = collection.Type();

    CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, foreachHeader.InKeyword,
      static (rule, source, target) => rule.ClassifyImplicitExtensionMethodThisArgumentConversion(source, target),
      data, consumer);
  }

  private static void CheckExtensionGetAwaiterInvocation(
    [NotNull] IAwaitExpression awaitExpression,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    var taskExpression = awaitExpression.Task;
    if (taskExpression == null) return;

    var getAwaiterReference = awaitExpression.GetAwaiterReference;
    if (getAwaiterReference == null) return;

    var targetType = FindExtensionMethodWithReferenceTypeThisParameter(getAwaiterReference);
    if (targetType == null) return;

    var sourceExpressionType = taskExpression.Type();

    CheckConversionRequiresBoxing(
      sourceExpressionType, targetType, awaitExpression.AwaitKeyword,
      static (rule, source, target) => rule.ClassifyImplicitExtensionMethodThisArgumentConversion(source, target),
      data, consumer);
  }

  [CanBeNull, Pure]
  private static IType FindExtensionMethodWithReferenceTypeThisParameter([NotNull] IReference deconstructionReference)
  {
    var resolveResult = deconstructionReference.Resolve();
    if (resolveResult.ResolveErrorType.IsAcceptable
        && resolveResult.Result.IsExtensionMethodInvocation()
        && resolveResult.DeclaredElement is IMethod { IsExtensionMethod: true } extensionsMethod)
    {
      foreach (var parameter in extensionsMethod.Parameters)
      {
        return resolveResult.Substitution[parameter.Type];
      }
    }

    return null;
  }

  #endregion
  #region Implicit conversions in deconstructions

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
            sourceExpressionType: elementType, targetType: declarationExpression.Type(), explicitTypeUsage,
            static (conversionRule, source, target) => conversionRule.ClassifyImplicitConversionFromExpression(source, target),
            data, consumer);
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
            sourceExpressionType, targetComponentType, correspondingNode,
            static (conversionRule, source, target) => conversionRule.ClassifyImplicitConversionFromExpression(source, target),
            data, consumer);
          break;
        }
      }
    }
  }

  #endregion
  #region Casts in LINQ queries

  private static void CheckLinqQueryCastConversion(
    [NotNull] IQueryCastReferenceProvider queryCastReferenceProvider,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    var castReference = queryCastReferenceProvider.CastReference;
    if (castReference == null) return;

    var collection = queryCastReferenceProvider.Expression;
    if (collection == null) return;

    var targetTypeUsage = GetCastNode(queryCastReferenceProvider);
    if (targetTypeUsage == null) return;

    var resolveResult = castReference.Resolve();
    if (!resolveResult.ResolveErrorType.IsAcceptable) return;

    if (resolveResult.DeclaredElement is not IMethod { ShortName: nameof(Enumerable.Cast) } castMethod) return;

    var targetTypeParameter = castMethod.TypeParameters.SingleItem();
    if (targetTypeParameter == null) return;

    var castTargetType = resolveResult.Substitution[targetTypeParameter];

    var sourceType = TryGetEnumerableCollectionType(collection.Type());
    if (sourceType == null) return;

    CheckConversionRequiresBoxing(
      sourceType, castTargetType, targetTypeUsage,
      static (rule, source, target) => rule.ClassifyConversionFromExpression(source, target),
      data, consumer);

    [CanBeNull]
    static ITypeUsage GetCastNode(IQueryCastReferenceProvider queryCastReferenceProvider)
    {
      return queryCastReferenceProvider switch
      {
        IQueryFirstFrom queryFirstFrom => queryFirstFrom.TypeUsage,
        IQueryFromClause queryFromClause => queryFromClause.TypeUsage,
        IQueryJoinClause queryJoinClause => queryJoinClause.TypeUsage,
        _ => throw new ArgumentOutOfRangeException(nameof(queryCastReferenceProvider))
      };
    }
  }

  [NotNull] private static readonly ClrTypeName SystemEnumerableClassTypeName = new("System.Linq.Enumerable");

  private static void CheckLinqEnumerableCastConversion(
    [NotNull] IInvocationExpression invocationExpression,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
  {
    if (invocationExpression.InvokedExpression is not IReferenceExpression invokedReferenceExpression) return;

    var typeArgumentList = invokedReferenceExpression.TypeArgumentList;
    if (typeArgumentList == null) return;

    if (!typeArgumentList.CommasEnumerable.IsEmpty()) return; // single type argument expected

    var resolveResult = invocationExpression.Reference.Resolve();
    if (!resolveResult.ResolveErrorType.IsAcceptable) return;

    if (resolveResult.DeclaredElement is not IMethod
        {
          ShortName: nameof(Enumerable.Cast),
          ContainingType: { ShortName: nameof(Enumerable) } containingType
        } castMethod)
    {
      return;
    }

    if (!containingType.GetClrName().Equals(SystemEnumerableClassTypeName)) return;

    var collectionExpression = resolveResult.Result.IsExtensionMethodInvocation()
      ? invokedReferenceExpression.QualifierExpression
      : invocationExpression.ArgumentsEnumerable.SingleItem?.Value;

    if (collectionExpression == null) return;

    var targetTypeUsage = typeArgumentList.TypeArgumentNodes.SingleItem();
    if (targetTypeUsage == null) return;

    var targetTypeParameter = castMethod.TypeParameters.SingleItem();
    if (targetTypeParameter == null) return;

    var castTargetType = resolveResult.Substitution[targetTypeParameter];

    var sourceType = TryGetEnumerableCollectionType(collectionExpression.Type());
    if (sourceType == null) return;

    CheckConversionRequiresBoxing(
      sourceType, castTargetType, targetTypeUsage,
      static (rule, source, target) => rule.ClassifyConversionFromExpression(source, target),
      data, consumer);
  }

  [CanBeNull, Pure]
  private static IType TryGetEnumerableCollectionType([NotNull] IType sourceType)
  {
    switch (sourceType)
    {
      case IArrayType arrayType:
      {
        return arrayType.ElementType;
      }

      case IDeclaredType ({ } typeElement, var substitution):
      {
        var predefinedType = sourceType.Module.GetPredefinedType();

        var iEnumerable = predefinedType.GenericIEnumerable.GetTypeElement();
        if (iEnumerable is { TypeParameters: { Count: 1 } typeParameters })
        {
          var singleImplementation = typeElement.GetAncestorSubstitution(iEnumerable).SingleItem;
          if (singleImplementation != null)
          {
            return substitution.Apply(singleImplementation).Apply(typeParameters[0]);
          }
        }

        break;
      }
    }

    return null;
  }

  #endregion
  #region Pattern-matching conversions

  // ..

  #endregion
  #region Implicit conversions in expressions

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
      sourceExpressionType, targetType, expression,
      static (rule, source, target) => rule.ClassifyImplicitConversionFromExpression(source, target),
      data, consumer);
  }

  private static void CheckConversionRequiresBoxing(
    [NotNull] IExpressionType sourceExpressionType,
    [NotNull] IType targetType,
    [NotNull] ITreeNode correspondingNode,
    [NotNull, RequireStaticDelegate] Func<ICSharpTypeConversionRule, IExpressionType, IType, Conversion> getConversion,
    [NotNull] ElementProblemAnalyzerData data,
    [NotNull] IHighlightingConsumer consumer)
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

  private enum BoxingClassification
  {
    Definitely,
    Possibly,
    Not
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



    //Boxing.TryFind(, )

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

      // structValue is I and { P: 42 }
      var containingParenthesizedPattern = typeCheckPattern.GetContainingParenthesizedPattern();

      while (BinaryPatternNavigator.GetByRightPattern(containingParenthesizedPattern) is { } unwrappedRight)
      {
        containingParenthesizedPattern = unwrappedRight.GetContainingParenthesizedPattern();
      }

      var binaryPattern = BinaryPatternNavigator.GetByLeftPattern(containingParenthesizedPattern);
      if (binaryPattern != null)
      {
        return binaryPattern.RightPattern != null;
      }

      // _ part of and pattern
      // _ part of or pattern?

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
    // todo: JIT emits the specialization at runtime, source type is unknown for us?
    if (!dispatchType.IsTypeParameterType()) return BoxingClassification.Not;

    // .NET Framework only
    var runtime = data.GetTargetRuntime();
    if (runtime != TargetRuntime.NetFramework) return BoxingClassification.Not;

    if (targetType.IsValueType() // todo: int
        || targetType.IsInterfaceType()
        // unconstrainedT is System.ValueType
        || (targetType.IsSystemValueType() && dispatchType.Classify == TypeClassification.UNKNOWN))
    {
      if (dispatchType.Classify == TypeClassification.VALUE_TYPE)
        return BoxingClassification.Definitely;



      return BoxingClassification.Possibly;
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

  #region Compiler optimizations

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

  #endregion
  #region Runtime optimizations

  // todo: not tested
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

  // todo: not tested
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

  #endregion
}