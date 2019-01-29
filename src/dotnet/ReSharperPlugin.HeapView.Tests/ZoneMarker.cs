using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Application.Zones;

namespace JetBrains.ReSharper.HeapView
{
  #if RESHARPER2018_2
  [ZoneDefinition]
  public class HeapViewTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone> { }
  #else
  [ZoneDefinition]
  public class HeapViewTestEnvironmentZone : ITestsZone, IRequire<PsiFeatureTestZone> { }
  #endif

  [ZoneMarker]
  public class ZoneMarker : IRequire<ICodeEditingZone>, IRequire<ILanguageCSharpZone>, IRequire<HeapViewTestEnvironmentZone> { }
}