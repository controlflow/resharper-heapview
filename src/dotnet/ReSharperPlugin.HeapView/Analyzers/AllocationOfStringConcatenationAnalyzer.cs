using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.DataStructures;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

// TODO: 4-parameter overloads of string.Concat do exist

[ElementProblemAnalyzer(
  ElementTypes: [ typeof(IAssignmentExpression), typeof(IAdditiveExpression) ],
  HighlightingTypes = [
    typeof(ObjectAllocationHighlighting),
    typeof(BoxingAllocationHighlighting)
  ])]
public class AllocationOfStringConcatenationAnalyzer : HeapAllocationAnalyzerBase<IOperatorExpression>
{
  protected override void Run(
    IOperatorExpression operatorExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (!IsTopLevelStringInterpolation(operatorExpression))
      return;

    if (operatorExpression.IsInTheContextWhereAllocationsAreNotImportant())
      return;

    using var concatenationCompiler = myInspectorPool.Allocate();

    if (concatenationCompiler.TryInspect(operatorExpression))
    {
      concatenationCompiler.ReportAllocations(data, consumer);
    }
  }

  [Pure]
  private static bool IsTopLevelStringInterpolation(IOperatorExpression operatorExpression)
  {
    if (!operatorExpression.OperatorReference.IsStringConcatOperator()) return false;

    var containingExpression = operatorExpression.GetContainingParenthesizedExpression();
    var parentConcatenation = AdditiveExpressionNavigator.GetByLeftOperand(containingExpression)
                              ?? AdditiveExpressionNavigator.GetByRightOperand(containingExpression);
    if (parentConcatenation != null)
    {
      return !parentConcatenation.OperatorReference.IsStringConcatOperator();
    }

    var assignmentExpression = AssignmentExpressionNavigator.GetBySource(operatorExpression);
    if (assignmentExpression != null)
    {
      return !assignmentExpression.OperatorReference.IsStringConcatOperator();
    }

    return true;
  }

  private readonly ObjectPool<ConcatenationInspector> myInspectorPool = new(static _ => new ConcatenationInspector());

  private sealed class ConcatenationInspector : IDisposable
  {
    // expressions or constant markers
    private readonly List<object> myOperands = [];
    private ITokenNode? myCurrentConcatSign, myFirstConcatSign;

    private static readonly object ConstantPartMarker = new();

    public void Dispose()
    {
      myOperands.Clear();
      if (myOperands.Count > 100)
        myOperands.TrimExcess();

      myCurrentConcatSign = null;
      myFirstConcatSign = null;
    }

    public bool TryInspect(IOperatorExpression stringConcatenation)
    {
      var operands = stringConcatenation.OperatorOperands;
      if (operands.Count != 2) return false;

      myCurrentConcatSign = stringConcatenation.OperatorSign;
      if (!AppendOperandConvertToString(operands[0]))
        return false;

      myCurrentConcatSign = stringConcatenation.OperatorSign;
      if (!AppendOperandConvertToString(operands[1]))
        return false;

      return true;
    }

    private bool AppendOperandConvertToString(ICSharpExpression? expression)
    {
      if (expression == null) return false;

      var constantValue = expression.ConstantValue;
      if (!constantValue.IsErrorOrNonCompileTimeConstantValue())
      {
        if (constantValue.IsChar())
        {
          AppendConstPart();
          return true;
        }

        if (constantValue.IsString(out var stringValue))
        {
          if (!string.IsNullOrEmpty(stringValue))
            AppendConstPart();

          return true;
        }

        if (constantValue.IsPureNull())
        {
          return true;
        }
      }

      if (expression.GetOperandThroughParenthesis() is IAdditiveExpression additiveExpression
          && additiveExpression.OperatorReference.IsStringConcatOperator())
      {
        myCurrentConcatSign = additiveExpression.OperatorSign;
        if (!AppendOperandConvertToString(additiveExpression.LeftOperand))
          return false;

        myCurrentConcatSign = additiveExpression.OperatorSign;
        return AppendOperandConvertToString(additiveExpression.RightOperand);
      }

      myFirstConcatSign ??= myCurrentConcatSign;
      myOperands.Add(expression);
      return true;

      void AppendConstPart()
      {
        if (myOperands.Count > 0 && myOperands[^1] == ConstantPartMarker)
          return;

        myOperands.Add(ConstantPartMarker);
      }
    }

    public void ReportAllocations(ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      string? allocationDescription;

      switch (myOperands.Count)
      {
        // optimized into "" return
        case 0: return;
        // optimized into `foo.ToString()`/`foo?.ToString()`/`foo.ToString() ?? ""`
        case 1: allocationDescription = null; break;
        case 2: allocationDescription = "string concatenation"; break;
        case 3: allocationDescription = "string concatenation (3 operands)"; break;
        case var count: allocationDescription = $"string concatenation ({count} operands, allocates parameter array)"; break;
      }

      if (allocationDescription != null && myFirstConcatSign != null)
      {
        consumer.AddHighlighting(
          new ObjectAllocationHighlighting(myFirstConcatSign, allocationDescription));
      }

      foreach (var operand in myOperands)
      {
        // C# >= 8.0 started to invoke .ToString() methods before passing args to string.Concat() invocation
        if (operand is ICSharpExpression operandExpression && operandExpression.IsCSharp8Supported())
        {
          ReportOperandAllocations(operandExpression, data, consumer);
        }
      }
    }

    private static void ReportOperandAllocations(
      ICSharpExpression operandExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      var operandType = operandExpression.Type();

      if (IsTypeKnownToAllocateOnToStringCall(operandType))
      {
        var typeName = operandType.GetPresentableName(operandExpression.Language, CommonUtils.DefaultTypePresentationStyle);
        consumer.AddHighlighting(
          new ObjectAllocationHighlighting(operandExpression, $"implicit 'ToString' invocation over '{typeName}' value"));
      }
      else if (operandType is IDeclaredType (IStruct structType))
      {
        if (!StructOverridesChecker.IsMethodOverridenInStruct(structType, nameof(ToString), data))
        {
          consumer.AddHighlighting(
            new BoxingAllocationHighlighting(
              operandExpression, "inherited 'ValueType.ToString' virtual method invocation over the value type instance"));
        }
      }
    }
  }

  [Pure]
  private static bool IsTypeKnownToAllocateOnToStringCall(IType type)
  {
    if (type is IDeclaredType (not null) declaredType)
    {
      if (declaredType.IsPredefinedNumericOrNativeNumeric())
      {
        return true;
      }

      // todo: what about other well-known compiled types?
      // todo: add StringBuilder?
    }

    return false;
  }
}