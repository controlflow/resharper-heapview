using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
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
}

public enum TargetRuntime
{
  Unknown,
  NetFramework,
  NetCore
}