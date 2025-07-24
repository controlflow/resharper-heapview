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

  [Test] public void TestBoxing01() { DoNamedTest(); }
  [Test] public void TestBoxing02() { DoNamedTest(); }
  [Test] public void TestBoxing03() { DoNamedTest(); }
  [Test] public void TestBoxing04() { DoNamedTest(); }
  [Test] public void TestBoxing05() { DoNamedTest(); }
  [Test] public void TestBoxing06() { DoNamedTest(); }
  [Test] public void TestBoxing07() { DoNamedTest(); }

  [Test] public void TestNullChecks01() { DoNamedTest(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestNullChecks02() { DoNamedTest(); }
  [Test] public void TestNullChecks03() { DoNamedTest(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestNullChecks04() { DoNamedTest(); }
  [Test] public void TestNullChecks05() { DoNamedTest(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestNullChecks06() { DoNamedTest(); }

  [Test] public void TestConstrainedBoxing01() { DoNamedTest(); }

  [Test] public void TestGenericBoxing01() { DoNamedTest(); }
  [Test] public void TestGenericBoxing02() { DoNamedTest(); }
  [Test] public void TestGenericBoxing03() { DoNamedTest(); }
  [Test] public void TestGenericBoxing04() { DoNamedTest(); }
  [Test] public void TestGenericBoxing05() { DoNamedTest(); }

  [Test] public void TestTuplesIndividualRight01() { DoNamedTest(); }
  [Test] public void TestTuplesIndividualRight02() { DoNamedTest(); }
  [Test] public void TestTuplesIndividualRight03() { DoNamedTest(); }

  [Test] public void TestTuplesIndividualLeft01() { DoNamedTest(); }
  [Test] public void TestTuplesIndividualLeft02() { DoNamedTest(); }
  [Test] public void TestTuplesIndividualLeft03() { DoNamedTest(); }
  [Test] public void TestTuplesIndividualLeft04() { DoNamedTest(); }

  [Test] public void TestTuplesForeach01() { DoNamedTest(); }
  [Test] public void TestTuplesForeach02() { DoNamedTest(); }

  [Test] public void TestTuplesMerged01() { DoNamedTest(); }
  [Test] public void TestTuplesMerged02() { DoNamedTest(); }
  [Test] public void TestTuplesMerged03() { DoNamedTest(); }
  [Test] public void TestTuplesMerged04() { DoNamedTest(); }
  [Test] public void TestTuplesMerged05() { DoNamedTest(); }

  [Test] public void TestTuplesAndUserDefined01() { DoNamedTest(); }
  [Test] public void TestTuplesAndUserDefined02() { DoNamedTest(); }

  [CSharpLanguageLevel(CSharpLanguageLevel.CSharp73)]
  [Test] public void TestConcatenationOptimization01() { DoNamedTest(); }
  [Test] public void TestConcatenationOptimization02() { DoNamedTest(); }

  [Test] public void TestStructVirtualMethodInvocation01() { DoNamedTest(); }
  [Test] public void TestStructVirtualMethodInvocation02() { DoNamedTest(); }
  [Test] public void TestStructVirtualMethodInvocation03() { DoNamedTest(); }
  [Test] public void TestStructVirtualMethodInvocation04() { DoNamedTest(); }
  [Test] public void TestStructVirtualMethodInvocation05() { DoNamedTest(); }
  [Test] public void TestStructVirtualMethodInvocation06() { DoNamedTest(); }

  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestStructGetTypeInvocation01() { DoNamedTest(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestStructGetTypeInvocation02() { DoNamedTest(); }
  [Test] public void TestStructGetTypeInvocation03() { DoNamedTest(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreEnabled)]
  [Test] public void TestStructGetTypeInvocation04() { DoNamedTest(); }

  [Test] public void TestStructMethodGroup01() { DoNamedTest(); }
  [Test] public void TestStructMethodGroup02() { DoNamedTest(); }

  [Test] public void TestExtensionMethodBoxing01() { DoNamedTest(); }
  [Test] public void TestExtensionMethodBoxing02() { DoNamedTest(); }
  [Test] public void TestExtensionMethodBoxing03() { DoNamedTest(); }
  [Test] public void TestExtensionMethodBoxing04() { DoNamedTest(); }

  [Test] public void TestArgList01() { DoNamedTest(); }

  [Test] public void TestLinqCast01() { DoNamedTest(); }
  [Test] public void TestLinqCast02() { DoNamedTest(); }
  [Test] public void TestLinqCast03() { DoNamedTest(); }
  [Test] public void TestLinqCast04() { DoNamedTest(); }

  [Test] public void TestPatternMatching01() { DoNamedTest(); }
  [Test] public void TestPatternMatching02() { DoNamedTest(); }
  [Test] public void TestPatternMatching03() { DoNamedTest(); }
  [Test] public void TestPatternMatching04() { DoNamedTest(); }
  [Test] public void TestPatternMatching05() { DoNamedTest(); }
}

[TestNetFramework46]
public class BoxingAllocationsNetFrameworkTest : BoxingAllocationsTestBase
{
  [Test] public void TestEnumVirtualMethodInvocationFramework01() { DoNamedTest(); }

  [Test] public void TestStructMethodGroupFramework01() { DoNamedTest(); }
  [Test] public void TestStructMethodGroupFramework02() { DoNamedTest(); }

  [Test] public void TestGenericUnboxingFramework01() { DoNamedTest(); }

  [Test] public void TestPatternMatchingFramework04() { DoNamedTest(); }
  [Test] public void TestPatternMatchingFramework05() { DoNamedTest(); }
  [Test] public void TestPatternMatchingFramework06() { DoNamedTest(); }
  [Test] public void TestPatternMatchingFramework07() { DoNamedTest(); }

  [Test] public void TestStringInterpolationFramework01() { DoNamedTest(); }

  [Test] public void TestConstrainedBoxingFramework01() { DoNamedTest(); }

  [Test] public void TestEnumHasFlagFramework01() { DoNamedTest(); }

  [Test] public void TestEnumGetHashCodeFramework01() { DoNamedTest(); }
}

[TestNet80]
public class BoxingAllocationsNetCoreTest : BoxingAllocationsTestBase
{
  [Test] public void TestTuplesAwaitForeach01() { DoNamedTest(); }
  [Test] public void TestTuplesAwaitForeach02() { DoNamedTest(); }

  [Test] public void TestEnumVirtualMethodInvocationCore01() { DoNamedTest(); }

  [Test] public void TestStructMethodGroupCore01() { DoNamedTest(); }
  [Test] public void TestStructMethodGroupCore02() { DoNamedTest(); }

  [Test] public void TestGenericUnboxingCore01() { DoNamedTest(); }

  [Test] public void TestPatternMatchingCore04() { DoNamedTest(); }
  [Test] public void TestPatternMatchingCore05() { DoNamedTest(); }

  [Test] public void TestStringInterpolationCore01() { DoNamedTest(); }
  [Test] public void TestStringInterpolationCore02() { DoNamedTest(); }

  [Test] public void TestConstrainedBoxingCore01() { DoNamedTest(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestConstrainedBoxingCore02() { DoNamedTest(); }

  [Test] public void TestEnumHasFlagCore01() { DoNamedTest(); }
  [TestSetting(typeof(HeapViewAnalysisSettings), nameof(HeapViewAnalysisSettings.OptimizationsHandling), OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled)]
  [Test] public void TestEnumHasFlagCore02() { DoNamedTest(); }

  [Test] public void TestEnumGetHashCodeCore01() { DoNamedTest(); }

  [Test] public void TestDefaultMembersCore01() { DoNamedTest(); }
  [Test] public void TestDefaultMembersCore02() { DoNamedTest(); }
  [Test] public void TestDefaultMembersCore03() { DoNamedTest(); }
}