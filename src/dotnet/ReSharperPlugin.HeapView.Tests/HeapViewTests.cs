using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.HeapView.Highlightings;
using ReSharperPlugin.HeapView.Settings;

namespace ReSharperPlugin.HeapView.Tests;

[TestNetFramework46]
[TestPackages(Packages = [ SYSTEM_VALUE_TUPLE_PACKAGE ])]
[TestReferences("System", "System.Core", "Microsoft.CSharp")]
[CSharpLanguageLevel(CSharpLanguageLevel.CSharp80)]
public class HeapViewHighlightingTest : CSharpHighlightingTestBase
{
  protected override string RelativeTestDataPath => "Daemon";

  protected override bool HighlightingPredicate(
    IHighlighting highlighting, IPsiSourceFile sourceFile, IContextBoundSettingsStore settingsStore)
  {
    return highlighting is PerformanceHighlightingBase;
  }

  [Test] public void TestBoxing01() { DoNamedTest(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestBoxing02() { DoNamedTest(); }
  [Test] public void TestBoxing03() { DoNamedTest(); }
  [Test] public void TestBoxing04() { DoNamedTest(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestBoxing05() { DoNamedTest(); }
  [Test] public void TestBoxing06() { DoNamedTest(); }
  [Test] public void TestBoxing07() { DoNamedTest(); }
  [Test] public void TestBoxing08() { DoNamedTest(); }
  [Test] public void TestBoxing09() { DoNamedTest(); }
  [Test] public void TestBoxing10() { DoNamedTest(); }
  [Test] public void TestBoxing11() { DoNamedTest(); }
  [Test] public void TestBoxing12() { DoNamedTest(); }
  [Test] public void TestBoxing13() { DoNamedTest(); }
  [Test] public void TestBoxing14() { DoNamedTest(); }
  [Test] public void TestBoxing15() { DoNamedTest(); }
  [CSharpLanguageLevel(CSharpLanguageLevel.CSharp73)]
  [Test] public void TestBoxing15_73() { DoNamedTest(); }
  [Test] public void TestBoxing16() { DoNamedTest(); }
  [Test] public void TestBoxing17() { DoNamedTest(); }
  [Test] public void TestBoxing18() { DoNamedTest(); }

  [Test] public void TestClosure01() { DoNamedTest(); }
  [Test] public void TestClosure02() { DoNamedTest(); }
  [Test] public void TestClosure03() { DoNamedTest(); }

  [Test] public void TestHeap01() { DoNamedTest(); }
  [Test] public void TestHeap02() { DoNamedTest(); }
  [Test] public void TestHeap03() { DoNamedTest(); }
}

[TestNetCore30]
[TestPackages(Packages = [ SYSTEM_VALUE_TUPLE_PACKAGE ])]
[TestReferences("System", "System.Core", "Microsoft.CSharp")]
[CSharpLanguageLevel(CSharpLanguageLevel.CSharp80)]
[TestAdditionalGoldSuffix(".netcore")]
public class HeapViewNetCoreHighlightingTest : CSharpHighlightingTestBase
{
  protected override string RelativeTestDataPath => "Daemon";

  protected override bool HighlightingPredicate(
    IHighlighting highlighting, IPsiSourceFile sourceFile, IContextBoundSettingsStore settingsStore)
  {
    return highlighting is PerformanceHighlightingBase;
  }

  [Test] public void TestBoxing11() { DoNamedTest(); }
  [Test] public void TestBoxing13() { DoNamedTest(); }
  [Test] public void TestBoxing14() { DoNamedTest(); }
  [Test] public void TestBoxing16() { DoNamedTest(); }
}