using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
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
    return highlighting is ClosureAllocationHighlighting;
  }

  [Test] public void TestClosures01() { DoNamedTest2(); }
  [Test] public void TestClosures02() { DoNamedTest2(); }

  [Test] public void TestThisCapture01() { DoNamedTest2(); }
  [Test] public void TestThisCapture02() { DoNamedTest2(); }
  [Test] public void TestThisCapture03() { DoNamedTest2(); }
  [Test] public void TestThisCapture04() { DoNamedTest2(); }

  [Test] public void TestIndexers01() { DoNamedTest2(); }
  [Test] public void TestIndexers02() { DoNamedTest2(); }
  [Test] public void TestIndexers03() { DoNamedTest2(); }
  [Test] public void TestIndexers04() { DoNamedTest2(); }
}

[TestNetFramework46]
public class ClosureAllocationsNetFrameworkTest : ClosureAllocationsTestBase
{
}

[TestNet60]
public class ClosureAllocationsNetCoreTest : ClosureAllocationsTestBase
{
}