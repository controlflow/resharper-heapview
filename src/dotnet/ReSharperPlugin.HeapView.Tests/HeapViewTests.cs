using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Tests
{
  [TestNetFramework46]
  [TestPackages(Packages = new[] {SYSTEM_VALUE_TUPLE_PACKAGE})]
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

    [Test] public void TestBoxing01() { DoNamedTest2(); }
    [Test] public void TestBoxing02() { DoNamedTest2(); }
    [Test] public void TestBoxing03() { DoNamedTest2(); }
    [Test] public void TestBoxing04() { DoNamedTest2(); }
    [Test] public void TestBoxing05() { DoNamedTest2(); }
    [Test] public void TestBoxing06() { DoNamedTest2(); }
    [Test] public void TestBoxing07() { DoNamedTest2(); }
    [Test] public void TestBoxing08() { DoNamedTest2(); }
    [Test] public void TestBoxing09() { DoNamedTest2(); }
    [Test] public void TestBoxing10() { DoNamedTest2(); }
    [Test] public void TestBoxing11() { DoNamedTest2(); }
    [Test] public void TestBoxing12() { DoNamedTest2(); }
    [Test] public void TestBoxing13() { DoNamedTest2(); }
    [Test] public void TestBoxing14() { DoNamedTest2(); }
    [Test] public void TestBoxing15() { DoNamedTest2(); }
    [CSharpLanguageLevel(CSharpLanguageLevel.CSharp73)]
    [Test] public void TestBoxing15_73() { DoNamedTest2(); }
    [Test] public void TestBoxing16() { DoNamedTest2(); }
    [Test] public void TestBoxing17() { DoNamedTest2(); }
    [Test] public void TestBoxing18() { DoNamedTest2(); }

    [Test] public void TestClosure01() { DoNamedTest2(); }
    [Test] public void TestClosure02() { DoNamedTest2(); }
    [Test] public void TestClosure03() { DoNamedTest2(); }

    [Test] public void TestHeap01() { DoNamedTest2(); }
    [Test] public void TestHeap02() { DoNamedTest2(); }
    [Test] public void TestHeap03() { DoNamedTest2(); }

    [Test] public void TestClosureless01() { DoNamedTest2(); }
    [Test] public void TestClosureless02() { DoNamedTest2(); }
    [Test] public void TestClosureless03() { DoNamedTest2(); }
  }

  [TestNetCore30]
  [TestPackages(Packages = new[] {SYSTEM_VALUE_TUPLE_PACKAGE})]
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

    [Test] public void TestBoxing11() { DoNamedTest2(); }
    [Test] public void TestBoxing13() { DoNamedTest2(); }
    [Test] public void TestBoxing14() { DoNamedTest2(); }
    [Test] public void TestBoxing16() { DoNamedTest2(); }
  }
}