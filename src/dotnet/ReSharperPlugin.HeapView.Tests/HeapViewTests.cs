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
  [TestNetFramework45]
  [CSharpLanguageLevel(CSharpLanguageLevel.CSharp50)]
  public class HeapViewHighlightingTest : CSharpHighlightingTestBase
  {
    protected override string RelativeTestDataPath => "Daemon";

    protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
      IContextBoundSettingsStore settingsStore)
    {
      return highlighting is BoxingAllocationHighlighting
          || highlighting is BoxingAllocationPossibleHighlighting
          || highlighting is ObjectAllocationHighlighting
          || highlighting is ObjectAllocationEvidentHighlighting
          || highlighting is ObjectAllocationPossibleHighlighting
          || highlighting is ClosureAllocationHighlighting
          || highlighting is CanEliminateClosureCreationHighlighting
          || highlighting is DelegateAllocationHighlighting;
    }

    [Test] public void TestBoxing01() { DoNamedTest2(); }
    [Test] public void TestBoxing02() { DoNamedTest2(); }
    [Test] public void TestBoxing03() { DoNamedTest2(); }
    [Test] public void TestBoxing04() { DoNamedTest2(); }
    [Test] public void TestBoxing05() { DoNamedTest2(); }

    [Test] public void TestClosure01() { DoNamedTest2(); }
    [Test] public void TestClosure02() { DoNamedTest2(); }

    [Test] public void TestHeap01() { DoNamedTest2(); }
    [Test] public void TestHeap02() { DoNamedTest2(); }
    [Test] public void TestHeap03() { DoNamedTest2(); }

    [Test] public void TestClosureless01() { DoNamedTest2(); }
    [Test] public void TestClosureless02() { DoNamedTest2(); }
    [Test] public void TestClosureless03() { DoNamedTest2(); }
  }
}