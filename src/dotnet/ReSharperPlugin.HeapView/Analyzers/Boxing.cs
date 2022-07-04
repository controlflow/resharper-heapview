using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using JetBrains.Util.DataStructures.Collections;
using JetBrains.Util.Utils;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

public abstract class Boxing
{
  private Boxing([NotNull] ITreeNode correspondingNode)
  {
    CorrespondingNode = correspondingNode;
  }

  [NotNull] public ITreeNode CorrespondingNode { get; }

  protected abstract bool IsPossible { get; }
  protected virtual bool IsAnyPossible => IsPossible;
  protected virtual bool IsAllPossible => IsPossible;
  protected abstract void AppendReasons(
    [NotNull] StringBuilder builder, [NotNull] string indent, bool presentPossible);

  public abstract void Report([NotNull] IHighlightingConsumer consumer);

  [CanBeNull, Pure]
  public static Boxing TryFind(
    Conversion conversion, [NotNull] IExpressionType sourceExpressionType, [NotNull] IType targetType, [NotNull] ITreeNode correspondingNode)
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
          var componentNode = TryGetComponentNode(correspondingNode, componentIndex) ?? correspondingNode;

          var nestedBoxing = TryFind(nested.Conversion, nested.SourceType, nested.TargetType, componentNode);
          if (nestedBoxing != null)
          {
            components.Add(nestedBoxing);
          }
        }

        if (components.Count > 0)
        {
          return new InsideTupleConversion(components.ReadOnlyList(), correspondingNode);
        }

        break;
      }

      case ConversionKind.ImplicitUserDefined:
      case ConversionKind.ExplicitUserDefined:
      {
        foreach (var nested in conversion.GetTopLevelNestedConversionsWithTypeInfo())
        {
          var nestedBoxing = TryFind(nested.Conversion, nested.SourceType, nested.TargetType, correspondingNode);
          if (nestedBoxing != null)
          {
            return nestedBoxing;
          }
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
        // TUnconstrained source;
        // TValueType source;
        Assertion.Assert(!sourceTypeParameterType.IsReferenceType());

        // type parameter type to type parameter type conversions
        if (targetType.IsTypeParameterType())
        {
          if (IsValueTypeOrEffectivelyTypeParameterType(sourceTypeParameterType))
          {
            // (TUnconstrained) valueSource; - how?
            return new Ordinary(sourceExpressionType, targetType, correspondingNode, isPossible: true);
          }

          return null; // very unlikely
        }

        // target type is some concrete type, not type parameter type here
        if (!IsValueTypeOrEffectivelyTypeParameterType(sourceTypeParameterType))
        {
          // we can't be sure that the boxing will be produced at runtime
          return new Ordinary(sourceExpressionType, targetType, correspondingNode, isPossible: true);
        }
      }

      return new Ordinary(sourceExpressionType, targetType, correspondingNode);

      [Pure]
      static bool IsValueTypeOrEffectivelyTypeParameterType([NotNull] IType type)
      {
        if (type.IsValueType())
          return true;

        if (type is IDeclaredType(ITypeParameter typeParameter))
        {
          // where T : int - indirectly 'struct', can happen in type argument substitutions in type hierarchies
          foreach (var typeConstraint in typeParameter.TypeConstraints)
          {
            if (IsValueTypeOrEffectivelyTypeParameterType(typeConstraint))
              return true;
          }
        }

        return false;
      }
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
          return new Ordinary(sourceExpressionType, targetType, correspondingNode);
        }

        // unconstrained generic to reference type
        return new Ordinary(sourceExpressionType, targetType, correspondingNode, isPossible: true);
      }

      return null;
    }
  }

  [CanBeNull]
  private static ITreeNode TryGetComponentNode([NotNull] ITreeNode nodeToHighlight, int componentIndex)
  {
    switch (nodeToHighlight)
    {
      // (object, int) t;
      // t = (1, 2);
      case ICSharpExpression sourceExpression
        when sourceExpression.GetOperandThroughParenthesis() is ITupleExpression tupleExpression:
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

      // (object a, int b) = intIntTuple;
      case ICSharpExpression sourceExpression
        when AssignmentExpressionNavigator.GetBySource(sourceExpression.GetContainingParenthesizedExpression())
          is { AssignmentType: AssignmentType.EQ, Dest: ITupleExpression tupleExpression }:
      {
        foreach (var tupleComponent in tupleExpression.ComponentsEnumerable)
        {
          if (componentIndex == 0)
          {
            if (tupleComponent is { NameIdentifier: null, Value: IDeclarationExpression { TypeUsage: { } typeUsage } })
            {
              return typeUsage;
            }

            return null;
          }

          componentIndex--;
        }

        break;
      }

      // var t = ((object, int)) intIntTuple;
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

  private sealed class Ordinary : Boxing
  {
    public Ordinary(IExpressionType sourceExpressionType, IType targetType, ITreeNode correspondingNode, bool isPossible = false)
      : base(correspondingNode)
    {
      IsPossible = isPossible;

      var sourceTypeText = sourceExpressionType.GetPresentableName(CorrespondingNode.Language, TypePresentationStyle.Default).Text;
      var targetTypeText = targetType.GetPresentableName(CorrespondingNode.Language, TypePresentationStyle.Default).Text;
      Reason = $"conversion from '{sourceTypeText}' to '{targetTypeText}'";
    }

    public string Reason { get; }

    protected override bool IsPossible { get; }

    protected override void AppendReasons(StringBuilder builder, string indent, bool presentPossible)
    {
      var line = indent + CapitalizationUtil.ForceCapitalization(Reason, CapitalizationStyle.SentenceCase);

      if (presentPossible && IsPossible)
      {
        line += " (possible boxing)";
      }

      builder.AppendLine(line);
    }

    public override void Report(IHighlightingConsumer consumer)
    {
      if (!IsPossible)
      {
        var description = Reason + " requires boxing of the value type";
        consumer.AddHighlighting(new BoxingAllocationHighlighting(CorrespondingNode, description));
      }
      else
      {
        var description = Reason + " possibly requires boxing of the value type";
        consumer.AddHighlighting(new PossibleBoxingAllocationHighlighting(CorrespondingNode, description));
      }
    }
  }

  private sealed class InsideTupleConversion : Boxing
  {
    public InsideTupleConversion([NotNull] IReadOnlyList<Boxing> componentBoxings, [NotNull] ITreeNode correspondingNode)
      : base(correspondingNode)
    {
      Assertion.Assert(componentBoxings.Count > 0);

      ComponentBoxings = componentBoxings;
    }

    [NotNull] public IReadOnlyList<Boxing> ComponentBoxings { get; }

    protected override bool IsPossible
    {
      get
      {
        foreach (var componentBoxing in ComponentBoxings)
        {
          if (!componentBoxing.IsPossible)
            return false;
        }

        return true;
      }
    }

    protected override bool IsAnyPossible
    {
      get
      {
        foreach (var componentBoxing in ComponentBoxings)
        {
          if (componentBoxing.IsAnyPossible)
            return true;
        }

        return false;
      }
    }

    protected override bool IsAllPossible
    {
      get
      {
        foreach (var componentBoxing in ComponentBoxings)
        {
          if (!componentBoxing.IsAllPossible)
            return false;
        }

        return true;
      }
    }

    protected override void AppendReasons(StringBuilder builder, string indent, bool presentPossible)
    {
      foreach (var componentBoxing in ComponentBoxings)
      {
        componentBoxing.AppendReasons(builder, indent, presentPossible);
      }
    }

    public override void Report(IHighlightingConsumer consumer)
    {
      if (CanUseIndividualReports())
      {
        foreach (var componentBoxing in ComponentBoxings)
        {
          componentBoxing.Report(consumer);
        }

        return;
      }

      var singleBoxing = TryFindSingleOrdinaryBoxing();
      if (singleBoxing != null)
      {
        var reason = singleBoxing.Reason;

        if (!singleBoxing.IsPossible)
        {
          var description = $"tuple component {reason} performs boxing of the value type";
          consumer.AddHighlighting(new BoxingAllocationHighlighting(CorrespondingNode, description));
        }
        else
        {
          var description = $"tuple component {reason} possibly performs boxing of the value type";
          consumer.AddHighlighting(new PossibleBoxingAllocationHighlighting(CorrespondingNode, description));
        }

        return;
      }

      var isAllPossible = IsAllPossible;

      using var builder = PooledStringBuilder.GetInstance();
      builder.Append("tuple conversion contains component type conversions that perform ");
      if (isAllPossible) builder.Append("possible ");
      builder.AppendLine("boxing of the value types");

      AppendReasons(builder.Builder, indent: "    ", presentPossible: IsAnyPossible && !isAllPossible);
      var longDescription = builder.ToString().Trim();

      if (isAllPossible)
      {
        consumer.AddHighlighting(new PossibleBoxingAllocationHighlighting(CorrespondingNode, longDescription));
      }
      else
      {
        consumer.AddHighlighting(new BoxingAllocationHighlighting(CorrespondingNode, longDescription));
      }

      bool CanUseIndividualReports()
      {
        foreach (var componentBoxing in ComponentBoxings)
        {
          if (componentBoxing.CorrespondingNode == CorrespondingNode)
            return false;
        }

        return true;
      }

      [CanBeNull]
      Ordinary TryFindSingleOrdinaryBoxing()
      {
        var boxing = ComponentBoxings.SingleItem();
        while (boxing is InsideTupleConversion innerTupleBoxings)
        {
          boxing = innerTupleBoxings.ComponentBoxings.SingleItem();
        }

        return boxing as Ordinary;
      }
    }
  }
}