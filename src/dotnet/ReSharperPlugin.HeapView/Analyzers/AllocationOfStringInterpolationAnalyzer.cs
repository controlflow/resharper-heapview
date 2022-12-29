#nullable enable
using System;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

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

    switch (interpolatedStringExpression.GetInterpolatedStringKind())
    {
      case InterpolatedStringKind.String:
        ReportOrdinaryStringFormatting(interpolatedStringExpression, consumer);
        break;

      case InterpolatedStringKind.FormattableString:
        ReportFormattableStringAllocations(interpolatedStringExpression, consumer);
        break;

      case InterpolatedStringKind.InterpolatedStringHandler:
        ReportInterpolatedStringHandlerAllocations(interpolatedStringExpression, data, consumer);
        break;

      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  private static void ReportOrdinaryStringFormatting(IInterpolatedStringExpression interpolatedStringExpression, IHighlightingConsumer consumer)
  {
    if (interpolatedStringExpression.InsertsEnumerable.IsEmpty())
      return; // compiled into ordinary string

    if (interpolatedStringExpression.IsConstantValue())
      return; // C# 10 constant interpolation expressions

    var hint = CanCompileToStringConcat(interpolatedStringExpression) ? " ('String.Concat' method call)" : null;
    consumer.AddHighlighting(
      new ObjectAllocationHighlighting(interpolatedStringExpression, $"new 'String' instance creation{hint}"),
      GetRangeToHighlight(interpolatedStringExpression));
  }

  private static void ReportFormattableStringAllocations(IInterpolatedStringExpression interpolatedStringExpression, IHighlightingConsumer consumer)
  {
    var resolveResult = interpolatedStringExpression.FormatReference.Resolve();
    if (resolveResult is not { DeclaredElement: IMethod method, ResolveErrorType.IsAcceptable: true }) return;

    var returnType = resolveResult.Substitution[method.ReturnType];
    var interpolationType = returnType.GetPresentableName(interpolatedStringExpression.Language, CommonUtils.DefaultTypePresentationStyle);

    var paramsArrayHint = (string?)null;

    if (interpolatedStringExpression.InsertsEnumerable.Any()
        && method.Parameters is { Count: > 1 } parameters
        && parameters[^1].IsParameterArray)
    {
      paramsArrayHint = ", allocates parameter array";
    }

    consumer.AddHighlighting(
      new ObjectAllocationHighlighting(interpolatedStringExpression, $"new '{interpolationType}' instance creation{paramsArrayHint}"),
      GetRangeToHighlight(interpolatedStringExpression));
  }

  private static void ReportInterpolatedStringHandlerAllocations(
    IInterpolatedStringExpression interpolatedStringExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var resolveResult = interpolatedStringExpression.HandlerConstructorReference.Resolve();
    if (resolveResult is not { DeclaredElement: IConstructor { ContainingType: { } interpolatedStringHandlerTypeElement }, ResolveErrorType.IsAcceptable: true }) return;

    var predefinedType = data.GetPredefinedType();
    if (interpolatedStringHandlerTypeElement.Equals(predefinedType.DefaultInterpolatedStringHandler.GetTypeElement()))
    {
      consumer.AddHighlighting(
        new ObjectAllocationHighlighting(interpolatedStringExpression, $"new 'String' instance creation"),
        GetRangeToHighlight(interpolatedStringExpression));
    }
    else if (interpolatedStringHandlerTypeElement is IClass classTypeElement)
    {
      var handlerTypeText = TypeFactory.CreateType(classTypeElement)
        .GetPresentableName(interpolatedStringExpression.Language, CommonUtils.DefaultTypePresentationStyle);

      consumer.AddHighlighting(
        new ObjectAllocationHighlighting(interpolatedStringExpression, $"new '{handlerTypeText}' interpolated string handler instance creation"),
        GetRangeToHighlight(interpolatedStringExpression));
    }
  }

  [Pure]
  private static bool CanCompileToStringConcat(IInterpolatedStringExpression interpolatedStringExpression)
  {
    foreach (var insert in interpolatedStringExpression.InsertsEnumerable)
    {
      if (insert.AlignmentExpression != null) return false;
      if (insert.FormatSpecifier != null) return false;

      var insertExpression = insert.Expression;
      if (insertExpression == null || !insertExpression.GetExpressionType().ToIType().IsString()) return false;
    }

    return true;
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