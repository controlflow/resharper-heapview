using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.VsIntegration.Shell.Zones;

namespace ReSharperPlugin.HeapView.VisualStudio;

[ZoneMarker]
public class ZoneMarker : IRequire<IVisualStudioFrontendEnvZone> { }