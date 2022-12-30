using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;

namespace ReSharperPlugin.HeapView.Analyzers;

// todo: support M(Key, Func<T>) + M(TKey, Func<TKey, T>) where TKey : Key;

public static class ClosurelessOverloadSearcher
{
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

  public static IParameter? FindClosureParameter(ICSharpExpression expression)
  {
    var containingExpression = expression.GetContainingParenthesizedExpressionStrict();
    var argument = CSharpArgumentNavigator.GetByValue(containingExpression);

    var parameterInstance = argument?.MatchingParameter;
    if (parameterInstance == null) return null;

    if (parameterInstance.Expanded != ArgumentsUtil.ExpandedKind.None) return null;

    var parameterDeclaredType = parameterInstance.Type as IDeclaredType;

    var delegateType = parameterDeclaredType?.GetTypeElement() as IDelegate;
    if (delegateType == null) return null;

    return parameterInstance.Element;
  }

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
        if (
          CompareSignatures2(invokedMethod, candidate, parameter) ||
          CompareSignatures(invokedMethod, candidate, parameter))
        {
          return candidate;
        }
      }
    }

    return null;
  }

  private static bool CompareSignatures2(IMethod invokedMethod, IMethod candidate, IParameter closureParameter)
  {
    var invokedParameters = invokedMethod.Parameters;
    var candidateParameters = candidate.Parameters;
    if (invokedParameters.Count + 1 != candidateParameters.Count)
      return false; // one more formal parameter expected

    var invokedTypeParameters = invokedMethod.TypeParameters;
    var candidateTypeParameters = candidate.TypeParameters;
    if (invokedTypeParameters.Count + 1 != candidateTypeParameters.Count)
      return false; // one more type parameter expected

    foreach (var (stateTypeParameter, equalitySubstitution) in EnumeratePossibleTStateTypeParameters(invokedTypeParameters, candidateTypeParameters))
    {
      // compare signatures


    }

    return false;
  }

  private static IEnumerable<(ITypeParameter StateTypeParameter, ISubstitution EqualitySubstitution)> EnumeratePossibleTStateTypeParameters(
    IList<ITypeParameter> invokedTypeParameters, IList<ITypeParameter> candidateTypeParameters)
  {
    using var substitution = PooledDictionary<ITypeParameter, IType>.GetInstance();

    for (var candidateIndex = 0; candidateIndex < candidateTypeParameters.Count; candidateIndex++)
    {
      var candidateTypeParameter = candidateTypeParameters[candidateIndex];
      if (candidateTypeParameter is { IsReferenceType: false, IsUnmanagedType: false, IsValueType: false, HasDefaultConstructor: false })
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

  private static bool CompareSignatures(IMethod currentMethod, IMethod closurelessCandidate, IParameter closureParameter)
  {
    // we expect extra formal parameters to pass state
    var parameters = currentMethod.Parameters;
    var candidateParameters = closurelessCandidate.Parameters;
    if (parameters.Count >= candidateParameters.Count) return false;

    var typeParameters = currentMethod.TypeParameters;
    var candidateTypeParameters = closurelessCandidate.TypeParameters;

    // we expect extra type parameters for state parameter
    if (candidateTypeParameters.Count > typeParameters.Count)
    {
      foreach (var (stateTypeParameters, candidateSubstitution) in EnumeratePossibleTStateCandidates(typeParameters, candidateTypeParameters))
      {
        // ([M2::T1], {M1::T1 -> M2::T2, M1::T2 -> M2::TState}),
        // ([M2::T2], {M1::T1 -> M2::T1, M1::T2 -> M2::TState}),
        // ([M3::TState], {M1::T1 -> M2::T1, M1::T2 -> M2::T2})
        HashSet<ITypeParameter>? stateParametersToVisit = null;

        int currentIndex = 0, candidateIndex = 0;
        while (candidateIndex < candidateParameters.Count)
        {
          var candidateParameter = candidateParameters[candidateIndex];

          if (currentIndex < parameters.Count)
          {
            var currentParameter = parameters[currentIndex];
            if (CompareParameterKinds(currentParameter, candidateParameter))
            {
              var parameterType = currentParameter.Type;
              var candidateParameterType = candidateParameter.Type;

              var isClosureParameter = ReferenceEquals(currentParameter, closureParameter);
              if (isClosureParameter
                    ? CompareDelegateTypes(parameterType, candidateParameterType, stateTypeParameters, candidateSubstitution)
                    : candidateSubstitution.Apply(parameterType).Equals(candidateParameterType))
              {
                currentIndex++;
                candidateIndex++;
                continue;
              }
            }
          }

          var stateTypeParameter = TryFindStateTypeParameter(candidateParameter, candidateParameter.IdSubstitution);
          if (stateTypeParameter != null)
          {
            stateParametersToVisit ??= new HashSet<ITypeParameter>(stateTypeParameters);

            if (stateParametersToVisit.Remove(stateTypeParameter))
            {
              candidateIndex++;
              continue;
            }
          }

          stateParametersToVisit = null;
          break;
        }

        if (stateParametersToVisit is { Count: 0 }) return true;
      }

      return false;
    }

    // look for 'object state'
    if (candidateTypeParameters.Count == typeParameters.Count)
    {
      // todo: the same as before, but without substitution and different state parameter check
    }

    return false;
  }

  private static ITypeParameter? TryFindStateTypeParameter(IParameter parameter, ISubstitution substitution)
  {
    if (parameter is { Kind: ParameterKind.VALUE, IsOptional: false, IsParameterArray: false })
    {
      var parameterType = substitution[parameter.Type];
      if (parameterType is IDeclaredType (ITypeParameter { OwnerFunction: { }, HasDefaultConstructor: false } typeParameter, _))
      {
        return typeParameter;
      }
    }

    return null;
  }

  private static bool CompareDelegateTypes(
    IType closureParameterType, IType closureCandidateParameterType,
    IList<ITypeParameter> stateTypeParameters, ISubstitution typeParametersMapping)
  {
    // Func<int, M1::T1, List<M1::T2>, string>
    var closureDeclaredType = closureParameterType as IDeclaredType;
    if (closureDeclaredType == null) return false;

    var closureDelegateType = closureDeclaredType.GetTypeElement() as IDelegate;
    if (closureDelegateType == null) return false;

    // Func<int, M2::T1, List<M2::T2>, M2::TState, string>
    var candidateClosureDeclaredType = closureCandidateParameterType as IDeclaredType;
    if (candidateClosureDeclaredType == null) return false;

    var candidateClosureDelegateType = candidateClosureDeclaredType.GetTypeElement() as IDelegate;
    if (candidateClosureDelegateType == null) return false;

    // {T1 -> int, T2 -> M1::T1, T3 -> List<M1::T2>, TResult -> string}
    var closureSubstitution = closureDeclaredType.GetSubstitution();

    // {T1 -> int, T2 -> M2::T1, T3 -> List<M2::T2>, T4 -> M2::TState, TResult -> string}
    var candidateClosureSubstitution = candidateClosureDeclaredType.GetSubstitution();

    // check return types
    var delegateReturnType = typeParametersMapping[closureSubstitution[closureDelegateType.InvokeMethod.ReturnType]];
    var candidateDelegateReturnType = candidateClosureSubstitution[candidateClosureDelegateType.InvokeMethod.ReturnType];
    if (!delegateReturnType.Equals(candidateDelegateReturnType)) return false;

    // TResult Func<T1, T2, T3, TResult>(T1 arg1, T2 arg2)
    var delegateParameters = closureDelegateType.InvokeMethod.Parameters;
    // TResult Func<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3)
    var candidateDelegateParameters = candidateClosureDelegateType.InvokeMethod.Parameters;

    var parametersCountDelta = candidateDelegateParameters.Count - delegateParameters.Count;
    if (parametersCountDelta != stateTypeParameters.Count) return false; // we expect more parameters

    HashSet<ITypeParameter>? stateParametersToVisit = null;
    int currentIndex = 0, candidateIndex = 0;

    while (candidateIndex < candidateDelegateParameters.Count)
    {
      var candidateDelegateParameter = candidateDelegateParameters[candidateIndex];

      if (currentIndex < delegateParameters.Count)
      {
        var delegateParameter = delegateParameters[currentIndex];

        if (CompareParameterKinds(delegateParameter, candidateDelegateParameter))
        {
          var delegateParameterType = typeParametersMapping[closureSubstitution[delegateParameter.Type]];
          var candidateDelegateParameterType = candidateClosureSubstitution[candidateDelegateParameter.Type];

          if (delegateParameterType.Equals(candidateDelegateParameterType))
          {
            currentIndex++;
            candidateIndex++;
            continue;
          }
        }
      }

      var stateTypeParameter = TryFindStateTypeParameter(candidateDelegateParameter, candidateClosureSubstitution);
      if (stateTypeParameter != null)
      {
        stateParametersToVisit ??= new HashSet<ITypeParameter>(stateTypeParameters);

        if (stateParametersToVisit.Remove(stateTypeParameter))
        {
          candidateIndex++;
          continue;
        }
      }

      return false;
    }

    return stateParametersToVisit is { Count: 0 };
  }

  private static IEnumerable<Pair<IList<ITypeParameter>, ISubstitution>> EnumeratePossibleTStateCandidates(
    IList<ITypeParameter> typeParameters, IList<ITypeParameter> candidateTypeParameters)
  {
    var typeParametersCount = typeParameters.Count;

    var delta = candidateTypeParameters.Count - typeParametersCount;
    if (delta <= 0) yield break;

    ISubstitution headSubstitution = EmptySubstitution.INSTANCE;

    var candidateTypes = candidateTypeParameters.ToIList(TypeFactory.CreateType);

    var stateTypeParameters = new List<ITypeParameter>();
    var tailTypeParameters = new List<ITypeParameter>();
    var tailTypeArguments = new List<IType>();

    for (var headIndex = 0; headIndex <= typeParametersCount; headIndex++)
    {
      tailTypeParameters.Clear();
      tailTypeArguments.Clear();

      for (var tailIndex = headIndex; tailIndex < typeParametersCount; tailIndex++)
      {
        tailTypeParameters.Add(typeParameters[tailIndex]);
        tailTypeArguments.Add(candidateTypes[tailIndex + delta]);
      }

      stateTypeParameters.Clear();
      for (var stateIndex = 0; stateIndex < delta; stateIndex++)
      {
        stateTypeParameters.Add(candidateTypeParameters[headIndex + stateIndex]);
      }

      yield return Pair.Of<IList<ITypeParameter>, ISubstitution>(
        stateTypeParameters, headSubstitution.Extend(tailTypeParameters, tailTypeArguments));

      if (headIndex == typeParametersCount) yield break;

      headSubstitution = headSubstitution.Extend(typeParameters[headIndex], candidateTypes[headIndex]);
    }
  }

  private static bool CompareParameterKinds(IParameter currentParameter, IParameter candidateParameter)
  {
    if (currentParameter.Kind != candidateParameter.Kind) return false;
    if (currentParameter.IsParameterArray != candidateParameter.IsParameterArray) return false;

    if (currentParameter.IsOptional)
    {
      if (!candidateParameter.IsOptional) return false;

      var currentDefaultValue = currentParameter.GetDefaultValue();
      var candidateDefaultValue = candidateParameter.GetDefaultValue();
      if (!currentDefaultValue.Equals(candidateDefaultValue)) return false;
    }

    return true;
  }
}