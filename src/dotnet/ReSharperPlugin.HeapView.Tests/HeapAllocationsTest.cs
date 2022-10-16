using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Tests;

public abstract class HeapAllocationsTestBase : CSharpHighlightingTestBase
{
  protected override string RelativeTestDataPath => "Allocations";

  protected override bool HighlightingPredicate(
    IHighlighting highlighting, IPsiSourceFile sourceFile, IContextBoundSettingsStore settingsStore)
  {
    return highlighting
      is ObjectAllocationHighlighting
      or ObjectAllocationEvidentHighlighting
      or ObjectAllocationPossibleHighlighting;
  }

  [Test] public void TestObjectCreation01() { DoNamedTest2(); }
  [Test] public void TestObjectCreation02() { DoNamedTest2(); }
  [Test] public void TestObjectCreation03() { DoNamedTest2(); }
  [Test] public void TestObjectCreation04() { DoNamedTest2(); } // fix: after PSI fix

  [Test] public void TestAnonymousObject01() { DoNamedTest2(); }
  [Test] public void TestAnonymousObject02() { DoNamedTest2(); }
  [Test] public void TestAnonymousObject03() { DoNamedTest2(); }

  [Test] public void TestArrayCreation01() { DoNamedTest2(); }
  [Test] public void TestArrayCreation02() { DoNamedTest2(); }

  [Test] public void TestArrayInitializer01() { DoNamedTest2(); }

  [Test] public void TestWithExpression01() { DoNamedTest2(); }
}

[TestNetFramework46]
public class HeapAllocationsNetFrameworkTest : HeapAllocationsTestBase
{
  [Test] public void TestActivatorCreateInstanceFramework01() { DoNamedTest2(); }
}

[TestNet60]
public class HeapAllocationsNetCoreTest : HeapAllocationsTestBase
{
  [Test] public void TestArrayCreationCore01() { DoNamedTest2(); }

  [Test] public void TestSliceCore01() { DoNamedTest2(); }
  [Test] public void TestSliceCore02() { DoNamedTest2(); }

  [Test] public void TestActivatorCreateInstanceCore01() { DoNamedTest2(); }
}