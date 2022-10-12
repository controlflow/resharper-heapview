#nullable enable
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.HeapView.Highlightings;
using ReSharperPlugin.HeapView.Settings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[]
  {
    typeof(IConstantOrTypePattern),
    typeof(IRecursivePattern),
    typeof(IReferenceExpression)
  },
  HighlightingTypes = new[]
  {
    typeof(PossibleBoxingAllocationHighlighting)
  })]
public class BoxingInNullChecksAnalyzer : HeapAllocationAnalyzerBase<ITreeNode>
{
  protected override bool ShouldRun(IFile file, ElementProblemAnalyzerData data)
  {
    // note: unconstrained value null checks only produce allocations in DEBUG builds
    return !data.AnalyzeCodeLikeIfOptimizationsAreEnabled();
  }

  protected override void Run(ITreeNode element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    switch (element)
    {
      // tUnconstrained is null
      // tUnconstrained is not null
      case IConstantOrTypePattern (ConstantOrTypePatternKind.ConstantValueCheck) constantPattern:
        CheckUnconstrainedNullCheckInPattern(constantPattern, consumer);
        break;

      // tUnconstrained is { }
      // note: list & deconstructions patterns are very unlikely over unconstrained generics
      case IRecursivePattern { TypeUsage: null } recursivePattern when recursivePattern.ChecksInputForNull():
        CheckUnconstrainedNullCheckInPattern(recursivePattern, consumer);
        break;

      // x is { Unconstrained.Property: { } }
      case IReferenceExpression { QualifierExpression: { } } referenceExpression when referenceExpression.IsSubpatternMemberAccessPart():
        CheckUnconstrainedNullCheckInSubpattern(referenceExpression, consumer);
        break;
    }
  }

  private static void CheckUnconstrainedNullCheckInPattern(IConstantOrTypePattern constantPattern, IHighlightingConsumer consumer)
  {
    if (!constantPattern.ConstantValue.IsNull()) return;

    var sourceType = constantPattern.GetDispatchType();
    if (!sourceType.IsUnconstrainedGenericType()) return;

    ReportUnconstrainedBoxing(sourceType, constantPattern, consumer);
  }

  private static void CheckUnconstrainedNullCheckInPattern(IRecursivePattern recursivePattern, IHighlightingConsumer consumer)
  {
    var sourceType = recursivePattern.GetDispatchType();
    if (!sourceType.IsUnconstrainedGenericType()) return;

    var patternNode = recursivePattern.DeconstructionPatternClause?.LPar
                      ?? recursivePattern.PropertyPatternClause?.LBrace
                      ?? (ITreeNode) recursivePattern;

    ReportUnconstrainedBoxing(sourceType, patternNode, consumer);
  }

  private static void CheckUnconstrainedNullCheckInSubpattern(
    IReferenceExpression subpatternAccessReferenceExpression, IHighlightingConsumer consumer)
  {
    var nameIdentifier = subpatternAccessReferenceExpression.NameIdentifier;
    if (nameIdentifier == null) return;

    var qualifierExpression = subpatternAccessReferenceExpression.QualifierExpression.NotNull();

    var qualifierType = qualifierExpression.GetExpressionType().ToIType();
    if (qualifierType == null) return;

    if (!qualifierType.IsUnconstrainedGenericType()) return;

    ReportUnconstrainedBoxing(qualifierType, nameIdentifier, consumer);
  }

  private static void ReportUnconstrainedBoxing(IType sourceType, ITreeNode patternNode, IHighlightingConsumer consumer)
  {
    if (patternNode.IsInTheContextWhereAllocationsAreNotImportant()) return;

    var boxing = Boxing.Create(
      sourceType, targetType: sourceType, patternNode, isPossible: true,
      messageFormat: "checking the value of unconstrained generic type '{0}' for 'null'");

    boxing.Report(consumer);
  }
}