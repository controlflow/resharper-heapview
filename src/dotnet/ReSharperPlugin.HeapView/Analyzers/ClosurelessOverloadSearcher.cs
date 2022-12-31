using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util.DataStructures.Collections;

namespace ReSharperPlugin.HeapView.Analyzers;

public static class ClosurelessOverloadSearcher
{
  [Pure]
  public static IReference? FindMethodInvocationByArgument(ICSharpExpression expression)
  {
    var containingExpression = expression.GetContainingParenthesizedExpressionStrict();
    var argument = CSharpArgumentNavigator.GetByValue(containingExpression);

    var invocationExpression = InvocationExpressionNavigator.GetByArgument(argument);
    if (invocationExpression == null) return null;

    var invokedReference = invocationExpression.InvokedExpression.GetOperandThroughParenthesis() as IReferenceExpression;
    if (invokedReference == null) return null;

    var invocationReference = invocationExpression.InvocationExpressionReference;

    var (declaredElement, _, resolveErrorType) = invocationReference.Resolve();
    if (resolveErrorType != ResolveErrorType.OK) return null;
    if (declaredElement is not IMethod) return null;

    return invokedReference.Reference;
  }

  [Pure]
  public static IParameter? FindClosureParameter(ICSharpExpression expression)
  {
    var containingExpression = expression.GetContainingParenthesizedExpressionStrict();

    var argument = CSharpArgumentNavigator.GetByValue(containingExpression);
    if (argument is
        {
          MatchingParameter:
          {
            Expanded: ArgumentsUtil.ExpandedKind.None,
            Element: { Type: IDeclaredType (IDelegate) } parameter
          }
        })
    {
      return parameter;
    }

    return null;
  }

  [Pure]
  public static IMethod? FindOverloadByParameter(IParameter parameter)
  {
    if (parameter.ContainingParametersOwner is not IMethod { ContainingType: { } containingType } invokedMethod)
      return null;

    var invokedName = invokedMethod.ShortName;
    var invokedIsStatic = invokedMethod.IsStatic;
    var invokedReturnKind = invokedMethod.ReturnKind;
    var invokedAccessRights = invokedMethod.GetAccessRights();

    foreach (var typeMember in containingType.GetMembers())
    {
      if (typeMember is IMethod candidate
          && !ReferenceEquals(candidate, invokedMethod)
          && candidate.ShortName == invokedName
          && candidate.IsStatic == invokedIsStatic
          && candidate.ReturnKind == invokedReturnKind
          && candidate.IsExtensionMethod == invokedMethod.IsExtensionMethod
          && candidate.GetAccessRights() == invokedAccessRights)
      {
        if (CompareSignatures(invokedMethod, candidate, parameter))
        {
          return candidate;
        }
      }
    }

    return null;
  }

