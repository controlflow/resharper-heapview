#nullable enable
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.HeapView.Highlightings;
using ReSharperPlugin.HeapView.Settings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[]
  {
    typeof(IInvocationExpression),
    typeof(IReferenceExpression)
  },
  HighlightingTypes = new[]
  {
    typeof(BoxingAllocationHighlighting),
    typeof(PossibleBoxingAllocationHighlighting)
  })]
public class BoxingInStructInvocationsAnalyzer : ElementProblemAnalyzer<ICSharpExpression>
{
  protected override void Run(ICSharpExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    switch (expression)
    {
      // structWithNoToString.ToString()
      case IInvocationExpression invocationExpression:
        CheckInheritedMethodInvocationOverValueType(invocationExpression, data, consumer);
        break;

      // Action a = structValue.InstanceMethod;
      case IReferenceExpression referenceExpression:
        CheckStructMethodConversionToDelegateInstance(referenceExpression, consumer);
        break;
    }
  }

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
}