#nullable enable
using System;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Managed;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[]
  {
    typeof(IAssignmentExpression),
    typeof(IForeachStatement)
  },
  HighlightingTypes = new[]
  {
    typeof(BoxingAllocationHighlighting),
    typeof(PossibleBoxingAllocationHighlighting)
  })]
public class BoxingInDeconstructionAnalyzer : HeapAllocationAnalyzerBase<ITreeNode>
{
  protected override void Run(ITreeNode element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    switch (element)
    {
      // (object o, objVariable) = intIntTuple;
      case IAssignmentExpression assignmentExpression:
        CheckDeconstructingAssignmentImplicitConversions(assignmentExpression, data, consumer);
        break;

      // foreach ((object o, _) in arrayOfIntIntTuples) { }
      case IForeachStatement { ForeachHeader.DeconstructionTuple: { } } foreachStatement:
        CheckForeachImplicitConversions(foreachStatement, data, consumer);
        break;
    }
  }

  private static void CheckDeconstructingAssignmentImplicitConversions(
    IAssignmentExpression assignmentExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    switch (assignmentExpression.GetAssignmentKind())
    {
      case AssignmentKind.OrdinaryAssignment:
        return;

      // all kinds of deconstructions
      case AssignmentKind.DeconstructingAssignment:
      case AssignmentKind.DeconstructingDeclaration:
      case AssignmentKind.DeconstructionMixed:
        break;

      default:
        throw new ArgumentOutOfRangeException();
    }

    var targetTupleExpression = assignmentExpression.Dest as ITupleExpression;
    if (targetTupleExpression == null) return;

    UniversalContext? resolveContext = null;
    CheckImplicitConversionsInDeconstruction(targetTupleExpression, ref resolveContext, data, consumer);
  }

  private static void CheckForeachImplicitConversions(
    IForeachStatement foreachStatement, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (foreachStatement.ForeachHeader is { DeconstructionTuple: { } targetTupleExpression })
    {
      UniversalContext? resolveContext = null;
      CheckImplicitConversionsInDeconstruction(targetTupleExpression, ref resolveContext, data, consumer);
    }
  }

  private static void CheckImplicitConversionsInDeconstruction(
    ITupleExpression targetTupleExpression, ref UniversalContext? universalContext, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    foreach (var tupleComponent in targetTupleExpression.ComponentsEnumerable)
    {
      switch (tupleComponent.Value)
      {
        // ((a, b), _) = e;
        case ITupleExpression innerTupleExpression:
        {
          CheckImplicitConversionsInDeconstruction(innerTupleExpression, ref universalContext, data, consumer);
          break;
        }

        // (_, _) = e;           - discards elimiate access to component
        // (object _, _) = e;    - discard designations elimiate access as well
        // (var a, _) = e;       - source type captured, no conversion
        // (var (a, b), _) = e;  - source type deconstructed, no conversion
        case IReferenceExpression discardReferenceExpression when discardReferenceExpression.IsDiscardReferenceExpression():
        case IDeclarationExpression { Designation: IDiscardDesignation }:
        case IDeclarationExpression { TypeUsage: null }:
        {
          break;
        }

        // (a, _) = e;
        // (object o, _) = e;
        case { IsLValue: true } lValueExpression:
        {
          var targetComponentType = lValueExpression.GetExpressionType().ToIType();
          if (targetComponentType == null) continue;

          universalContext ??= new UniversalContext(targetTupleExpression);

          var sourceExpressionType = targetTupleExpression.GetComponentSourceExpressionType(tupleComponent, universalContext);

          ITreeNode correspondingNode = lValueExpression is IDeclarationExpression declarationExpression
            ? declarationExpression.TypeUsage.NotNull()
            : lValueExpression;

          BoxingInExpressionConversionsAnalyzer.CheckConversionRequiresBoxing(
            sourceExpressionType, targetComponentType, correspondingNode,
            static (conversionRule, source, target) => conversionRule.ClassifyImplicitConversionFromExpression(source, target),
            data, consumer);
          break;
        }
      }
    }
  }
}