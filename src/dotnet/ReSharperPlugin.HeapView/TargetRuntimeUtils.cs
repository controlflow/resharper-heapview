using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

namespace ReSharperPlugin.HeapView;

public static class TargetRuntimeUtils
{
  private static readonly Key<Boxed<TargetRuntime>> RuntimeKey = new(nameof(RuntimeKey));

  [Pure]
  public static TargetRuntime GetTargetRuntime(this ElementProblemAnalyzerData data)
  {
    return (TargetRuntime) data.GetOrCreateDataNoLock(RuntimeKey, data.File, static file =>
    {
      var psiModule = file.GetPsiModule();
      var frameworkId = psiModule.TargetFrameworkId;

      if (frameworkId.IsNetCoreApp || frameworkId.IsNetCore)
        return Boxed.From(TargetRuntime.NetCore);

      if (frameworkId.IsNetFramework)
        return Boxed.From(TargetRuntime.NetFramework);

      return Boxed.From(TargetRuntime.Unknown);
    });
  }

  private static readonly Key<object> ArrayEmptyIsAvailableKey = new(nameof(ArrayEmptyIsAvailableKey));

  [Pure]
  public static bool IsSystemArrayEmptyMemberAvailable(this ElementProblemAnalyzerData data, IType elementType)
  {
    if (elementType.IsPointerOrFunctionPointer())
      return false; // can't use unmanaged types as type arguments for Array.Empty<T>()

    return (bool)data.GetOrCreateDataUnderLock(ArrayEmptyIsAvailableKey, data, static data =>
    {
      // note: we do not check for C# 6 compiler, since it is minimal supported compiler now

      var predefinedType = data.GetPredefinedType();
      if (predefinedType.Array.GetTypeElement() is IClass systemArrayTypeElement)
      {
        foreach (var typeMember in systemArrayTypeElement.EnumerateMembers(nameof(Array.Empty), caseSensitive: true))
        {
          if (typeMember is IMethod { Parameters.Count: 0, TypeParameters.Count: 1 } method
              && method.GetAccessRights() == AccessRights.PUBLIC)
          {
            return BooleanBoxes.True;
          }
        }
      }

      return BooleanBoxes.False;
    });
  }
}

public enum TargetRuntime
{
  Unknown,
  NetFramework,
  NetCore
}