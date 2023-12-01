using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.HeapView.Highlightings;
using ReSharperPlugin.HeapView.Settings;

namespace ReSharperPlugin.HeapView.Tests;

public abstract class BoxingAllocationsTestBase : CSharpHighlightingTestBase
{
  protected override string RelativeTestDataPath => "Boxing";

  protected override bool HighlightingPredicate(
    IHighlighting highlighting, IPsiSourceFile sourceFile, IContextBoundSettingsStore settingsStore)
  {
    return highlighting is BoxingAllocationHighlighting or PossibleBoxingAllocationHighlighting;
  }

  [Test] public void TestBoxing01() { DoNamedTest2(); }
  [Test] public void TestBoxing02() { DoNamedTest2(); }
  [Test] public void TestBoxing03() { DoNamedTest2(); }
  [Test] public void TestBoxing04() { DoNamedTest2(); }
  [Test] public void TestBoxing05() { DoNamedTest2(); }
  [Test] public void TestBoxing06() { DoNamedTest2(); }
  [Test] public void TestBoxing07() { DoNamedTest2(); }

  [Test] public void TestNullChecks01() { DoNamedTest2(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestNullChecks02() { DoNamedTest2(); }
  [Test] public void TestNullChecks03() { DoNamedTest2(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestNullChecks04() { DoNamedTest2(); }
  [Test] public void TestNullChecks05() { DoNamedTest2(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestNullChecks06() { DoNamedTest2(); }

  [Test] public void TestConstrainedBoxing01() { DoNamedTest2(); }

  [Test] public void TestGenericBoxing01() { DoNamedTest2(); }
  [Test] public void TestGenericBoxing02() { DoNamedTest2(); }
  [Test] public void TestGenericBoxing03() { DoNamedTest2(); }
  [Test] public void TestGenericBoxing04() { DoNamedTest2(); }
  [Test] public void TestGenericBoxing05() { DoNamedTest2(); }

  [Test] public void TestTuplesIndividualRight01() { DoNamedTest2(); }
  [Test] public void TestTuplesIndividualRight02() { DoNamedTest2(); }
  [Test] public void TestTuplesIndividualRight03() { DoNamedTest2(); }

  [Test] public void TestTuplesIndividualLeft01() { DoNamedTest2(); }
  [Test] public void TestTuplesIndividualLeft02() { DoNamedTest2(); }
  [Test] public void TestTuplesIndividualLeft03() { DoNamedTest2(); }
  [Test] public void TestTuplesIndividualLeft04() { DoNamedTest2(); }

  [Test] public void TestTuplesForeach01() { DoNamedTest2(); }
  [Test] public void TestTuplesForeach02() { DoNamedTest2(); }

  [Test] public void TestTuplesMerged01() { DoNamedTest2(); }
  [Test] public void TestTuplesMerged02() { DoNamedTest2(); }
  [Test] public void TestTuplesMerged03() { DoNamedTest2(); }
  [Test] public void TestTuplesMerged04() { DoNamedTest2(); }
  [Test] public void TestTuplesMerged05() { DoNamedTest2(); }

  [Test] public void TestTuplesAndUserDefined01() { DoNamedTest2(); }
  [Test] public void TestTuplesAndUserDefined02() { DoNamedTest2(); }

  [CSharpLanguageLevel(CSharpLanguageLevel.CSharp73)]
  [Test] public void TestConcatenationOptimization01() { DoNamedTest2(); }
  [Test] public void TestConcatenationOptimization02() { DoNamedTest2(); }

  [Test] public void TestStructVirtualMethodInvocation01() { DoNamedTest2(); }
  [Test] public void TestStructVirtualMethodInvocation02() { DoNamedTest2(); }
  [Test] public void TestStructVirtualMethodInvocation03() { DoNamedTest2(); }
  [Test] public void TestStructVirtualMethodInvocation04() { DoNamedTest2(); }
  [Test] public void TestStructVirtualMethodInvocation05() { DoNamedTest2(); }
  [Test] public void TestStructVirtualMethodInvocation06() { DoNamedTest2(); }

  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestStructGetTypeInvocation01() { DoNamedTest2(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestStructGetTypeInvocation02() { DoNamedTest2(); }
  [Test] public void TestStructGetTypeInvocation03() { DoNamedTest2(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreEnabled)]
  [Test] public void TestStructGetTypeInvocation04() { DoNamedTest2(); }

  [Test] public void TestStructMethodGroup01() { DoNamedTest2(); }
  [Test] public void TestStructMethodGroup02() { DoNamedTest2(); }

  [Test] public void TestExtensionMethodBoxing01() { DoNamedTest2(); }
  [Test] public void TestExtensionMethodBoxing02() { DoNamedTest2(); }
  [Test] public void TestExtensionMethodBoxing03() { DoNamedTest2(); }
  [Test] public void TestExtensionMethodBoxing04() { DoNamedTest2(); }

  [Test] public void TestArgList01() { DoNamedTest2(); }

  [Test] public void TestLinqCast01() { DoNamedTest2(); }
  [Test] public void TestLinqCast02() { DoNamedTest2(); }
  [Test] public void TestLinqCast03() { DoNamedTest2(); }
  [Test] public void TestLinqCast04() { DoNamedTest2(); }

  [Test] public void TestPatternMatching01() { DoNamedTest2(); }
  [Test] public void TestPatternMatching02() { DoNamedTest2(); }
  [Test] public void TestPatternMatching03() { DoNamedTest2(); }
  [Test] public void TestPatternMatching04() { DoNamedTest2(); }
  [Test] public void TestPatternMatching05() { DoNamedTest2(); }
}

[TestNetFramework46]
public class BoxingAllocationsNetFrameworkTest : BoxingAllocationsTestBase
{
  [Test] public void TestEnumVirtualMethodInvocationFramework01() { DoNamedTest2(); }

  [Test] public void TestStructMethodGroupFramework01() { DoNamedTest2(); }
  [Test] public void TestStructMethodGroupFramework02() { DoNamedTest2(); }

  [Test] public void TestGenericUnboxingFramework01() { DoNamedTest2(); }

  [Test] public void TestPatternMatchingFramework04() { DoNamedTest2(); }
  [Test] public void TestPatternMatchingFramework05() { DoNamedTest2(); }
  [Test] public void TestPatternMatchingFramework06() { DoNamedTest2(); }
  [Test] public void TestPatternMatchingFramework07() { DoNamedTest2(); }

  [Test] public void TestStringInterpolationFramework01() { DoNamedTest2(); }

  [Test] public void TestConstrainedBoxingFramework01() { DoNamedTest2(); }

  [Test] public void TestEnumHasFlagFramework01() { DoNamedTest2(); }

  [Test] public void TestEnumGetHashCodeFramework01() { DoNamedTest2(); }
}

[TestNet80]
public class BoxingAllocationsNetCoreTest : BoxingAllocationsTestBase
{
  [Test] public void TestTuplesAwaitForeach01() { DoNamedTest2(); }
  [Test] public void TestTuplesAwaitForeach02() { DoNamedTest2(); }

  [Test] public void TestEnumVirtualMethodInvocationCore01() { DoNamedTest2(); }

  [Test] public void TestStructMethodGroupCore01() { DoNamedTest2(); }
  [Test] public void TestStructMethodGroupCore02() { DoNamedTest2(); }

  [Test] public void TestGenericUnboxingCore01() { DoNamedTest2(); }

  [Test] public void TestPatternMatchingCore04() { DoNamedTest2(); }
  [Test] public void TestPatternMatchingCore05() { DoNamedTest2(); }

  [Test] public void TestStringInterpolationCore01() { DoNamedTest2(); }
  [Test] public void TestStringInterpolationCore02() { DoNamedTest2(); }

  [Test] public void TestConstrainedBoxingCore01() { DoNamedTest2(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestConstrainedBoxingCore02() { DoNamedTest2(); }

  [Test] public void TestEnumHasFlagCore01() { DoNamedTest2(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestEnumHasFlagCore02() { DoNamedTest2(); }

  [Test] public void TestEnumGetHashCodeCore01() { DoNamedTest2(); }

  [Test] public void TestDefaultMembersCore01() { DoNamedTest2(); }
  [Test] public void TestDefaultMembersCore02() { DoNamedTest2(); }
  [Test] public void TestDefaultMembersCore03() { DoNamedTest2(); }
}