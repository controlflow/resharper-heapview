using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp;
using JetBrains.ReSharper.HeapView.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.HeapView
{
  public class CSharpPerformanceHighlightingTest : CSharpHighlightingTestNet4Base
  {
    protected override string RelativeTestDataPath { get { return "Daemon"; } }

    protected override void DoTestSolution(params string[] fileSet)
    {
      ExecuteWithinSettingsTransaction(store =>
      {
        //RunGuarded(() => store.SetValue(HighlightingSettingsAccessor.PerformanceInspections, true));

        base.DoTestSolution(fileSet);
      });
    }

    protected override bool HighlightingPredicate(
      IHighlighting highlighting, IContextBoundSettingsStore settingsStore)
    {
      return highlighting is BoxingAllocationHighlighting
          || highlighting is ObjectAllocationHighlighting
          || highlighting is SlowDelegateCreationHighlighting;
    }

    [Test] public void TestBoxing01() { DoNamedTest2(); }
    [Test] public void TestBoxing02() { DoNamedTest2(); }

    [Test] public void TestClosure01() { DoNamedTest2(); }
    [Test] public void TestClosure02() { DoNamedTest2(); }

    [Test] public void TestHeap01() { DoNamedTest2(); }
    [Test] public void TestHeap02() { DoNamedTest2(); }
  }
}