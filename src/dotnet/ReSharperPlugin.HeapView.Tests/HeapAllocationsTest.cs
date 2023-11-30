using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
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
      or ObjectAllocationPossibleHighlighting
      or BoxingAllocationHighlighting;
  }

  [Test] public void TestObjectCreation01() { DoNamedTest2(); }
  [Test] public void TestObjectCreation02() { DoNamedTest2(); }
  [Test] public void TestObjectCreation03() { DoNamedTest2(); }
  [Test] public void TestObjectCreation04() { DoNamedTest2(); }
  [Test] public void TestObjectCreation05() { DoNamedTest2(); }

  [Test] public void TestAnonymousObject01() { DoNamedTest2(); }
  [Test] public void TestAnonymousObject02() { DoNamedTest2(); }
  [Test] public void TestAnonymousObject03() { DoNamedTest2(); }
  [Test] public void TestAnonymousObject04() { DoNamedTest2(); }
  [Test] public void TestAnonymousObject05() { DoNamedTest2(); }
  [Test] public void TestAnonymousObject06() { DoNamedTest2(); }
  [Test] public void TestAnonymousObject07() { DoNamedTest2(); }

  [Test] public void TestArrayCreation01() { DoNamedTest2(); }
  [Test] public void TestArrayCreation02() { DoNamedTest2(); }

  [Test] public void TestArrayInitializer01() { DoNamedTest2(); }

  [Test] public void TestCollectionExpression01() { DoNamedTest2(); }
  [Test] public void TestCollectionExpression02() { DoNamedTest2(); }
  [Test] public void TestCollectionExpression03() { DoNamedTest2(); }

  [Test] public void TestWithExpression01() { DoNamedTest2(); }

  [Test] public void TestStringConcat01() { DoNamedTest2(); }
  [Test] public void TestStringConcat02() { DoNamedTest2(); }

  [Test] public void TestEvents01() { DoNamedTest2(); }

  [Test] public void TestIterators01() { DoNamedTest2(); }

  [Test] public void TestForeach01() { DoNamedTest2(); }
}

[TestNetFramework45]
public class HeapAllocationsNetFrameworkTest : HeapAllocationsTestBase
{
  [Test] public void TestActivatorCreateInstanceFramework01() { DoNamedTest2(); }

  [Test] public void TestParamsFramework01() { DoNamedTest2(); }
  [Test] public void TestParamsFramework02() { DoNamedTest2(); }
  [Test] public void TestParamsFramework03() { DoNamedTest2(); }
  [Test] public void TestParamsFramework04() { DoNamedTest2(); }
  [Test] public void TestParamsFramework05() { DoNamedTest2(); }
  [Test] public void TestParamsFramework06() { DoNamedTest2(); }

  [Test] public void TestStringInterpolationFramework01() { DoNamedTest2(); }
}

[TestNet70]
public class HeapAllocationsNetCoreTest : HeapAllocationsTestBase
{
  [Test] public void TestArrayCreationCore01() { DoNamedTest2(); }

  [Test] public void TestSliceCore01() { DoNamedTest2(); }
  [Test] public void TestSliceCore02() { DoNamedTest2(); }

  [Test] public void TestActivatorCreateInstanceCore01() { DoNamedTest2(); }

  [Test] public void TestParamsCore01() { DoNamedTest2(); }
  [Test] public void TestParamsCore02() { DoNamedTest2(); }
  [Test] public void TestParamsCore03() { DoNamedTest2(); }
  [Test] public void TestParamsCore04() { DoNamedTest2(); }
  [Test] public void TestParamsCore05() { DoNamedTest2(); }
  [Test] public void TestParamsCore06() { DoNamedTest2(); }

  [Test] public void TestStringConcatCore01() { DoNamedTest2(); }

  [Test] public void TestStringInterpolationCore01() { DoNamedTest2(); }
  [Test] public void TestStringInterpolationCore02() { DoNamedTest2(); }
  [LatestSupportedCSharpLanguageLevel(CSharpLanguageLevel.CSharp90)]
  [Test] public void TestStringInterpolationCore03() { DoNamedTest2(); }
  [Test] public void TestStringInterpolationCore04() { DoNamedTest2(); }
  [Test] public void TestStringInterpolationCore05() { DoNamedTest2(); }
  [Test] public void TestStringInterpolationCore06() { DoNamedTest2(); }
  [Test] public void TestStringInterpolationCore07() { DoNamedTest2(); }
  [Test] public void TestStringInterpolationCore08() { DoNamedTest2(); }
  [Test] public void TestStringInterpolationCore09() { DoNamedTest2(); }

  [Test] public void TestIteratorsCore01() { DoNamedTest2(); }

  [Test] public void TestForeachCore01() { DoNamedTest2(); }
}