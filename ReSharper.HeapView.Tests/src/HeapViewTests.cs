using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Generate;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.HeapView
{
  [TestNetFramework45]
#if !NOLANGLEVELATTR
  [CSharpLanguageLevel(CSharpLanguageLevel.CSharp50)]
#endif
  public class HeapViewHighlightingTest : CSharpHighlightingTestBase
  {
    protected override string RelativeTestDataPath => "Daemon";

#if RESHARPER2018_2
    protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
      IContextBoundSettingsStore settingsStore)
#else
    protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
#endif
    {
      return highlighting is BoxingAllocationHighlighting
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

    [Test] public void TestClosure01() { DoNamedTest2(); }
    [Test] public void TestClosure02() { DoNamedTest2(); }

    [Test] public void TestHeap01() { DoNamedTest2(); }
    [Test] public void TestHeap02() { DoNamedTest2(); }
    [Test] public void TestHeap03() { DoNamedTest2(); }

    [Test] public void TestClosureless01() { DoNamedTest2(); }
    [Test] public void TestClosureless02() { DoNamedTest2(); }
    [Test] public void TestClosureless03() { DoNamedTest2(); }

#if NOLANGLEVELATTR
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