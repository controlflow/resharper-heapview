using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Tests;

[TestNetFramework46]
public class BoxingAllocationsTest : CSharpHighlightingTestBase
{
  protected override string RelativeTestDataPath => "Boxing";

  protected override bool HighlightingPredicate(
    IHighlighting highlighting, IPsiSourceFile sourceFile, IContextBoundSettingsStore settingsStore)
  {
    return highlighting is PerformanceHighlightingBase;
  }

  [Test] public void TestBoxing01() { DoNamedTest2(); }

  // todo: possible boxing
  // todo: test refinements

  [Test] public void TestTuples01() { DoNamedTest2(); }

  [Test] public void TestTuplesIndividualRight01() { DoNamedTest2(); }
  [Test] public void TestTuplesIndividualRight02() { DoNamedTest2(); }
  [Test] public void TestTuplesIndividualRight03() { DoNamedTest2(); }

}