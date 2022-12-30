using System;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

// TODO: [ReSharper] fix RSRP-490846

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(IInterpolatedStringExpression) },
  HighlightingTypes = new[] { typeof(ObjectAllocationHighlighting) })]
public class AllocationOfStringInterpolationAnalyzer : HeapAllocationAnalyzerBase<IInterpolatedStringExpression>
{
  protected override void Run(
    IInterpolatedStringExpression interpolatedStringExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (interpolatedStringExpression.IsInTheContextWhereAllocationsAreNotImportant())
      return;

    if (IsConcatenatedContinuationOfInterpoledString(interpolatedStringExpression))
      return; // allocations reported before

    var concatenations = UnwrapFromContainingInterpolationConcatentations(interpolatedStringExpression);

    var highlighting = TryFindAllocationInConcatenation(concatenations);
    if (highlighting != null)
    {
      consumer.AddHighlighting(highlighting, GetRangeToHighlight(interpolatedStringExpression));
    }

    IHighlighting? TryFindAllocationInConcatenation(ICSharpExpression expression)
    {
      switch (expression.GetOperandThroughParenthesis())
      {
        case IInterpolatedStringExpression interpolation:
          return TryFindAllocation(interpolation, data);

        case IAdditiveExpression { OperatorReference: (InterpolatedStringConcatenationOperator, _) } concatenation:
          return TryFindAllocationInConcatenation(concatenation.LeftOperand)
                 ?? TryFindAllocationInConcatenation(concatenation.RightOperand);

        default:
          return null;
      }
    }
  }

