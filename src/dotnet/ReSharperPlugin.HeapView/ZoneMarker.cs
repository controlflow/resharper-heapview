using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi.CSharp;

namespace ReSharperPlugin.HeapView
{
  [ZoneMarker]
  public class ZoneMarker : IRequire<ILanguageCSharpZone> { }
}