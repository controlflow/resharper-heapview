#if RESHARPER2017_3 && !RIDER

using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.VsIntegration.Shell.Zones;

namespace JetBrains.ReSharper.HeapView.VisualStudio
{
#if RESHARPER2018_2
    [ZoneMarker]
    public class ZoneMarker : IRequire<IVisualStudioEnvZone> { }
#else
    [ZoneMarker]
    public class ZoneMarker : IRequire<IVisualStudioZone> { }
#endif
}

#endif