  private static bool CompareSignatures(IMethod invokedMethod, IMethod candidate, IParameter closureParameter)
  {
    var invokedParameters = invokedMethod.Parameters;
    var candidateParameters = candidate.Parameters;
    if (invokedParameters.Count + 1 != candidateParameters.Count)
      return false; // one more formal parameter expected

    var invokedTypeParameters = invokedMethod.TypeParameters;
    var candidateTypeParameters = candidate.TypeParameters;
    if (invokedTypeParameters.Count + 1 != candidateTypeParameters.Count)
      return false; // one more type parameter expected

    foreach (var (stateTypeParameterCandidate, equalitySubstitution) in EnumeratePossibleTStateTypeParameters(invokedTypeParameters, candidateTypeParameters))
    {
      // compare signatures

      bool seenStateParameter = false; // note: not needed?
      bool seenDelegateParameter = false;

      for (int invokedIndex = 0, candidateIndex = 0; candidateIndex < candidateParameters.Count;)
      {
        var candidateParameter = candidateParameters[candidateIndex];
        var candidateParameterType = candidateParameter.Type;

        if (invokedIndex == invokedParameters.Count)
        {
          if (IsValidParameterForTState(candidateParameter, candidateParameterType, stateTypeParameterCandidate))
          {
            seenStateParameter = true;
          }

          Assertion.Assert(candidateIndex == candidateParameters.Count - 1);
          break;
        }

        Assertion.Assert(invokedIndex < invokedParameters.Count);
        var invokedParameter = invokedParameters[invokedIndex];
        var invokedParameterType = equalitySubstitution[invokedParameter.Type];

        if (!TypeEqualityComparer.DefaultWithTupleNamesAndNullabilityAndNativeIntegerMismatch.Equals(invokedParameterType, candidateParameterType))
        {
          if (IsValidParameterForTState(candidateParameter, candidateParameterType, stateTypeParameterCandidate))
          {
            seenStateParameter = true;
            candidateIndex++;
            continue;
          }

          if (ReferenceEquals(invokedParameter, closureParameter))
          {
            if (CheckCandidateDelegateToHaveExtraStateParameterPassed(
                  invokedParameterType, candidateParameterType, stateTypeParameterCandidate, equalitySubstitution))
            {
              seenDelegateParameter = true;
              invokedIndex++;
              candidateIndex++;
              continue;
            }
          }

          // may be delegate parameter
          // may be TState parameter - skip

          seenStateParameter = false; // todo: replace w return
          break; // fail
        }

        if (!MatchParameterKinds(invokedParameter, candidateParameter))
        {
          seenStateParameter = false; // todo: replace w return
          break;
        }

        invokedIndex++;
        candidateIndex++;
      }

      if (seenStateParameter && seenDelegateParameter)
        return true;
    }

    return false;

    [Pure]
    static bool CheckCandidateDelegateToHaveExtraStateParameterPassed(
      IType invokedType, IType candidateType, ITypeParameter stateTypeParameterCandidate, ISubstitution equalitySubstitution)
    {
      if (invokedType is not IDeclaredType (IDelegate invokedDelegate, var invokedSubstitution)) return false;
      if (candidateType is not IDeclaredType (IDelegate candidateDelegate, var candidateSubstitution)) return false;

      var delegateInvokeMethod = invokedDelegate.InvokeMethod;
      var candidateInvokeMethod = candidateDelegate.InvokeMethod;

      if (delegateInvokeMethod.ReturnKind != candidateInvokeMethod.ReturnKind) return false;

      if (!TypeEqualityComparer.DefaultWithTupleNamesAndNullabilityAndNativeIntegerMismatch.Equals(
            candidateSubstitution[candidateInvokeMethod.ReturnType],
            equalitySubstitution[invokedSubstitution[delegateInvokeMethod.ReturnType]])) return false;

      var invokedParameters = delegateInvokeMethod.Parameters;
      var candidateParameters = candidateInvokeMethod.Parameters;
      if (invokedParameters.Count + 1 != candidateParameters.Count) return false;

      // `Func<A, T>` in non-TState overload +
      // `Func<A, TState, T>` in TState overload
      var seenExtraStateParameter = false;

      for (int invokedIndex = 0, candidateIndex = 0; candidateIndex < candidateParameters.Count;)
      {
        var candidateParameter = candidateParameters[candidateIndex];
        var candidateParameterType = candidateSubstitution[candidateParameter.Type];

        if (invokedIndex == invokedParameters.Count)
        {
          if (IsValidParameterForTState(candidateParameter, candidateParameterType, stateTypeParameterCandidate))
          {
            seenExtraStateParameter = true;
          }

          Assertion.Assert(candidateIndex == candidateParameters.Count - 1);
          break;
        }

        Assertion.Assert(invokedIndex < invokedParameters.Count);
        var invokedParameter = invokedParameters[invokedIndex];
        var invokedParameterType = equalitySubstitution[invokedSubstitution[invokedParameter.Type]];

        if (!TypeEqualityComparer.DefaultWithTupleNamesAndNullabilityAndNativeIntegerMismatch.Equals(invokedParameterType, candidateParameterType))
        {
          if (IsValidParameterForTState(candidateParameter, candidateParameterType, stateTypeParameterCandidate))
          {
            seenExtraStateParameter = true;
            candidateIndex++;
            continue;
          }

          return false;
        }

        if (!MatchParameterKinds(invokedParameter, candidateParameter)) return false;

        invokedIndex++;
        candidateIndex++;
      }

      return seenExtraStateParameter;
    }
  }

  private static IEnumerable<(ITypeParameter StateTypeParameter, ISubstitution EqualitySubstitution)> EnumeratePossibleTStateTypeParameters(
    IList<ITypeParameter> invokedTypeParameters, IList<ITypeParameter> candidateTypeParameters)
  {
    using var substitution = PooledDictionary<ITypeParameter, IType>.GetInstance();

    for (var candidateIndex = 0; candidateIndex < candidateTypeParameters.Count; candidateIndex++)
    {
      var candidateTypeParameter = candidateTypeParameters[candidateIndex];
      if (candidateTypeParameter is { IsReferenceType: false, IsUnmanagedType: false, IsValueType: false, HasDefaultConstructor: false, OwnerFunction: IMethod })
      {
        substitution.Clear();

        for (var invokedIndex = 0; invokedIndex < invokedTypeParameters.Count; invokedIndex++)
        {
          var correspondingIndex = invokedIndex + (invokedIndex < candidateIndex ? 0 : 1);
          substitution[invokedTypeParameters[invokedIndex]] = TypeFactory.CreateType(candidateTypeParameters[correspondingIndex]);
        }

        yield return (candidateTypeParameter, EmptySubstitution.INSTANCE.Extend(substitution));
      }
    }
  }

  [Pure]
  private static bool MatchParameterKinds(IParameter parameter, IParameter candidateParameter)
  {
    if (parameter.Kind != candidateParameter.Kind) return false;
    if (parameter.IsParameterArray != candidateParameter.IsParameterArray) return false;

    if (parameter.IsOptional)
    {
      if (!candidateParameter.IsOptional) return false;

      var currentDefaultValue = parameter.GetDefaultValue();
      var candidateDefaultValue = candidateParameter.GetDefaultValue();
      if (!currentDefaultValue.Equals(candidateDefaultValue)) return false;
    }

    return true;
  }

  [Pure]
  private static bool IsValidParameterForTState(IParameter parameter, IType parameterType, ITypeParameter stateParameterCandidate)
  {
    if (!parameterType.IsTypeParameterType(out var candidateTypeParameter)) return false;
    if (!candidateTypeParameter.Equals(stateParameterCandidate)) return false;

    if (parameter.IsParameterArray) return false;
    if (parameter.Kind == ParameterKind.OUTPUT) return false;
    if (parameter.IsOptional) return false;

    return true;
  }
}