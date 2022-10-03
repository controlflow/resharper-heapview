#nullable enable
using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Settings;
using JetBrains.Util;

namespace ReSharperPlugin.HeapView.Settings;

[SettingsKey(typeof(CodeInspectionSettings), "HeapView Plugin settings")]
public class HeapViewAnalysisSettings
{
  [SettingsEntry(OptimizationsHandling.AnalyzeAssumingOptimizationsAreEnabled, "Optimizations handling")]
  public OptimizationsHandling OptimizationsHandling;
}

internal static class HeapViewAnalysisSettingsExtensions
{
  private static readonly Expression<Func<HeapViewAnalysisSettings, OptimizationsHandling>> OptimizationSettingKey = s => s.OptimizationsHandling;

  private static readonly Key<object> AnalyzeIfOptimizationsAreEnabledKey = new(nameof(AnalyzeIfOptimizationsAreEnabledKey));

  [Pure]
  public static bool AnalyzeCodeLikeIfOptimizationsAreEnabled(this ElementProblemAnalyzerData data)
  {
    return (bool) data.GetOrCreateDataUnderLock(AnalyzeIfOptimizationsAreEnabledKey, data, static data =>
    {
      switch (data.SettingsStore.GetValue(OptimizationSettingKey))
      {
        case OptimizationsHandling.AnalyzeAssumingOptimizationsAreEnabled:
        {
          return BooleanBoxes.True;
        }

        case OptimizationsHandling.AnalyzeAssumingOptimizationsAreDisabled:
        {
          return BooleanBoxes.False;
        }

        case OptimizationsHandling.UseProjectSettings:
        {
          var sourceFile = data.SourceFile;
          if (sourceFile == null) return BooleanBoxes.False;

          var project = sourceFile.GetProject();
          if (project == null) return BooleanBoxes.False;

          var configuration = project.ProjectProperties.TryGetConfiguration<ICSharpProjectConfiguration>(sourceFile.PsiModule.TargetFrameworkId);
          if (configuration == null) return BooleanBoxes.False;

          return configuration.Optimize ? BooleanBoxes.True : BooleanBoxes.False;
        }

        default:
        {
          throw new ArgumentOutOfRangeException();
        }
      }
    });
  }
}