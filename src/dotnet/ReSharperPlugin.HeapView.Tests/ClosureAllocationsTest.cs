using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Tests;

public abstract class ClosureAllocationsTestBase : CSharpHighlightingTestBase
{
  protected override string RelativeTestDataPath => "Closures";

  protected override bool HighlightingPredicate(
    IHighlighting highlighting, IPsiSourceFile sourceFile, IContextBoundSettingsStore settingsStore)
  {
    return highlighting is PerformanceHighlightingBase;
  }

  [Test] public void TestClosures01() { DoNamedTest(); }
  [Test] public void TestClosures02() { DoNamedTest(); }
  [Test] public void TestClosures03() { DoNamedTest(); }
  [Test] public void TestClosures04() { DoNamedTest(); }
  [Test] public void TestClosures05() { DoNamedTest(); }
  [Test] public void TestClosures06() { DoNamedTest(); }
  [Test] public void TestClosures07() { DoNamedTest(); }
  [Test] public void TestClosures08() { DoNamedTest(); }
  [Test] public void TestClosures09() { DoNamedTest(); }
  [Test] public void TestClosures10() { DoNamedTest(); }
  [Test] public void TestClosures11() { DoNamedTest(); }
  [Test] public void TestClosures12() { DoNamedTest(); }
  [Test] public void TestClosures13() { DoNamedTest(); }
  [Test] public void TestClosures14() { DoNamedTest(); }

  [Test] public void TestThisCapture01() { DoNamedTest(); }
  [Test] public void TestThisCapture02() { DoNamedTest(); }
  [Test] public void TestThisCapture03() { DoNamedTest(); }
  [Test] public void TestThisCapture04() { DoNamedTest(); }

  [Test] public void TestIndexers01() { DoNamedTest(); }
  [Test] public void TestIndexers02() { DoNamedTest(); }
  [Test] public void TestIndexers03() { DoNamedTest(); }
  [Test] public void TestIndexers04() { DoNamedTest(); }

  [Test] public void TestStructClosure01() { DoNamedTest(); }

  [Test] public void TestExpressionTree01() { DoNamedTest(); }

  [Test] public void TestExtensions01() { DoNamedTest(); }

  [CSharpLanguageLevel(CSharpLanguageLevel.CSharp100)]
  [Test] public void TestMethodGroup01() { DoNamedTest(); }
  [Test] public void TestMethodGroup02() { DoNamedTest(); }
  [Test] public void TestMethodGroup03() { DoNamedTest(); }
  [CSharpLanguageLevel(CSharpLanguageLevel.CSharp100)]
  [Test] public void TestMethodGroup04() { DoNamedTest(); }
  [Test] public void TestMethodGroup05() { DoNamedTest(); }
  [Test] public void TestMethodGroup06() { DoNamedTest(); }
  [Test] public void TestMethodGroup07() { DoNamedTest(); }
  [Test] public void TestMethodGroup08() { DoNamedTest(); }
  [Test] public void TestMethodGroup09() { DoNamedTest(); }

  [Test] public void TestClosureless01() { DoNamedTest(); }
  [Test] public void TestClosureless02() { DoNamedTest(); }
  [Test] public void TestClosureless03() { DoNamedTest(); }
  [Test] public void TestClosureless04() { DoNamedTest(); }
  [Test] public void TestClosureless05() { DoNamedTest(); }
}

[TestNetFramework46]
public class ClosureAllocationsNetFrameworkTest : ClosureAllocationsTestBase;

[TestNet80]
public class ClosureAllocationsNetCoreTest : ClosureAllocationsTestBase
{
  [Test] public void TestStructClosureCore01() { DoNamedTest(); }
}