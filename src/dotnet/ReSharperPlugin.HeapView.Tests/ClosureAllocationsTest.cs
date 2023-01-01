using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.VB.Errors;
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

  [Test] public void TestClosures01() { DoNamedTest2(); }
  [Test] public void TestClosures02() { DoNamedTest2(); }
  [Test] public void TestClosures03() { DoNamedTest2(); }
  [Test] public void TestClosures04() { DoNamedTest2(); }
  [Test] public void TestClosures05() { DoNamedTest2(); }
  [Test] public void TestClosures06() { DoNamedTest2(); }
  [Test] public void TestClosures07() { DoNamedTest2(); }
  [Test] public void TestClosures08() { DoNamedTest2(); }
  [Test] public void TestClosures09() { DoNamedTest2(); }
  [Test] public void TestClosures10() { DoNamedTest2(); }
  [Test] public void TestClosures11() { DoNamedTest2(); }

  [Test] public void TestThisCapture01() { DoNamedTest2(); }
  [Test] public void TestThisCapture02() { DoNamedTest2(); }
  [Test] public void TestThisCapture03() { DoNamedTest2(); }
  [Test] public void TestThisCapture04() { DoNamedTest2(); }

  [Test] public void TestIndexers01() { DoNamedTest2(); }
  [Test] public void TestIndexers02() { DoNamedTest2(); }
  [Test] public void TestIndexers03() { DoNamedTest2(); }
  [Test] public void TestIndexers04() { DoNamedTest2(); }

  [Test] public void TestStructClosure01() { DoNamedTest2(); }

  [Test] public void TestExpressionTree01() { DoNamedTest2(); }

  [LatestSupportedCSharpLanguageLevel(CSharpLanguageLevel.CSharp100)]
  [Test] public void TestMethodGroup01() { DoNamedTest2(); }
  [Test] public void TestMethodGroup02() { DoNamedTest2(); }
  [Test] public void TestMethodGroup03() { DoNamedTest2(); }
  [LatestSupportedCSharpLanguageLevel(CSharpLanguageLevel.CSharp100)]
  [Test] public void TestMethodGroup04() { DoNamedTest2(); }
  [Test] public void TestMethodGroup05() { DoNamedTest2(); }
  [Test] public void TestMethodGroup06() { DoNamedTest2(); }
  [Test] public void TestMethodGroup07() { DoNamedTest2(); }
  [Test] public void TestMethodGroup08() { DoNamedTest2(); }

  [Test] public void TestClosureless01() { DoNamedTest2(); }
  [Test] public void TestClosureless02() { DoNamedTest2(); }
  [Test] public void TestClosureless03() { DoNamedTest2(); }
  [Test] public void TestClosureless04() { DoNamedTest2(); }
  [Test] public void TestClosureless05() { DoNamedTest2(); }
}

[TestNetFramework46]
public class ClosureAllocationsNetFrameworkTest : ClosureAllocationsTestBase
{
}

[TestNet70]
public class ClosureAllocationsNetCoreTest : ClosureAllocationsTestBase
{
  [Test] public void TestStructClosureCore01() { DoNamedTest2(); }
}