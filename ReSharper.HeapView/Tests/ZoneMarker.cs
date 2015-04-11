using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Application.Zones;

namespace JetBrains.ReSharper.HeapView
{
  [ZoneDefinition]
  public class IHeapViewTestEnvironmentZone : ITestsZone, IRequire<PsiFeatureTestZone> { }

  [ZoneMarker]
  public class ZoneMarker : IRequire<ICodeEditingZone>, IRequire<ILanguageCSharpZone>, IRequire<IHeapViewTestEnvironmentZone> { }
}