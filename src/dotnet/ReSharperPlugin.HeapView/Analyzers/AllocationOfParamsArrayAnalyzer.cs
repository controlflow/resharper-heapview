#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(IReferenceExpression) },
  HighlightingTypes = new[] { typeof(DelegateAllocationHighlighting) })]
public class AllocationOfParamsArrayAnalyzer : HeapAllocationAnalyzerBase<IReferenceExpression>
{
  protected override void Run(
    IReferenceExpression referenceExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {

  }
}