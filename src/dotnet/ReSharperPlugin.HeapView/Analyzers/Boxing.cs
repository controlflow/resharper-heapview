using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Conversions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

public abstract class Boxing
{
  protected Boxing([NotNull] ITreeNode correspondingNode)
  {
    CorrespondingNode = correspondingNode;
  }

  [NotNull] public ITreeNode CorrespondingNode { get; }

  public abstract void Report([NotNull] IHighlightingConsumer consumer);

  [CanBeNull, Pure]
  public static Boxing TryFind(
    Conversion conversion, [NotNull] IExpressionType sourceExpressionType, [NotNull] IType targetType, [NotNull] ITreeNode correspondingTreeNode)
  {
    switch (conversion.Kind)
    {
      case ConversionKind.Boxing:
      {
        return RefineBoxingConversionResult();
      }

      case ConversionKind.Unboxing:
      {
        return RefineUnboxingConversionResult();
      }

      case ConversionKind.ImplicitTuple:
      case ConversionKind.ImplicitTupleLiteral:
      case ConversionKind.ExplicitTuple:
      case ConversionKind.ExplicitTupleLiteral:
      {
        var components = new LocalList<Boxing>();

        foreach (var (nested, componentIndex) in conversion.GetTopLevelNestedConversionsWithTypeInfo().WithIndexes())
        {
          var componentNode = TryGetComponentNode(correspondingTreeNode, componentIndex) ?? correspondingTreeNode;

          var nestedBoxing = TryFind(nested.Conversion, nested.SourceType, nested.TargetType, componentNode);
          if (nestedBoxing != null)
          {
            components.Add(nestedBoxing);
          }
        }

        if (components.Count > 0)
        {
          return new InsideTupleConversion(components.ReadOnlyList(), correspondingTreeNode);
        }

        break;
      }
    }

    return null;

    [CanBeNull]
    Boxing RefineBoxingConversionResult()
    {
      var sourceType = sourceExpressionType.ToIType();
      if (sourceType is IDeclaredType (ITypeParameter, _) sourceTypeParameterType)
      {
        Assertion.Assert(!sourceTypeParameterType.IsReferenceType());

        if (targetType.IsTypeParameterType())
        {
          if (sourceTypeParameterType.IsValueType())
            return new Ordinary(sourceExpressionType, targetType, correspondingTreeNode, isPossible: true);

          return null; // very unlikely
        }

        if (!sourceTypeParameterType.IsValueType())
        {
          return new Ordinary(sourceExpressionType, targetType, correspondingTreeNode, isPossible: true);
        }
      }

      return new Ordinary(sourceExpressionType, targetType, correspondingTreeNode);
    }

    [CanBeNull]
    Boxing RefineUnboxingConversionResult()
    {
      var sourceType = sourceExpressionType.ToIType();

      // yep, some "unboxing" conversions do actually cause boxing at runtime
      if (sourceType != null && targetType.Classify == TypeClassification.REFERENCE_TYPE)
      {
        // value type to reference type
        if (sourceType.Classify == TypeClassification.VALUE_TYPE)
        {
          return new Ordinary(sourceExpressionType, targetType, correspondingTreeNode);
        }

        // unconstrained generic to reference type
        return new Ordinary(sourceExpressionType, targetType, correspondingTreeNode, isPossible: true);
      }

      return null;
    }

    [CanBeNull]
    Boxing CheckNestedConversions()
    {
      return null;
    }
  }

  [CanBeNull]
  private static ITreeNode TryGetComponentNode([NotNull] ITreeNode nodeToHighlight, int componentIndex)
  {
    switch (nodeToHighlight)
    {
      case ICSharpExpression expressionToHighlight
        when expressionToHighlight.GetOperandThroughParenthesis() is ITupleExpression tupleExpression:
      {
        foreach (var tupleComponent in tupleExpression.ComponentsEnumerable)
        {
          if (componentIndex == 0)
          {
            return tupleComponent.Value;
          }

          componentIndex--;
        }

        break;
      }

      case ITupleTypeUsage tupleTypeUsage:
      {
        foreach (var tupleTypeComponent in tupleTypeUsage.ComponentsEnumerable)
        {
          if (componentIndex == 0)
          {
            return tupleTypeComponent?.TypeUsage;
          }

          componentIndex--;
        }

        break;
      }
    }

    return null;
  }

  public sealed class Ordinary : Boxing
  {
    public bool IsPossible { get; }
    public string Reason { get; }

    public Ordinary(IExpressionType sourceExpressionType, IType targetType, ITreeNode correspondingNode, bool isPossible = false)
      : base(correspondingNode)
    {
      IsPossible = isPossible;

      var sourceTypeText = sourceExpressionType.GetPresentableName(CorrespondingNode.Language, TypePresentationStyle.Default).Text;
      var targetTypeText = targetType.GetPresentableName(CorrespondingNode.Language, TypePresentationStyle.Default).Text;
      Reason = $"conversion from '{sourceTypeText}' to '{targetTypeText}'";
    }

    public override void Report(IHighlightingConsumer consumer)
    {
      var documentRange = CorrespondingNode is ICSharpExpression expression ? expression.GetExpressionRange() : CorrespondingNode.GetDocumentRange();

      if (!IsPossible)
      {
        var description = Reason + " requires boxing of the value type";
        consumer.AddHighlighting(new BoxingAllocationHighlighting(CorrespondingNode, description), documentRange);
      }
      else
      {
        var description = Reason + " possibly requires boxing of the value type";
        consumer.AddHighlighting(new PossibleBoxingAllocationHighlighting(CorrespondingNode, description), documentRange);
      }
    }
  }

  public sealed class InsideTupleConversion : Boxing
  {
    public InsideTupleConversion([NotNull] IReadOnlyList<Boxing> componentBoxings, [NotNull] ITreeNode correspondingNode)
      : base(correspondingNode)
    {
      ComponentBoxings = componentBoxings;
    }

    [NotNull] public IReadOnlyList<Boxing> ComponentBoxings { get; }


    public override void Report(IHighlightingConsumer consumer)
    {
      var canUseIndividualReports = true;

      foreach (var componentBoxing in ComponentBoxings)
      {
        if (componentBoxing.CorrespondingNode == CorrespondingNode)
        {
          canUseIndividualReports = false;
          break;
        }
      }

      if (canUseIndividualReports)
      {
        foreach (var componentBoxing in ComponentBoxings)
        {
          componentBoxing.Report(consumer);
        }
      }
      else
      {

      }
    }
  }
}