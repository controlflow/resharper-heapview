using System;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.IDE.UI.Options;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Daemon.SolutionAnalysis.Resources;
using JetBrains.ReSharper.Feature.Services.Daemon.OptionPages;

namespace ReSharperPlugin.HeapView.Settings;

[OptionsPage(
  id: "HeapViewer",
  name: "Heap Allocations Viewer",
  typeofIcon: typeof(SolutionAnalysisThemedIcons.FindSimilarIssues),
  Sequence = 10,
  ParentId = CodeInspectionPage.PID)]
public class HeapViewSettingsPage : BeSimpleOptionsPage
{
  public HeapViewSettingsPage(
    Lifetime lifetime,
    OptionsPageContext optionsPageContext,
    OptionsSettingsSmartContext optionsSettingsSmartContext,
    bool wrapInScrollablePanel = false)
    : base(lifetime, optionsPageContext, optionsSettingsSmartContext, wrapInScrollablePanel)
  {
    AddBoolOption((HeapViewAnalysisSettings x) => x.AnalysisIsEnabled, "Enable allocation analysis");

    AddSpacer();

    AddText("Some heap allocations are eliminated by the .NET JIT compiler, " +
            "but only for 'Release' build configurations with \"Optimize code\" setting enabled. " +
            "Heap Allocations Viewer can analyze your code assuming different optimization settings.");
    AddComboEnum(
      (HeapViewAnalysisSettings x) => x.OptimizationsHandling,
      caption: "Code analysis mode:",
      getPresentation: handling =>
      {
        return handling switch
        {
          OptimizationsHandling.AnalyzeAssumingOptimizationsAreEnabled
            => "I only care about heap allocations in the optimized code",
          OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled
            => "I care about heap allocations in builds without optimizations enabled",
          OptimizationsHandling.UseProjectSettings
            => "Use the current project build configuration \"Optimize code\" setting",
          _
            => throw new ArgumentOutOfRangeException(nameof(handling), handling, null)
        };
      });
  }
}