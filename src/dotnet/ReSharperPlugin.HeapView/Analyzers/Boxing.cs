using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Conversions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.UI.RichText;
using JetBrains.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[HighlightingSource(HighlightingTypes = [typeof(Ordinary)])]
public abstract class Boxing
{
  private Boxing(ITreeNode correspondingNode)
  {
    CorrespondingNode = correspondingNode;
  }

  private ITreeNode CorrespondingNode { get; }

  protected abstract bool IsPossible { get; }
  protected virtual bool IsAnyPossible => IsPossible;
  protected virtual bool IsAllPossible => IsPossible;
  protected abstract void AppendReasons(
    RichText builder, string indent, bool presentPossible);

  public abstract void Report(IHighlightingConsumer consumer);

  [Pure]
  public static Boxing? TryFind(
    Conversion conversion, IExpressionType sourceExpressionType, IType targetType, ITreeNode correspondingNode)
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

    Boxing? RefineBoxingConversionResult()
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
            return Create(sourceExpressionType, targetType, correspondingNode, isPossible: true);
          }

          return null; // very unlikely
        }

        // target type is some concrete type, not type parameter type here
        if (!IsValueTypeOrEffectivelyTypeParameterType(sourceTypeParameterType))
        {
          // we can't be sure that the boxing will be produced at runtime
          return Create(sourceExpressionType, targetType, correspondingNode, isPossible: true);
        }
      }

      return Create(sourceExpressionType, targetType, correspondingNode);

      [Pure]
      static bool IsValueTypeOrEffectivelyTypeParameterType(IType type)
      {
        if (type.IsValueType())
          return true;

        if (type is IDeclaredType(ITypeParameter typeParameter))
        {
          var parametersInProgress = TypeParametersInProgress ??= new HashSet<ITypeParameter>(DeclaredElementEqualityComparer.TypeElementComparer);
          try
          {
            if (parametersInProgress.Add(typeParameter))
            {
              // where T : int - indirectly 'struct', can happen in type argument substitutions in type hierarchies
              foreach (var typeConstraint in typeParameter.TypeConstraints)
              {
                if (IsValueTypeOrEffectivelyTypeParameterType(typeConstraint))
                  return true;
              }
            }
          }
          finally
          {
            parametersInProgress.Remove(typeParameter);
          }
        }

        return false;
      }
    }

    Boxing? RefineUnboxingConversionResult()
    {
      var sourceType = sourceExpressionType.ToIType();

      // yep, some "unboxing" conversions do actually cause boxing at runtime
      if (sourceType != null && targetType.Classify == TypeClassification.REFERENCE_TYPE)
      {
        // value type parameter type to some random reference type
        // unconstrained type parameter type to some random reference type
        return Create(
          sourceExpressionType, targetType, correspondingNode,
          isPossible: sourceType.Classify != TypeClassification.VALUE_TYPE);
      }

      return null;
    }
  }

  [Pure]
  [StringFormatMethod(nameof(messageFormat))]
  public static Boxing Create(
    IExpressionType sourceType,
    IType targetType,
    ITreeNode correspondingNode,
    bool isPossible = false,
    string messageFormat = "conversion from '{0}' to '{1}'")
  {
    return new Ordinary(sourceType, targetType, correspondingNode, isPossible, new RichText(messageFormat));
  }

  [Pure]
  public static Boxing Create(
    IExpressionType sourceType,
    IType targetType,
    ITreeNode correspondingNode,
    bool isPossible,
    RichText messageFormat)
  {
    return new Ordinary(sourceType, targetType, correspondingNode, isPossible, messageFormat);
  }

  [ThreadStatic]
  private static HashSet<ITypeParameter>? TypeParametersInProgress;

  private static ITreeNode? TryGetComponentNode(ITreeNode nodeToHighlight, int componentIndex)
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

  [HighlightingSource(HighlightingTypes = [
      typeof(BoxingAllocationHighlighting),
      typeof(PossibleBoxingAllocationHighlighting)
    ])]
  private sealed class Ordinary : Boxing
  {
    public Ordinary(
      IExpressionType sourceExpressionType,
      IType targetType,
      ITreeNode correspondingNode,
      bool isPossible,
      RichText messageFormat)
      : base(correspondingNode)
    {
      IsPossible = isPossible;

      var sourceTypeText = sourceExpressionType.GetPresentableName(
        CorrespondingNode.Language, CommonUtils.DefaultTypePresentationStyle);
      var targetTypeText = targetType.GetPresentableName(
        CorrespondingNode.Language, CommonUtils.DefaultTypePresentationStyle);
      Reason = RichText.Format(messageFormat, sourceTypeText, targetTypeText);
    }

    public RichText Reason { get; }

    protected override bool IsPossible { get; }

    protected override void AppendReasons(RichText builder, string indent, bool presentPossible)
    {
      builder.Append(indent).Append(Reason.Capitalize());

      if (presentPossible && IsPossible)
      {
        builder.Append(" (possible boxing)");
      }

      builder.AppendLine();
    }

    public override void Report(IHighlightingConsumer consumer)
    {
      if (!IsPossible)
      {
        consumer.AddHighlighting(new BoxingAllocationHighlighting(
          CorrespondingNode, Reason + " requires boxing of the value type"));
      }
      else
      {
        consumer.AddHighlighting(new PossibleBoxingAllocationHighlighting(
          CorrespondingNode, Reason + " possibly requires boxing of the value type"));
      }
    }
  }

  [HighlightingSource]
  private sealed class InsideTupleConversion : Boxing
  {
    public InsideTupleConversion(IReadOnlyList<Boxing> componentBoxings, ITreeNode correspondingNode)
      : base(correspondingNode)
    {
      Assertion.Assert(componentBoxings.Count > 0);

      ComponentBoxings = componentBoxings;
    }

    private IReadOnlyList<Boxing> ComponentBoxings { get; }

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

    protected override void AppendReasons(RichText builder, string indent, bool presentPossible)
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
          consumer.AddHighlighting(new BoxingAllocationHighlighting(
            CorrespondingNode, new RichText(
              $"tuple component {reason} performs boxing of the value type")));
        }
        else
        {
          consumer.AddHighlighting(new PossibleBoxingAllocationHighlighting(
            CorrespondingNode, new RichText(
              $"tuple component {reason} possibly performs boxing of the value type")));
        }

        return;
      }

      var isAllPossible = IsAllPossible;

      var richText = new RichText();
      richText.Append("tuple conversion contains component type conversions that perform ");
      if (isAllPossible) richText.Append("possible ");
      richText.AppendLine("boxing of the value types");

      AppendReasons(richText, indent: "    ", presentPossible: IsAnyPossible && !isAllPossible);
      var longDescription = richText.Trim();

      if (isAllPossible)
      {
        consumer.AddHighlighting(new PossibleBoxingAllocationHighlighting(CorrespondingNode, longDescription));
      }
      else
      {
        consumer.AddHighlighting(new BoxingAllocationHighlighting(CorrespondingNode, longDescription));
      }

      return;

      bool CanUseIndividualReports()
      {
        foreach (var componentBoxing in ComponentBoxings)
        {
          if (componentBoxing.CorrespondingNode == CorrespondingNode)
            return false;
        }

        return true;
      }

      Ordinary? TryFindSingleOrdinaryBoxing()
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