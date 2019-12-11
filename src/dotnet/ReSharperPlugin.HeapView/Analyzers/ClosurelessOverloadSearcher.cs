using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

namespace ReSharperPlugin.HeapView.Analyzers
{
  // todo: support M(Key, Func<T>) + M(TKey, Func<TKey, T>) where TKey : Key;

  public static class ClosurelessOverloadSearcher
  {
    [CanBeNull]
    public static IReference FindMethodInvocation([NotNull] ICSharpExpression expression)
    {
      var containingExpression = expression.GetContainingParenthesizedExpressionStrict();
      var argument = CSharpArgumentNavigator.GetByValue(containingExpression);

      var invocationExpression = InvocationExpressionNavigator.GetByArgument(argument);

      var invokedReference = invocationExpression?.InvokedExpression as IReferenceExpression;
      if (invokedReference == null) return null;

      var invocationReference = invocationExpression.InvocationExpressionReference;

      var resolveResult = invocationReference.Resolve();
      if (resolveResult.ResolveErrorType != ResolveErrorType.OK) return null;
      if (!(resolveResult.DeclaredElement is IMethod)) return null;

      return invokedReference.Reference;
    }

    [CanBeNull]
    public static DeclaredElementInstance<IParameter> FindClosureParameter([NotNull] ICSharpExpression expression)
    {
      var containingExpression = expression.GetContainingParenthesizedExpressionStrict();
      var argument = CSharpArgumentNavigator.GetByValue(containingExpression);
      var parameterInstance = argument?.MatchingParameter;
      if (parameterInstance?.Expanded != ArgumentsUtil.ExpandedKind.None) return null;

      var parameterDeclaredType = parameterInstance.Type as IDeclaredType;

      var delegateType = parameterDeclaredType?.GetTypeElement() as IDelegate;
      if (delegateType == null) return null;

      return parameterInstance;
    }

    [CanBeNull]
    public static IMethod FindOverloadByParameter([NotNull] DeclaredElementInstance<IParameter> parameterInstance)
    {
      var closureParameter = parameterInstance.Element.NotNull("closureParameter != null");

      var currentMethod = closureParameter.ContainingParametersOwner as IMethod;

      var containingType = currentMethod?.GetContainingType();
      if (containingType == null) return null;

      var shortName = currentMethod.ShortName;

      foreach (var typeMember in containingType.GetMembers())
      {
        if (typeMember is IMethod closurelessCandidate
          && !ReferenceEquals(closurelessCandidate, currentMethod)
          && closurelessCandidate.ShortName == shortName
          && closurelessCandidate.IsStatic == currentMethod.IsStatic
          && closurelessCandidate.IsExtensionMethod == currentMethod.IsExtensionMethod)
        {
          if (CompareSignatures(currentMethod, closurelessCandidate, closureParameter))
          {
            return closurelessCandidate;
          }
        }
      }

      return null;
    }

    private static bool CompareSignatures([NotNull] IMethod currentMethod, [NotNull] IMethod closurelessCandidate, [NotNull] IParameter closureParameter)
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
        foreach (var pair in EnumeratePossibleTStateCandidates(typeParameters, candidateTypeParameters))
        {
          // ([M2::T1], {M1::T1 -> M2::T2, M1::T2 -> M2::TState}),
          // ([M2::T2], {M1::T1 -> M2::T1, M1::T2 -> M2::TState}),
          // ([M3::TState], {M1::T1 -> M2::T1, M1::T2 -> M2::T2})
          var stateTypeParameters = pair.First;
          var candidateSubstitution = pair.Second;
          HashSet<ITypeParameter> stateParametersToVisit = null;

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
              stateParametersToVisit = stateParametersToVisit ?? new HashSet<ITypeParameter>(stateTypeParameters);

              if (stateParametersToVisit.Remove(stateTypeParameter))
              {
                candidateIndex++;
                continue;
              }
            }

            stateParametersToVisit = null;
            break;
          }

          if (stateParametersToVisit != null && stateParametersToVisit.Count == 0) return true;
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

    [CanBeNull]
    private static ITypeParameter TryFindStateTypeParameter([NotNull] IParameter parameter, [NotNull] ISubstitution substitution)
    {
      if (parameter.Kind != ParameterKind.VALUE) return null;
      if (parameter.IsParameterArray) return null;
      if (parameter.IsOptional) return null;

      var declaredType = substitution[parameter.Type] as IDeclaredType;

      if (declaredType?.GetTypeElement() is ITypeParameter typeParameter
          && typeParameter.OwnerFunction != null
          && !typeParameter.HasDefaultConstructor)
      {
        return typeParameter;
      }

      return null;
    }

    private static bool CompareDelegateTypes(
      [NotNull] IType closureParameterType, [NotNull] IType closureCandidateParameterType,
      [NotNull] IList<ITypeParameter> stateTypeParameters, [NotNull] ISubstitution typeParametersMapping)
    {
      // Func<int, M1::T1, List<M1::T2>, string>
      var closureDeclaredType = closureParameterType as IDeclaredType;
      var closureDelegateType = closureDeclaredType?.GetTypeElement() as IDelegate;
      if (closureDelegateType == null) return false;

      // Func<int, M2::T1, List<M2::T2>, M2::TState, string>
      var candidateClosureDeclaredType = closureCandidateParameterType as IDeclaredType;
      var candidateClosureDelegateType = candidateClosureDeclaredType?.GetTypeElement() as IDelegate;
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

      HashSet<ITypeParameter> stateParametersToVisit = null;
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
          stateParametersToVisit = stateParametersToVisit ?? new HashSet<ITypeParameter>(stateTypeParameters);

          if (stateParametersToVisit.Remove(stateTypeParameter))
          {
            candidateIndex++;
            continue;
          }
        }

        return false;
      }

      return stateParametersToVisit != null && stateParametersToVisit.Count == 0;
    }

    [NotNull]
    private static IEnumerable<Pair<IList<ITypeParameter>, ISubstitution>> EnumeratePossibleTStateCandidates(
      [NotNull] IList<ITypeParameter> typeParameters, [NotNull] IList<ITypeParameter> candidateTypeParameters)
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

    private static bool CompareParameterKinds([NotNull] IParameter currentParameter, [NotNull] IParameter candidateParameter)
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
}