using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeAnnotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.HeapView.Highlightings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[] {
    typeof(IReferenceExpression),
    typeof(IObjectCreationExpression),
    typeof(IArrayCreationExpression),
    typeof(IInvocationExpression),
    typeof(IForeachStatement),
    typeof(IAdditiveExpression),
    typeof(IAssignmentExpression),
    typeof(IElementAccessExpression),
    typeof(IConstructorInitializer),
    typeof(ICollectionElementInitializer)
  },
  HighlightingTypes = new[] {
    typeof(ObjectAllocationHighlighting),
    typeof(ObjectAllocationEvidentHighlighting),
    typeof(ObjectAllocationPossibleHighlighting),
    typeof(DelegateAllocationHighlighting)
  })]
public sealed class HeapAllocationAnalyzer : HeapAllocationAnalyzerBase<ITreeNode>
{
  protected override void Run(ITreeNode element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    switch (element)
    {
      // F(); when F is iterator
      case IInvocationExpression invocationExpression:
        //CheckInvocationExpression(invocationExpression, consumer);
        return;

      // var xs = Iterator;
      case IReferenceExpression referenceExpression:
        //CheckReferenceExpression(referenceExpression, consumer);
        return;

      // foreach (var x in xs); when xs.GetEnumerator() is ref-type
      // note: produces false-positive for LocalList<T>-produced IList<T>
      case IForeachStatement foreachStatement:
        CheckForeachDeclaration(foreachStatement, consumer);
        return;
    }
  }

  private static void CheckInvocationExpression([NotNull] IInvocationExpression invocationExpression, [NotNull] IHighlightingConsumer consumer)
  {
    var invokedExpression = invocationExpression.InvokedExpression;
    if (invokedExpression == null) return;

    var invocationReference = invocationExpression.InvocationExpressionReference.NotNull();

    var (declaredElement, _, resolveErrorType) = invocationReference.Resolve();
    if (resolveErrorType != ResolveErrorType.OK) return;

    var method = declaredElement as IMethod;
    if (method == null) return;

    if (method.IsIterator)
    {
      consumer.AddHighlighting(
        new ObjectAllocationHighlighting(invocationExpression, "iterator method call"),
        invokedExpression.GetExpressionRange());
    }
    else if (method.ReturnType.Classify == TypeClassification.REFERENCE_TYPE)
    {
      var annotationsCache = invocationExpression.GetPsiServices().GetCodeAnnotationsCache();
      var linqTunnelAnnotationProvider = annotationsCache.GetProvider<LinqTunnelAnnotationProvider>();
      var pureAnnotationProvider = annotationsCache.GetProvider<PureAnnotationProvider>();

      if (pureAnnotationProvider.GetInfo(method) && linqTunnelAnnotationProvider.GetInfo(method))
      {
        consumer.AddHighlighting(
          new ObjectAllocationHighlighting(invocationExpression, "LINQ method call"),
          invokedExpression.GetExpressionRange());
      }
    }
  }

  private static void CheckReferenceExpression([NotNull] IReferenceExpression referenceExpression, [NotNull] IHighlightingConsumer consumer)
  {
    var (declaredElement, _) = referenceExpression.Reference.Resolve();

    switch (declaredElement)
    {
      case IProperty { Getter: { } getter }:
      {
        var languageService = referenceExpression.Language.LanguageServiceNotNull();

        var accessType = languageService.GetReferenceAccessType(referenceExpression.Reference);
        if (accessType == ReferenceAccessType.READ && getter.IsIterator)
        {
          consumer.AddHighlighting(
            new ObjectAllocationHighlighting(referenceExpression, "iterator property access"),
            referenceExpression.NameIdentifier.GetDocumentRange());
        }

        break;
      }
    }
  }

  private static void CheckForeachDeclaration([NotNull] IForeachStatement foreachStatement, [NotNull] IHighlightingConsumer consumer)
  {
    var collection = foreachStatement.Collection;

    var collectionType = collection?.Type() as IDeclaredType;
    if (collectionType == null || collectionType.IsUnknown) return;

    // no allocations because of compiler support (just like arrays)
    if (collectionType.IsString()) return;

    var typeElement = collectionType.GetTypeElement();
    if (typeElement == null) return;

    // search for GetEnumerator() method
    var symbolTable = ResolveUtil.GetSymbolTableByTypeElement(typeElement, SymbolTableMode.FULL, typeElement.Module);

    foreach (var symbolInfo in symbolTable.GetSymbolInfos("GetEnumerator"))
    {
      var method = symbolInfo.GetDeclaredElement() as IMethod;
      if (method == null) continue;

      if (!CSharpDeclaredElementUtil.IsForeachEnumeratorPatternMember(method)) continue;

      // with ref-return
      if (method.ReturnType.Classify == TypeClassification.REFERENCE_TYPE)
      {
        DocumentRange range;
        var inToken = collection.GetPreviousMeaningfulToken();
        if (inToken != null && inToken.GetTokenType().IsKeyword)
        {
          range = inToken.GetDocumentRange();
        }
        else
        {
          range = collection.GetExpressionRange();
        }

        var highlighting = new ObjectAllocationPossibleHighlighting(
          foreachStatement, "enumerator allocation (except iterators or collection with cached enumerator)");
        consumer.AddHighlighting(highlighting, range);
      }

      break;
    }
  }
}