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

  [Pure]
  public static bool IsMethodOverridenInStruct(
    IStruct structType, string methodShortName, IUserDataHolder cache)
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

    return (overrides & NameToOverride(methodShortName)) != 0;

    static StandardMethodOverrides DiscoverOverrides(IStruct structType)
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

    static StandardMethodOverrides NameToOverride(string overrideName)
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