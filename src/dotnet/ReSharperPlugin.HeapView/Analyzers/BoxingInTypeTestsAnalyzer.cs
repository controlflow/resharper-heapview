#nullable enable
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[]
  {
    typeof(IPatternWithTypeUsage),
    typeof(IConstantOrTypePattern),
    typeof(IAsExpression)
  },
  HighlightingTypes = new[]
  {
    typeof(BoxingAllocationHighlighting),
    typeof(PossibleBoxingAllocationHighlighting)
  })]
public class BoxingInTypeTestsAnalyzer : HeapAllocationAnalyzerBase<ITreeNode>
{
  protected override void Run(ITreeNode element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    switch (element)
    {
      // structValue is I iface
      // structValue is I { Property: 42 }
      case IPatternWithTypeUsage { TypeUsage: { } typeUsage } typeTestPattern:
        CheckRuntimeTypeTestConversion(typeTestPattern, typeUsage, data, consumer);
        break;

      // tUnconstrained is int
      // tUnconstrained is I iface
      case IConstantOrTypePattern (ConstantOrTypePatternKind.TypeCheck) typePattern:
        CheckRuntimeTypeTestConversion(typePattern, typePattern.Expression, data, consumer);
        break;

      // structValue as I
      // tUnconstrained as I
      case IAsExpression asExpression:
        CheckRuntimeTypeTestConversion(asExpression, consumer);
        break;
    }
  }

  private static void CheckRuntimeTypeTestConversion(
    IPattern typeTestPattern, ITreeNode typeTestTypeUsage, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var sourceType = typeTestPattern.GetDispatchType();
    if (sourceType.IsUnknownOrUnresolvedTypeElementType()) return;

    var targetType = typeTestPattern.GetPatternType();
    if (targetType.IsUnknownOrUnresolvedTypeElementType()) return;

    // in .NET Framework type test alone can produce boxing allocations
    if (CheckTypeTestIntroducesBoxing(sourceType, data, out var isPossible))
    {
      if (typeTestPattern.IsInTheContextWhereAllocationsAreNotImportant()) return;

      var boxing = Boxing.Create(
        sourceType, targetType, typeTestTypeUsage, isPossible,
        messageFormat: "type testing '{0}' value for '{1}' type in .NET Framework projects");

      boxing.Report(consumer);
      return;
    }

    // x is T t
    // x is T { ... }
    if (IsTypeTestedAndBoxedValueAssignedToDesignationOrTemporaryVariable(typeTestPattern)
        && IsBoxingConversionInRuntimeTypeTest(sourceType, targetType, out isPossible))
    {
      if (typeTestPattern.IsInTheContextWhereAllocationsAreNotImportant()) return;

      var boxing = Boxing.Create(
        sourceType, targetType, typeTestTypeUsage, isPossible,
        messageFormat: "type testing '{0}' value for '{1}' type and using the result");

      boxing.Report(consumer);
    }
  }

  private static bool IsTypeTestedAndBoxedValueAssignedToDesignationOrTemporaryVariable(IPattern typeTestPattern)
  {
    // structValue is I i
    if (typeTestPattern is IPatternWithDesignation { Designation: ISingleVariableDesignation or IParenthesizedVariableDesignation })
    {
      // note: if designation exists but never used - C# eliminates boxing in Release builds. We ignore this
      return true;
    }

    // structValue is I { P: 42 }
    if (typeTestPattern is IRecursivePattern recursivePattern)
    {
      return recursivePattern.HasSubpatterns();
    }

    // structValue is I and { P: 42 }
    var containingParenthesizedPattern = typeTestPattern.GetContainingParenthesizedPattern();

    while (AndPatternNavigator.GetByRightPattern(containingParenthesizedPattern) is { } unwrappedByRight)
    {
      containingParenthesizedPattern = unwrappedByRight.GetContainingParenthesizedPattern();
    }

    var andPatternByRight = AndPatternNavigator.GetByLeftPattern(containingParenthesizedPattern);
    if (andPatternByRight != null)
    {
      return andPatternByRight.RightPattern != null;
    }

    return false;
  }

  private static void CheckRuntimeTypeTestConversion(IAsExpression asExpression, IHighlightingConsumer consumer)
  {
    var targetTypeUsage = asExpression.TypeOperand;
    if (targetTypeUsage == null) return;

    var sourceType = asExpression.Operand.GetExpressionType().ToIType();
    if (sourceType == null) return;

    if (sourceType.IsUnknownOrUnresolvedTypeElementType()) return;

    var targetType = asExpression.GetExpressionType().ToIType();
    if (targetType == null) return;

    if (targetType.IsUnknownOrUnresolvedTypeElementType()) return;

    if (IsBoxingConversionInRuntimeTypeTest(sourceType, targetType, out var isPossible))
    {
      if (asExpression.IsInTheContextWhereAllocationsAreNotImportant()) return;

      var boxing = Boxing.Create(
        sourceType, targetType, targetTypeUsage, isPossible,
        messageFormat: "type testing '{0}' value for '{1}' type and using the result");

      boxing.Report(consumer);
    }
  }

  [Pure]
  private static bool CheckTypeTestIntroducesBoxing(IType sourceType, ElementProblemAnalyzerData data, out bool isPossible)
  {
    isPossible = false;

    // .NET Framework x86/x64 JITs do not optimize the 'box !T + isinst' pattern in IL code
    // so type checks of unconstained type parameter types can produce boxings in runtime
    if (!sourceType.IsTypeParameterType()) return false;

    // it can't be boxing if source type parameter type is a reference type
    var sourceClassification = sourceType.Classify;
    if (sourceClassification == TypeClassification.REFERENCE_TYPE) return false;

    // .NET Core JIT optimizes all isinst type checks
    var runtime = data.GetTargetRuntime();
    if (runtime != TargetRuntime.NetFramework) return false;

    // note: this boxing detection is actually not dependent on the target type! all type checks really perform 'box !T'
    isPossible = sourceClassification != TypeClassification.VALUE_TYPE;
    return true;
  }

  [Pure]
  private static bool IsBoxingConversionInRuntimeTypeTest(IType sourceType, IType targetType, out bool isPossible)
  {
    isPossible = false;

    var sourceClassification = sourceType.Classify;
    if (sourceClassification == TypeClassification.REFERENCE_TYPE)
      return false;

    if (!targetType.IsReferenceType())
      return false;

    if (targetType.IsObject()
        || targetType.IsSystemValueType()
        || targetType.IsSystemEnum()
        || targetType.IsInterfaceType())
    {
      // tStruct is object o
      // tStruct is System.ValueType vt
      // tStruct is System.Enum e
      // tStruct is I i
      isPossible = sourceClassification != TypeClassification.VALUE_TYPE;
      return true;
    }

    // tUnconstrained is int x
    // tStruct is int x
    return false;
  }
}