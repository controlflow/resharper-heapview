#nullable enable
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Resolve;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

// TODO: Delegate.Combine/Remove/RemoveAll APIs support

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(IAssignmentExpression), typeof(IAdditiveExpression) },
  HighlightingTypes = new[] { typeof(ObjectAllocationPossibleHighlighting) })]
public class AllocationOfDelegateOperatorsAnalyzer : HeapAllocationAnalyzerBase<IOperatorExpression>
{
  protected override void Run(
    IOperatorExpression operatorExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var resolveResult = operatorExpression.OperatorReference?.Resolve().Result;
    if (resolveResult == null) return;

    if (resolveResult is (DelegatePredefinedOperator predefinedOperator, _))
    {
      var operationName = predefinedOperator.ShortName == StandardOperatorNames.Addition ? "addition" : "removal";

      consumer.AddHighlighting(new ObjectAllocationPossibleHighlighting(
        operatorExpression.OperatorSign, $"delegate {operationName} operation may allocate new delegate instance"));
    }
    else if (resolveResult is EventSubscriptionResolveResult (IAccessor { Kind: var accessorKind }, _))
    {
      var operationName = accessorKind == AccessorKind.ADDER ? "subscription" : "unsubscription";

      consumer.AddHighlighting(new ObjectAllocationPossibleHighlighting(
        operatorExpression.OperatorSign, $"event {operationName} may allocate new delegate instance"));
    }
  }
}