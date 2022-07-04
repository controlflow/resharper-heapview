using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
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
    return highlighting is BoxingAllocationHighlighting or PossibleBoxingAllocationHighlighting;
  }

  [Test] public void TestBoxing01() { DoNamedTest2(); }
  [Test] public void TestBoxing02() { DoNamedTest2(); }
  [Test] public void TestBoxing03() { DoNamedTest2(); }
  [Test] public void TestBoxing04() { DoNamedTest2(); }
  [Test] public void TestBoxing05() { DoNamedTest2(); }

  [Test] public void TestGenericBoxing01() { DoNamedTest2(); }
  [Test] public void TestGenericBoxing02() { DoNamedTest2(); }
  [Test] public void TestGenericBoxing03() { DoNamedTest2(); }
  [Test] public void TestGenericBoxing04() { DoNamedTest2(); }

  // todo: test conversion logic in-depth
  // todo: possible boxing
  // todo: test refinements

  [Test] public void TestTuplesIndividualRight01() { DoNamedTest2(); }
  [Test] public void TestTuplesIndividualRight02() { DoNamedTest2(); }
  [Test] public void TestTuplesIndividualRight03() { DoNamedTest2(); }

  [Test] public void TestTuplesIndividualLeft01() { DoNamedTest2(); }
  [Test] public void TestTuplesIndividualLeft02() { DoNamedTest2(); }
  [Test] public void TestTuplesIndividualLeft03() { DoNamedTest2(); }

  [Test] public void TestTuplesForeach01() { DoNamedTest2(); }
  [Test] public void TestTuplesForeach02() { DoNamedTest2(); }

  [Test] public void TestTuplesMerged01() { DoNamedTest2(); }
  [Test] public void TestTuplesMerged02() { DoNamedTest2(); }
  [Test] public void TestTuplesMerged03() { DoNamedTest2(); }
  [Test] public void TestTuplesMerged04() { DoNamedTest2(); }
  [Test] public void TestTuplesMerged05() { DoNamedTest2(); }

  [Test] public void TestTuplesAndUserDefined01() { DoNamedTest2(); }
  [Test] public void TestTuplesAndUserDefined02() { DoNamedTest2(); }

  [CSharpLanguageLevel(CSharpLanguageLevel.CSharp73)]
  [Test] public void TestConcatenationOptimization01() { DoNamedTest2(); }
  [Test] public void TestConcatenationOptimization02() { DoNamedTest2(); }
}

[TestNet60]
//[TestAdditionalGoldSuffixes(".net6")]
public class BoxingAllocationsNetCoreTest : BoxingAllocationsTest
{
  // note: use the same gold

  [Test] public void TestTuplesAwaitForeach01() { DoNamedTest2(); }
}