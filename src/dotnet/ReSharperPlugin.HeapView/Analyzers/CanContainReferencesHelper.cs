#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.TestRunner.Abstractions.Extensions;
using JetBrains.Util;

namespace ReSharperPlugin.HeapView.Analyzers;

public static class CanContainReferencesHelper
{
  public static bool CanContainManagedReferences(this ElementProblemAnalyzerData data, IType type)
  {
    if (type.IsTypeParameterType())
    {
      // struct type argument can contain reference
      // class type argument is itself a reference
      return true;
    }

    switch (type.Classify)
    {
      case TypeClassification.UNKNOWN:
        return false; // pointers, etc
      case TypeClassification.VALUE_TYPE:
        return CanContainReferencesInValueType(data, type);
      case TypeClassification.REFERENCE_TYPE:
        return true;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  private static readonly Key<ConcurrentDictionary<IDeclaredType, bool>> CacheKey = new(nameof(CacheKey));
  [ThreadStatic] private static HashSet<IType>? TypeExploration;

  [Pure]
  private static bool CanContainReferencesInValueType(ElementProblemAnalyzerData data, IType type)
  {
    var declaredType = type as IDeclaredType;
    if (declaredType == null) return false;

    var typeElement = declaredType.GetTypeElement();
    if (typeElement is not IStruct) return false; // enums, etc

    var containReferencedCache = data.GetOrCreateDataUnderLock(
      CacheKey, factory: static () => new ConcurrentDictionary<IDeclaredType, bool>(TypeEqualityComparer.Default));

    return containReferencedCache.GetOrAdd(
      key: declaredType,
      factoryArgument: data,
      valueFactory: static (type, data) =>
      {
        var typeExploration = TypeExploration ??= new HashSet<IType>(TypeEqualityComparer.Default);
        if (!typeExploration.Add(type)) return false;

        try
        {
          var structType = (IStruct)type.GetTypeElement().NotNull()!;
          var substitution = type.GetSubstitution();

          foreach (var typeMember in structType.GetMembers())
          {
            if (typeMember.IsStatic) continue;

            switch (typeMember)
            {
              case IField { IsField: true } field:
              {
                var fieldType = substitution.Apply(field.Type);
                if (CanContainManagedReferences(data, fieldType))
                  return true;

                break;
              }

              case IProperty property when property.IsAutoOrSemiAuto():
              {
                var backingFieldType = substitution.Apply(property.Type);
                if (CanContainManagedReferences(data, backingFieldType))
                  return true;

                break;
              }

              case IEvent { IsFieldLikeEvent: true }:
              {
                return true; // delegate types are reference types
              }
            }
          }

          return false;
        }
        finally
        {
          typeExploration.Remove(type);
        }
      });
  }
}