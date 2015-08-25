using JetBrains.ProjectModel;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using NUnit.Framework;
#if RESHARPER8
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp;
#elif RESHARPER9
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
#endif

namespace JetBrains.ReSharper.HeapView
{
  public class HeapViewHighlightingTest : CSharpHighlightingTestNet4Base
  {
    protected override string RelativeTestDataPath { get { return "Daemon"; } }

    #if RESHARPER8
    protected override bool HighlightingPredicate(IHighlighting highlighting, IContextBoundSettingsStore settingsStore)
    #elif RESHARPER9
    protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
    #endif
    {
      return highlighting is BoxingAllocationHighlighting
          || highlighting is ObjectAllocationHighlighting
          || highlighting is ObjectAllocationEvidentHighlighting
          || highlighting is ObjectAllocationPossibleHighlighting
          || highlighting is ClosureAllocationHighlighting
          || highlighting is DelegateAllocationHighlighting;
    }

    [Test] public void TestBoxing01() { DoNamedTest2(); }
    [Test] public void TestBoxing02() { DoNamedTest2(); }
    [Test] public void TestBoxing03() { DoNamedTest2(); }

    [Test] public void TestClosure01() { DoNamedTest2(); }
    [Test] public void TestClosure02() { DoNamedTest2(); }

    [Test] public void TestHeap01() { DoNamedTest2(); }
    [Test] public void TestHeap02() { DoNamedTest2(); }
    [Test] public void TestHeap03() { DoNamedTest2(); }

#if RESHARPER9

    protected override void DoTest(IProject project)
    {
      var languageLevelProjectProperty = project.GetComponent<CSharpLanguageLevelProjectProperty>();
      languageLevelProjectProperty.ExecuteWithLanguageLevel(project, CSharpLanguageLevel.CSharp50, () =>
      {
        base.DoTest(project);
      });
    }

#endif
  }
}