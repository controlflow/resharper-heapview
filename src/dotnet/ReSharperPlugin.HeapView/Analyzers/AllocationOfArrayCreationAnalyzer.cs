#nullable enable
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(IArrayCreationExpression) },
  HighlightingTypes = new[] { typeof(ObjectAllocationEvidentHighlighting) })]
public class AllocationOfArrayCreationAnalyzer : HeapAllocationAnalyzerBase<IArrayCreationExpression>
{
  protected override void Run(
    IArrayCreationExpression arrayCreationExpression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (arrayCreationExpression.IsInTheContextWhereAllocationsAreNotImportant()) return;

    var createdArrayType = arrayCreationExpression.Type() as IArrayType;
    if (createdArrayType == null) return;

    if (arrayCreationExpression.Initializer != null
        && IsOptimizedArrayOfBytesConvertedReadonlySpan(arrayCreationExpression, createdArrayType)) return;

    var typeName = createdArrayType.GetPresentableName(arrayCreationExpression.Language, CommonUtils.DefaultTypePresentationStyle);

    var newKeyword = arrayCreationExpression.NewKeyword.NotNull();
    consumer.AddHighlighting(
      new ObjectAllocationEvidentHighlighting(newKeyword, $"new '{typeName}' array instance creation"));
  }

  private static bool IsOptimizedArrayOfBytesConvertedReadonlySpan(
    IArrayCreationExpression arrayCreationExpression, IArrayType createdArrayType)
  {
    if (createdArrayType.Rank != 1) return false;

    var targetType = arrayCreationExpression.GetImplicitlyConvertedTo();
    if (!targetType.IsReadOnlySpan()) return false;

    _ = targetType.IsSpanOrReadOnlySpan(out var typeArgument);

    var elementType = createdArrayType.ElementType;
    if (!TypeEqualityComparer.Default.Equals(typeArgument, elementType)) return false;

    // only byte-sized types supported by now, because of the endiannes
    var underlyingType = elementType.GetEnumUnderlying() ?? elementType;
    if (underlyingType.IsByte() || underlyingType.IsSbyte() || underlyingType.IsBool())
    {
      var arrayInitializer = arrayCreationExpression.ArrayInitializer.NotNull();

      // whole array creation must be constant
      foreach (var variableInitializer in arrayInitializer.ElementInitializersEnumerable)
      {
        if (variableInitializer is not IExpressionInitializer { Value: { } itemValue }) return false;

        if (!itemValue.IsConstantValue()) return false;
      }

      return true;
    }

    return false;
  }
}