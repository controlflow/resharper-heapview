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
  // todo: compiler do not emits boxing when component is discarded: (object _, var y) = t;
  // todo: possible + definite boxing inside tuple conversion - what is the highlighting type?

  [Test] public void TestTuples01() { DoNamedTest2(); }

  [Test] public void TestTuplesIndividualRight01() { DoNamedTest2(); }
  [Test] public void TestTuplesIndividualRight02() { DoNamedTest2(); }
  [Test] public void TestTuplesIndividualRight03() { DoNamedTest2(); }

  [Test] public void TestTuplesIndividualLeft01() { DoNamedTest2(); }
  [Test] public void TestTuplesIndividualLeft02() { DoNamedTest2(); }
  [Test] public void TestTuplesIndividualLeft03() { DoNamedTest2(); }

  [Test] public void TestTuplesForeach01() { DoNamedTest2(); }
}

[TestNet60]
//[TestAdditionalGoldSuffixes(".net6")]
public class BoxingAllocationsNetCoreTest : BoxingAllocationsTest
{
  // note: use the same gold

  [Test] public void TestTuplesAwaitForeach01() { DoNamedTest2(); }
}