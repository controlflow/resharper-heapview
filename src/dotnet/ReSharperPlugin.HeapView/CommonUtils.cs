#nullable enable
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace ReSharperPlugin.HeapView;

public static class CommonUtils
{
  public static readonly TypePresentationStyle DefaultTypePresentationStyle = TypePresentationStyle.Default with
  {
    Options = TypePresentationStyle.Default.Options & ~TypePresentationOptions.IncludeNullableAnnotations
  };

  [Pure]
  public static bool IsStringConcatOperator(this IReference? reference)
  {
    if (reference is (ISignOperator { IsPredefined: true } signOperator, _) && signOperator.ReturnType.IsString())
    {
      var predefined = CSharpPredefined.GetInstance(reference.GetTreeNode());
      return signOperator.Equals(predefined.BinaryPlusObjectString)
             || signOperator.Equals(predefined.BinaryPlusStringString)
             || signOperator.Equals(predefined.BinaryPlusStringObject);
    }

    return false;
  }

  [Pure]
  public static bool IsInTheContextWhereAllocationsAreNotImportant(this ITreeNode context)
  {
    if (context.IsUnderLinqExpressionTree())
      return true; // not a "real" code

    foreach (var containingNode in context.ContainingNodes(returnThis: true))
    {
      switch (containingNode)
      {
        case IAttribute: // compile-time boxing, array creations
        case IThrowExpression or IThrowStatement: // throw arguments
          return true;

        case ICSharpStatement statement:
          return NextStatementsExecutionAlwaysEndsWithThrowStatement(statement);
      }
    }

    return false;
  }

  [Pure]
  private static bool NextStatementsExecutionAlwaysEndsWithThrowStatement(ICSharpStatement statement)
  {
    var nextStatement = statement.GetNextStatement(skipPreprocessor: false);

    while (nextStatement != null)
    {
      if (nextStatement is IThrowStatement) return true;

      if (HasControlFlowJumps(nextStatement)) return false;

      nextStatement = nextStatement.GetNextStatement(skipPreprocessor: false);
    }

    // todo: support "next" statement by unwrapping
    /*
     * if (...) {
     *    ...boxing...
     * }
     *
     * throw ...;
     */
    return false;

    [SuppressMessage("ReSharper", "TailRecursiveCall")]
    static bool HasControlFlowJumps(ICSharpStatement? statement, bool allowContinue = false, bool allowBreak = false)
    {
      switch (statement)
      {
        case IBlock block:
        {
          foreach (var blockStatement in block.StatementsEnumerable)
          {
            if (HasControlFlowJumps(blockStatement, allowContinue, allowBreak))
              return true;
          }

          return false;
        }

        case ICheckedStatement checkedStatement:
        {
          return HasControlFlowJumps(checkedStatement.Body, allowContinue, allowBreak);
        }

        case IUncheckedStatement uncheckedStatement:
        {
          return HasControlFlowJumps(uncheckedStatement.Body, allowContinue, allowBreak);
        }

        case ILoopStatement loopStatement:
        {
          return HasControlFlowJumps(loopStatement.Body, allowContinue: true, allowBreak: true);
        }

        case IIfStatement ifStatement:
        {
          return HasControlFlowJumps(ifStatement.Then, allowContinue, allowBreak)
                 || HasControlFlowJumps(ifStatement.Else, allowContinue, allowBreak);
        }

        case ILockStatement lockStatement:
        {
          return HasControlFlowJumps(lockStatement.Body, allowContinue, allowBreak);
        }

        case ISwitchStatement switchStatement:
        {
          foreach (var switchSection in switchStatement.SectionsEnumerable)
          foreach (var switchSectionStatement in switchSection.StatementsEnumerable)
          {
            if (HasControlFlowJumps(switchSectionStatement, allowContinue, allowBreak: true))
              return true;

            // todo: why not recursive call
            //if (IsControlFlowJumpStatement(switchSectionStatement, allowContinue, allowBreak: true))
            //  return true;
          }

          return false;
        }

        case ITryStatement tryStatement:
        {
          if (HasControlFlowJumps(tryStatement.Try, allowContinue, allowBreak)) return true;

          foreach (var catchClause in tryStatement.CatchesEnumerable)
          {
            if (HasControlFlowJumps(catchClause.Body, allowContinue, allowBreak))
              return true;
          }

          return HasControlFlowJumps(tryStatement.FinallyBlock, allowContinue, allowBreak);
        }

        case IUnsafeCodeFixedStatement fixedStatement:
        {
          return HasControlFlowJumps(fixedStatement.Body, allowContinue, allowBreak);
        }

        case IUnsafeCodeUnsafeStatement unsafeStatement:
        {
          return HasControlFlowJumps(unsafeStatement.Body, allowContinue, allowBreak);
        }

        case IUsingStatement usingStatement:
        {
          return HasControlFlowJumps(usingStatement.Body, allowContinue, allowBreak);
        }

        case { }:
        {
          return IsControlFlowJumpStatement(statement, allowContinue, allowBreak);
        }

        case null:
        {
          return false;
        }
      }
    }

    static bool IsControlFlowJumpStatement(ICSharpStatement statement, bool allowContinue, bool allowBreak)
    {
      switch (statement)
      {
        case IBreakStatement when !allowBreak:
        case IContinueStatement when !allowContinue:
        case IGotoCaseStatement:
        case IGotoStatement:
        case ILoopStatement:
        case IReturnStatement:
        case IYieldStatement:
          return true;

        default:
          return false;
      }
    }
  }

  [Pure]
  public static bool IsTypeParameterType(this IType type, [NotNullWhen(returnValue: true)] out ITypeParameter? typeParameter)
  {
    if (type is IDeclaredType declaredType)
    {
      typeParameter = declaredType.GetTypeElement() as ITypeParameter;
      return typeParameter != null;
    }

    typeParameter = null;
    return false;
  }

  [Pure]
  public static bool IsUnconstrainedGenericType(
    this IType type, [NotNullWhen(returnValue: true)] out ITypeParameter? typeParameter)
  {
    if (type is IDeclaredType { Classify: TypeClassification.UNKNOWN } declaredType)
    {
      typeParameter = declaredType.GetTypeElement() as ITypeParameter;
      return typeParameter != null;
    }

    typeParameter = null;
    return false;
  }

  [Pure]
  public static IType? TryFindTargetDelegateType(this IReferenceExpression methodGroupExpression)
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