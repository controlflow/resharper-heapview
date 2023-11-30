using System.Threading;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using NUnit.Framework;

[assembly: Apartment(ApartmentState.STA)]

namespace ReSharperPlugin.HeapView.Tests;

[ZoneDefinition]
public class HeapViewTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>;

[ZoneMarker]
public class ZoneMarker : IRequire<ICodeEditingZone>, IRequire<ILanguageCSharpZone>, IRequire<HeapViewTestEnvironmentZone>;