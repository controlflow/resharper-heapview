using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using NUnit.Framework;

[assembly: RequiresSTA]

namespace JetBrains.ReSharper.HeapView
{
  [ZoneDefinition]
  public class HeapViewTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone> { }

  [ZoneMarker]
  public class ZoneMarker : IRequire<ICodeEditingZone>, IRequire<ILanguageCSharpZone>, IRequire<HeapViewTestEnvironmentZone> { }
}