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

  [Test] public void TestObjectCreation01() { DoNamedTest(); }
  [Test] public void TestObjectCreation02() { DoNamedTest(); }
  [Test] public void TestObjectCreation03() { DoNamedTest(); }
  [Test] public void TestObjectCreation04() { DoNamedTest(); }
  [Test] public void TestObjectCreation05() { DoNamedTest(); }

  [Test] public void TestAnonymousObject01() { DoNamedTest(); }
  [Test] public void TestAnonymousObject02() { DoNamedTest(); }
  [Test] public void TestAnonymousObject03() { DoNamedTest(); }
  [Test] public void TestAnonymousObject04() { DoNamedTest(); }
  [Test] public void TestAnonymousObject05() { DoNamedTest(); }
  [Test] public void TestAnonymousObject06() { DoNamedTest(); }
  [Test] public void TestAnonymousObject07() { DoNamedTest(); }

  [Test] public void TestArrayCreation01() { DoNamedTest(); }
  [Test] public void TestArrayCreation02() { DoNamedTest(); }

  [Test] public void TestArrayInitializer01() { DoNamedTest(); }

  [Test] public void TestCollectionExpression02() { DoNamedTest(); }
  [Test] public void TestCollectionExpression03() { DoNamedTest(); }
  [Test] public void TestCollectionExpression05() { DoNamedTest(); }
  [Test] public void TestCollectionExpression06() { DoNamedTest(); }

  [Test] public void TestWithExpression01() { DoNamedTest(); }

  [Test] public void TestStringConcat01() { DoNamedTest(); }
  [Test] public void TestStringConcat02() { DoNamedTest(); }

  [Test] public void TestEvents01() { DoNamedTest(); }

  [Test] public void TestIterators01() { DoNamedTest(); }

  [Test] public void TestForeach01() { DoNamedTest(); }
}

[TestNetFramework45]
[TestPackages(Packages = [SYSTEM_MEMORY_PACKAGE], Inherits = true)]
public class HeapAllocationsNetFrameworkTest : HeapAllocationsTestBase
{
  [Test] public void TestActivatorCreateInstanceFramework01() { DoNamedTest(); }

  [Test] public void TestParamsFramework01() { DoNamedTest(); }
  [Test] public void TestParamsFramework02() { DoNamedTest(); }
  [Test] public void TestParamsFramework03() { DoNamedTest(); }
  [Test] public void TestParamsFramework04() { DoNamedTest(); }
  [Test] public void TestParamsFramework05() { DoNamedTest(); }
  [Test] public void TestParamsFramework06() { DoNamedTest(); }

  [Test] public void TestCollectionExpressionFramework01() { DoNamedTest(); }
  [Test] public void TestCollectionExpressionFramework02() { DoNamedTest(); }

  [Test] public void TestStringInterpolationFramework01() { DoNamedTest(); }

  [Test] public void TestParamsCollectionsFramework01() { DoNamedTest(); }
  [Test] public void TestParamsCollectionsFramework02() { DoNamedTest(); }
  [Test] public void TestParamsCollectionsFramework03() { DoNamedTest(); }
  [Test] public void TestParamsCollectionsFramework04() { DoNamedTest(); }
}

[TestNet90]
public class HeapAllocationsNetCoreTest : HeapAllocationsTestBase
{
  [Test] public void TestArrayCreationCore01() { DoNamedTest(); }

  [Test] public void TestSliceCore01() { DoNamedTest(); }
  [Test] public void TestSliceCore02() { DoNamedTest(); }

  [Test] public void TestActivatorCreateInstanceCore01() { DoNamedTest(); }

  [Test] public void TestParamsCore01() { DoNamedTest(); }
  [Test] public void TestParamsCore02() { DoNamedTest(); }
  [Test] public void TestParamsCore03() { DoNamedTest(); }
  [Test] public void TestParamsCore04() { DoNamedTest(); }
  [Test] public void TestParamsCore05() { DoNamedTest(); }
  [Test] public void TestParamsCore06() { DoNamedTest(); }

  [Test] public void TestStringConcatCore01() { DoNamedTest(); }

  [Test] public void TestStringInterpolationCore01() { DoNamedTest(); }
  [Test] public void TestStringInterpolationCore02() { DoNamedTest(); }
  [LatestSupportedCSharpLanguageLevel(CSharpLanguageLevel.CSharp90)]
  [Test] public void TestStringInterpolationCore03() { DoNamedTest(); }
  [Test] public void TestStringInterpolationCore04() { DoNamedTest(); }
  [Test] public void TestStringInterpolationCore05() { DoNamedTest(); }
  [Test] public void TestStringInterpolationCore06() { DoNamedTest(); }
  [Test] public void TestStringInterpolationCore07() { DoNamedTest(); }
  [Test] public void TestStringInterpolationCore08() { DoNamedTest(); }
  [Test] public void TestStringInterpolationCore09() { DoNamedTest(); }

  [Test] public void TestIteratorsCore01() { DoNamedTest(); }

  [Test] public void TestForeachCore01() { DoNamedTest(); }

  [Test] public void TestCollectionExpressionCore01() { DoNamedTest(); }
  [Test] public void TestCollectionExpressionCore02() { DoNamedTest(); }
  [Test] public void TestCollectionExpressionCore03() { DoNamedTest(); }
  [Test] public void TestCollectionExpressionCore04() { DoNamedTest(); }
  [Test] public void TestCollectionExpressionCore05() { DoNamedTest(); }
  [Test] public void TestCollectionExpressionCore06() { DoNamedTest(); }

  [Test] public void TestParamsCollectionsCore01() { DoNamedTest(); }
  [Test] public void TestParamsCollectionsCore02() { DoNamedTest(); }
  [Test] public void TestParamsCollectionsCore03() { DoNamedTest(); }
  [Test] public void TestParamsCollectionsCore04() { DoNamedTest(); }
  [Test] public void TestParamsCollectionsCore05() { DoNamedTest(); }
  [Test] public void TestParamsCollectionsCore06() { DoNamedTest(); }
  [Test] public void TestParamsCollectionsCore07() { DoNamedTest(); }
  [Test] public void TestParamsCollectionsCore08() { DoNamedTest(); }
}

[TestNet70]
public class HeapAllocationsNetCore70Test : CSharpHighlightingTestBase
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

  // no inline arrays
  [Test] public void TestCollectionExpressionCore70_01() { DoNamedTest(); }
  [Test] public void TestCollectionExpressionCore70_02() { DoNamedTest(); }
  [Test] public void TestCollectionExpressionCore70_04() { DoNamedTest(); }
}