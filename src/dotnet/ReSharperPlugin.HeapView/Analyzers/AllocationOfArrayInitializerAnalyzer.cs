#nullable enable
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[] { typeof(IArrayInitializer) },
  HighlightingTypes = new[] { typeof(ObjectAllocationHighlighting) })]
public class AllocationOfArrayInitializerAnalyzer : HeapAllocationAnalyzerBase<IArrayInitializer>
{
  protected override void Run(
    IArrayInitializer arrayInitializer, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    var initializedElement = TryGetInitializedElement(arrayInitializer);
    if (initializedElement == null) return;

    if (arrayInitializer.IsInTheContextWhereAllocationsAreNotImportant()) return;

    var createdArrayType = initializedElement.Type as IArrayType;
    if (createdArrayType == null) return;

    ITreeNode startNode = arrayInitializer.LBrace, endNode = startNode;

    var previousToken = arrayInitializer.LBrace.GetPreviousMeaningfulToken();
    if (previousToken != null && previousToken.GetTokenType() == CSharpTokenType.EQ)
    {
      // larger range to highlight
      startNode = previousToken;
    }

    var typeName = createdArrayType.GetPresentableName(arrayInitializer.Language, CommonUtils.DefaultTypePresentationStyle);
    var highlighting = new ObjectAllocationHighlighting(arrayInitializer.LBrace, $"new '{typeName}' array instance creation");
    consumer.AddHighlighting(highlighting, startNode.GetDocumentRange().SetEndTo(endNode.GetDocumentEndOffset()));
  }

  [Pure]
  private static ITypeOwner? TryGetInitializedElement(IArrayInitializer arrayInitializer)
  {
    var fieldDeclaration = FieldDeclarationNavigator.GetByInitial(arrayInitializer);
    if (fieldDeclaration != null)
      return fieldDeclaration.DeclaredElement;

    var propertyDeclaration = PropertyDeclarationNavigator.GetByInitial(arrayInitializer);
    if (propertyDeclaration != null)
      return propertyDeclaration.DeclaredElement;

    var variableDeclaration = LocalVariableDeclarationNavigator.GetByInitial(arrayInitializer);
    if (variableDeclaration != null)
      return variableDeclaration.DeclaredElement;

    // int[,] matrix = { { 1, 2 }, { 3, 4 } };
    // - inner array initializers do not allocate separate objects
    return null;
  }
}