  private static IHighlighting? TryFindAllocation(IInterpolatedStringExpression interpolatedStringExpression, ElementProblemAnalyzerData data)
  {
    switch (interpolatedStringExpression.GetInterpolatedStringKind())
    {
      case InterpolatedStringKind.String:
        return TryReportOrdinaryStringFormatting(interpolatedStringExpression);
      case InterpolatedStringKind.FormattableString:
        return TryReportFormattableStringAllocations(interpolatedStringExpression);
      case InterpolatedStringKind.InterpolatedStringHandler:
        return ReportInterpolatedStringHandlerAllocations(interpolatedStringExpression, data);
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  private static IHighlighting? TryReportOrdinaryStringFormatting(IInterpolatedStringExpression interpolatedStringExpression)
  {
    if (interpolatedStringExpression.InsertsEnumerable.IsEmpty())
      return null; // compiled into ordinary string

    if (interpolatedStringExpression.IsConstantValue())
      return null; // C# 10 constant interpolation expressions

    var hint = CanCompileToStringConcat(interpolatedStringExpression) ? " ('String.Concat' method call)" : null;
    return new ObjectAllocationHighlighting(interpolatedStringExpression, $"new 'String' instance creation{hint}");
  }

  private static IHighlighting? TryReportFormattableStringAllocations(IInterpolatedStringExpression interpolatedStringExpression)
  {
    var resolveResult = interpolatedStringExpression.FormatReference.Resolve();
    if (resolveResult is not { DeclaredElement: IMethod method, ResolveErrorType.IsAcceptable: true }) return null;

    var returnType = resolveResult.Substitution[method.ReturnType];
    var interpolationType = returnType.GetPresentableName(interpolatedStringExpression.Language, CommonUtils.DefaultTypePresentationStyle);

    var paramsArrayHint = (string?)null;

    if (interpolatedStringExpression.InsertsEnumerable.Any()
        && method.Parameters is { Count: > 1 } parameters
        && parameters[^1].IsParameterArray)
    {
      paramsArrayHint = ", allocates parameter array";
    }

    return new ObjectAllocationHighlighting(interpolatedStringExpression, $"new '{interpolationType}' instance creation{paramsArrayHint}");
  }

  private static IHighlighting? ReportInterpolatedStringHandlerAllocations(
    IInterpolatedStringExpression interpolatedStringExpression, ElementProblemAnalyzerData data)
  {
    var resolveResult = interpolatedStringExpression.HandlerConstructorReference.Resolve();
    if (resolveResult is not { DeclaredElement: IConstructor { ContainingType: { } interpolatedStringHandlerTypeElement }, ResolveErrorType.IsAcceptable: true })
      return null;

    var predefinedType = data.GetPredefinedType();
    if (interpolatedStringHandlerTypeElement.Equals(predefinedType.DefaultInterpolatedStringHandler.GetTypeElement()))
    {
      return new ObjectAllocationHighlighting(interpolatedStringExpression, "new 'String' instance creation");
    }

    if (interpolatedStringHandlerTypeElement is IClass classTypeElement)
    {
      var handlerTypeText = TypeFactory.CreateType(classTypeElement)
        .GetPresentableName(interpolatedStringExpression.Language, CommonUtils.DefaultTypePresentationStyle);

      return new ObjectAllocationHighlighting(interpolatedStringExpression, $"new '{handlerTypeText}' interpolated string handler instance creation");
    }

    return null;
  }

  [Pure]
  private static bool IsConcatenatedContinuationOfInterpoledString(IInterpolatedStringExpression interpolatedStringExpression)
  {
    var currentExpression = interpolatedStringExpression.GetContainingParenthesizedExpression();

    while (AdditiveExpressionNavigator.GetByLeftOperand(currentExpression) is { OperatorReference: (InterpolatedStringConcatenationOperator, _) } byLeft)
    {
      currentExpression = byLeft.GetContainingParenthesizedExpression();
    }

    var byRight = AdditiveExpressionNavigator.GetByRightOperand(currentExpression);
    return byRight is { OperatorReference: (InterpolatedStringConcatenationOperator, _) };
  }

  [Pure]
  private static ICSharpExpression UnwrapFromContainingInterpolationConcatentations(IInterpolatedStringExpression interpolatedStringExpression)
  {
    ICSharpExpression currentExpression = interpolatedStringExpression;

    while (currentExpression.GetContainingParenthesizedExpression() is { } containingExpression
           && (AdditiveExpressionNavigator.GetByLeftOperand(containingExpression)
               ?? AdditiveExpressionNavigator.GetByRightOperand(containingExpression))
               is { OperatorReference: (InterpolatedStringConcatenationOperator, _) } interpolationConcatenation)
    {
      currentExpression = interpolationConcatenation;
    }

    return currentExpression;
  }

  [Pure]
  private static bool CanCompileToStringConcat(IInterpolatedStringExpression interpolatedStringExpression)
  {
    var expression = UnwrapFromContainingInterpolationConcatentations(interpolatedStringExpression);
    return CheckAllInterpolations(expression);

    [Pure]
    static bool CheckAllInterpolations(ICSharpExpression expressionToCheck)
    {
      switch (expressionToCheck.GetOperandThroughParenthesis())
      {
        case IInterpolatedStringExpression interpolatedStringExpression:
        {
          foreach (var insert in interpolatedStringExpression.InsertsEnumerable)
          {
            if (insert.AlignmentExpression != null) return false;
            if (insert.FormatSpecifier != null) return false;

            var insertExpression = insert.Expression;
            if (insertExpression == null) return false;

            var insertType = insertExpression.GetExpressionType();
            if (!insertType.ToIType().IsString()) return false;
          }

          return true;
        }

        case IAdditiveExpression { OperatorReference: (InterpolatedStringConcatenationOperator, _) } concatenation:
        {
          return CheckAllInterpolations(concatenation.LeftOperand)
              && CheckAllInterpolations(concatenation.RightOperand);
        }

        default: return false;
      }
    }
  }

  [Pure]
  private static DocumentRange GetRangeToHighlight(IInterpolatedStringExpression interpolatedStringExpression)
  {
    var firstToken = interpolatedStringExpression.LiteralsEnumerable.FirstOrDefault();
    if (firstToken == null) return DocumentRange.InvalidRange;

    var tokenText = firstToken.GetText();
    var index = 0;

    while (index < tokenText.Length && tokenText[index] is '@' or '$') index++;
    while (index < tokenText.Length && tokenText[index] == '"') index++;

    return firstToken.GetDocumentStartOffset().ExtendRight(index);
  }
}