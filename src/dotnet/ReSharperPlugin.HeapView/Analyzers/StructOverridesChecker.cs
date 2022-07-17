using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.Util;

namespace ReSharperPlugin.HeapView.Analyzers;

public static class StructOverridesChecker
{
  [Flags]
  private enum StandardMethodOverrides
  {
    None        = 0,
    GetHashCode = 1 << 0,
    Equals      = 1 << 1,
    ToString    = 1 << 2
  }

  private static readonly Key<Dictionary<IStruct, StandardMethodOverrides>> StructOverridesCache = new(nameof(StructOverridesCache));

  public static bool IsMethodOverridenInStruct(
    [NotNull] IStruct structType, [NotNull] IMethod method, [NotNull] IUserDataHolder cache)
  {
    var overridesMap = cache.GetOrCreateDataUnderLock(
      StructOverridesCache, static () => new(DeclaredElementEqualityComparer.TypeElementComparer));

    StandardMethodOverrides overrides;
    lock (overridesMap)
    {
      if (!overridesMap.TryGetValue(structType, out overrides))
      {
        overridesMap[structType] = overrides = DiscoverOverrides(structType);
      }
    }

    return (overrides & NameToOverride(method.ShortName)) != 0;

    static StandardMethodOverrides DiscoverOverrides([NotNull] IStruct structType)
    {
      var allOverrides = StandardMethodOverrides.None;

      foreach (var methodOfStruct in structType.Methods)
      {
        if (!methodOfStruct.IsStatic && methodOfStruct.IsOverride)
        {
          allOverrides |= NameToOverride(methodOfStruct.ShortName);
        }
      }

      return allOverrides;
    }

    static StandardMethodOverrides NameToOverride([NotNull] string overrideName)
    {
      return overrideName switch
      {
        nameof(GetHashCode) => StandardMethodOverrides.GetHashCode,
        nameof(Equals) => StandardMethodOverrides.Equals,
        nameof(ToString) => StandardMethodOverrides.ToString,
        _ => StandardMethodOverrides.None
      };
    }
  }
}