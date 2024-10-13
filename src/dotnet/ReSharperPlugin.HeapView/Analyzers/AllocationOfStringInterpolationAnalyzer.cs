using System;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.UI.RichText;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: [ typeof(IInterpolatedStringExpression) ],
  HighlightingTypes = [ typeof(ObjectAllocationHighlighting) ])]
public class AllocationOfStringInterpolationAnalyzer : HeapAllocationAnalyzerBase<IInterpolatedStringExpression>
{
  protected override void Run(
    IInterpolatedStringExpression interpolatedStringExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (interpolatedStringExpression.IsInTheContextWhereAllocationsAreNotImportant())
      return;

    if (IsConcatenatedContinuationOfInterpolatedString(interpolatedStringExpression))
      return; // allocations reported before

    var concatenations = UnwrapFromContainingInterpolationConcatenations(interpolatedStringExpression);

    var highlighting = TryFindAllocationInConcatenation(concatenations);
    if (highlighting != null)
    {
      consumer.AddHighlighting(highlighting, GetRangeToHighlight(interpolatedStringExpression));
    }

    return;

    IHighlighting? TryFindAllocationInConcatenation(ICSharpExpression expression)
    {
      switch (expression.GetOperandThroughParenthesis())
      {
        case IInterpolatedStringExpression interpolation:
        {
          return TryFindAllocation(interpolation, data);
        }

        case IAdditiveExpression concatenation
          when concatenation.OperatorReference?.Resolve().DeclaredElement is InterpolatedStringConcatenationOperator:
        {
          return TryFindAllocationInConcatenation(concatenation.LeftOperand)
                 ?? TryFindAllocationInConcatenation(concatenation.RightOperand);
        }

        default:
          return null;
      }
    }
  }

  private static IHighlighting? TryFindAllocation(
    IInterpolatedStringExpression interpolatedStringExpression, ElementProblemAnalyzerData data)
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

    var typeStyle = DeclaredElementPresenterTextStyles.Generic[DeclaredElementPresentationPartKind.Type];

    var richText = new RichText();

    richText.Append("new '");
    richText.Append(nameof(String), typeStyle);
    richText.Append("' instance creation");

    if (CanCompileToStringConcat(interpolatedStringExpression))
    {
      var methodStyle = DeclaredElementPresenterTextStyles.Generic[DeclaredElementPresentationPartKind.Method];
      richText
        .Append(" ('")
        .Append(nameof(String), typeStyle)
        .Append('.')
        .Append(nameof(string.Concat), methodStyle)
        .Append("' method call)");
    }

    return new ObjectAllocationHighlighting(interpolatedStringExpression, richText);
  }

  private static IHighlighting? TryReportFormattableStringAllocations(IInterpolatedStringExpression interpolatedStringExpression)
  {
    var resolveResult = interpolatedStringExpression.FormatReference.Resolve();
    if (resolveResult is not { DeclaredElement: IMethod method, ResolveErrorType.IsAcceptable: true }) return null;

    var returnType = resolveResult.Substitution[method.ReturnType];
    var interpolationType = returnType.GetPresentableName(
      interpolatedStringExpression.Language, CommonUtils.DefaultTypePresentationStyle);

    string? paramsArrayHint = null;

    if (interpolatedStringExpression.InsertsEnumerable.Any()
        && method.Parameters is { Count: > 1 } parameters
        && parameters[^1].IsParameterArray)
    {
      paramsArrayHint = ", allocates parameter array";
    }

    return new ObjectAllocationHighlighting(
      interpolatedStringExpression, new RichText(
        $"new '{interpolationType}' instance creation{paramsArrayHint}"));
  }

  private static IHighlighting? ReportInterpolatedStringHandlerAllocations(
    IInterpolatedStringExpression interpolatedStringExpression, ElementProblemAnalyzerData data)
  {
    var resolveResult = interpolatedStringExpression.HandlerConstructorReference.Resolve();
    if (resolveResult is not
        {
          DeclaredElement: IConstructor { ContainingType: { } interpolatedStringHandlerTypeElement },
          ResolveErrorType.IsAcceptable: true
        })
      return null;

    var predefinedType = data.GetPredefinedType();
    if (interpolatedStringHandlerTypeElement.Equals(predefinedType.DefaultInterpolatedStringHandler.GetTypeElement()))
    {
      var richText = new RichText();

      richText.Append("new '");
      richText.Append(nameof(String),
        DeclaredElementPresenterTextStyles.Generic[DeclaredElementPresentationPartKind.Type]);
      richText.Append("' instance creation");

      return new ObjectAllocationHighlighting(interpolatedStringExpression, richText);
    }

    if (interpolatedStringHandlerTypeElement is IClass classTypeElement)
    {
      var handlerTypeText = TypeFactory.CreateType(classTypeElement)
        .GetPresentableName(interpolatedStringExpression.Language, CommonUtils.DefaultTypePresentationStyle);

      return new ObjectAllocationHighlighting(
        interpolatedStringExpression,
        new RichText($"new '{handlerTypeText}' interpolated string handler instance creation"));
    }

    return null;
  }

  [Pure]
  private static bool IsConcatenatedContinuationOfInterpolatedString(IInterpolatedStringExpression interpolatedStringExpression)
  {
    var currentExpression = interpolatedStringExpression.GetContainingParenthesizedExpression();

    while (AdditiveExpressionNavigator.GetByLeftOperand(currentExpression) is { } byLeft
           && byLeft.OperatorReference?.Resolve().DeclaredElement is InterpolatedStringConcatenationOperator)
    {
      currentExpression = byLeft.GetContainingParenthesizedExpression();
    }

    var byRight = AdditiveExpressionNavigator.GetByRightOperand(currentExpression);
    return byRight?.OperatorReference.Resolve().DeclaredElement is InterpolatedStringConcatenationOperator;
  }

  [Pure]
  private static ICSharpExpression UnwrapFromContainingInterpolationConcatenations(IInterpolatedStringExpression interpolatedStringExpression)
  {
    ICSharpExpression currentExpression = interpolatedStringExpression;

    while (currentExpression.GetContainingParenthesizedExpression() is { } containingExpression
           && (AdditiveExpressionNavigator.GetByLeftOperand(containingExpression)
               ?? AdditiveExpressionNavigator.GetByRightOperand(containingExpression)) is { } interpolationConcatenation
           && interpolationConcatenation.OperatorReference?.Resolve().DeclaredElement is InterpolatedStringConcatenationOperator)
    {
      currentExpression = interpolationConcatenation;
    }

    return currentExpression;
  }

  [Pure]
  private static bool CanCompileToStringConcat(IInterpolatedStringExpression interpolatedStringExpression)
  {
    var expression = UnwrapFromContainingInterpolationConcatenations(interpolatedStringExpression);
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

        case IAdditiveExpression concatenation
          when concatenation.OperatorReference?.Resolve().DeclaredElement is InterpolatedStringConcatenationOperator:
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