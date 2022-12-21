#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(IAssignmentExpression) },
  HighlightingTypes = new[] { typeof(ObjectAllocationPossibleHighlighting) })]
public class AllocationDelegateCombineAnalyzer : HeapAllocationAnalyzerBase<IAssignmentExpression>
{
  protected override void Run(
    IAssignmentExpression assignmentExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    // += for events
    // -= for events
    // todo: possible!
  }
}