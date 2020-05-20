using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Collections;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Tree.Query;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using ReSharperPlugin.HeapView.Highlightings;

// ReSharper disable ConvertClosureToMethodGroup
// ReSharper disable RedundantExplicitParamsArrayCreation

namespace ReSharperPlugin.HeapView.Analyzers
{
  // todo: generics can be introduces by local functions - not a problem in Roslyn
  // todo: report delegate allocations from method group
  // todo: implement "Implicitly captured closure" warning

  [ElementProblemAnalyzer(
    ElementTypes: new[] {
      // constructors, methods, operators, accessors
      typeof(ICSharpFunctionDeclaration),
      // expression-bodied properties/indexers
      typeof(IExpressionBodyOwnerDeclaration),
      // field/event/auto-property initializers
      typeof(IFieldDeclaration),
      typeof(IEventDeclaration),
      typeof(IPropertyDeclaration)
    },
    HighlightingTypes = new[] {
      typeof(ObjectAllocationHighlighting),
      typeof(ObjectAllocationEvidentHighlighting),
      typeof(ObjectAllocationPossibleHighlighting),
      typeof(ClosureAllocationHighlighting),
      typeof(DelegateAllocationHighlighting),
      typeof(CanEliminateClosureCreationHighlighting)
    })]
  public class ClosureAnalyzer : ElementProblemAnalyzer<ICSharpDeclaration>
  {
    protected override void Run(ICSharpDeclaration declaration, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      var inspector = ClosuresInspector.TryBuild(declaration);
      if (inspector == null) return;

      inspector.Run();

      // report closures allocations
      if (inspector.Captures.Count > 0)
      {


        ReportClosureAllocations(declaration, inspector.TopLevelParametersOwner, inspector, consumer);
      }

      // todo: not the case in CS 6+?
      // report non-cached generic lambda expressions

      ReportClosurelessAllocations(declaration, inspector, consumer);

      // report anonymous types in query expressions
      if (inspector.AnonymousTypes.Count > 0)
      {
        ReportAnonymousTypes(inspector, consumer);
      }
    }

    private static void ReportClosureAllocations(
      [NotNull] ITreeNode topDeclaration, [CanBeNull] IParametersOwner thisElement,
      [NotNull] ClosuresInspector inspector, [NotNull] IHighlightingConsumer consumer)
    {
      // report allocations of delegate instances and expression trees
      foreach (var (closure, captures) in inspector.Captures)
      {
        // local function is the only closure construct that do not allocates itself, it's usages do
        if (closure is ILocalFunctionDeclaration) continue;

        var highlightingRange = GetClosureRange(closure);
        if (!highlightingRange.IsValid()) continue;

        if (IsExpressionTreeClosure(closure))
        {
          var highlighting = new ObjectAllocationHighlighting(closure, "expression tree construction");
          consumer.AddHighlighting(highlighting, highlightingRange);
        }
        else
        {
          var description = FormatClosureDescription(captures);
          var highlighting = new DelegateAllocationHighlighting(closure, "capture of " + description);
          consumer.AddHighlighting(highlighting, highlightingRange);

          TryReportClosurelessOverloads(closure, consumer);
        }
      }

      // todo: document how this-only captures do not allocate display class

      // compute the backwards map, IDeclaredElement -> it's display class scope
      var scopesMap = new Dictionary<IDeclaredElement, ILocalScope>();

      foreach (var (scope, captures) in inspector.CapturesOfScope)
      foreach (var capture in captures)
      {
        scopesMap[capture] = scope;
      }

      // highlight first captured entity per every scope
      foreach (var (localScope, caps) in inspector.CapturesOfScope)
      {
        if (inspector.IsDisplayClassForScopeCanBeLoweredToStruct(localScope))
        {
          continue; // do not report
        }

        // compute display class creation point
        var firstOffset = TreeOffset.MaxValue;
        IDeclaredElement firstCapture = null;

        foreach (var capture in caps)
        {
          // todo: where this-only closure is created?
          if (capture is IFunction) continue;

          var offset = GetCaptureStartOffset(capture);
          if (offset < firstOffset)
          {
            firstOffset = offset;
            firstCapture = capture;
          }
        }

        var scopeClosure = FormatClosureDescription(caps);

        // collect outer captures
        JetHashSet<IDeclaredElement> outerCaptures = null;
        foreach (var (closure, captures) in inspector.Captures)
        {
          if (!localScope.Contains(closure)) continue;

          // for every closure that is located inside current local scope


          foreach (var capture in captures)
          {
            if (!scopesMap.TryGetValue(capture, out var scope)) continue;

            if (localScope.Contains(scope)) continue;

            outerCaptures ??= new JetHashSet<IDeclaredElement>();
            outerCaptures.Add(capture);
          }
        }

        if (outerCaptures != null)
        {
          var description = FormatClosureDescription(outerCaptures);
          scopeClosure += $" + (outer closure of {description})";
        }

        if (firstCapture != null)
        {
          var anchor = GetCaptureHighlightingRange(topDeclaration, thisElement, firstCapture, out var highlightingRange);
          if (anchor != null && highlightingRange.IsValid())
          {
            consumer.AddHighlighting(new ClosureAllocationHighlighting(anchor, scopeClosure), highlightingRange);
          }
        }
      }
    }

    private static void TryReportClosurelessOverloads([NotNull] ICSharpClosure closure, [NotNull] IHighlightingConsumer consumer)
    {
      var closureExpression = closure as ICSharpExpression;
      if (closureExpression == null) return;

      var invocationReference = ClosurelessOverloadSearcher.FindMethodInvocationByArgument(closureExpression);
      if (invocationReference == null) return;

      var parameterInstance = ClosurelessOverloadSearcher.FindClosureParameter(closureExpression);
      if (parameterInstance == null) return;

      var overloadWithStateParameter = ClosurelessOverloadSearcher.FindOverloadByParameter(parameterInstance);
      if (overloadWithStateParameter != null)
      {
        var highlighting = new CanEliminateClosureCreationHighlighting(closureExpression);
        consumer.AddHighlighting(highlighting, invocationReference.GetDocumentRange());
      }
    }

    [NotNull]
    private static string FormatClosureDescription([NotNull] ISet<IDeclaredElement> declaredElements)
    {
      int parameters = 0, vars = 0;
      var hasThis = false;

      foreach (var element in declaredElements)
      {
        if (IsParameter(element)) parameters++;
        else if (element is ILocalVariable) vars++;
        else if (element is IParametersOwner) hasThis = true;

        // todo: local functions
      }

      var builder = new StringBuilder();

      if (parameters > 0)
      {
        var parameterElements = declaredElements.Where(element => IsParameter(element));
        builder
          .Append(FormatOrderedByStartOffset(parameterElements)).Append(' ')
          .Append(NounUtil.ToPluralOrSingular("parameter", parameters));
      }

      if (vars > 0)
      {
        if (parameters > 0) builder.Append(hasThis ? ", " : " and ");

        var localElements = declaredElements.Where(element => element is ILocalVariable);
        builder
          .Append(FormatOrderedByStartOffset(localElements)).Append(' ')
          .Append(NounUtil.ToPluralOrSingular("variable", vars));
      }

      if (hasThis)
      {
        if (parameters > 0 || vars > 0) builder.Append(" and ");

        builder.Append("'this' reference");
      }

      return builder.ToString();

      static bool IsParameter(IDeclaredElement declaredElement)
      {
        return declaredElement is IParameter
               || declaredElement is IQueryAnonymousTypeProperty;
      }
    }

    [NotNull]
    private static string FormatOrderedByStartOffset([NotNull] IEnumerable<IDeclaredElement> elements)
    {
      return elements
        .OrderBy(element => GetCaptureStartOffset(element))
        .AggregateString(", ", (sb, e) => sb.Append("'").Append(e.ShortName).Append("'"));
    }

    [CanBeNull]
    private static ITreeNode GetCaptureHighlightingRange(
      [NotNull] ITreeNode topDeclaration, [CanBeNull] IParametersOwner thisElement, [NotNull] IDeclaredElement capture, out DocumentRange range)
    {
      var declarations = capture.GetDeclarations();
      if (declarations.Count == 0) // accessors 'value' parameter
      {
        if (thisElement is IAccessor accessor && Equals(accessor.ValueVariable, capture))
        {
          var identifier = ((IAccessorDeclaration)topDeclaration).NameIdentifier;
          range = identifier.GetDocumentRange();
          return identifier;
        }

        range = DocumentRange.InvalidRange;
        return null;
      }

      var declaration = declarations[0];
      range = declaration.GetNameDocumentRange();
      var nameEndOffset = range.EndOffset;

      if (declaration is ILocalVariableDeclaration variableDeclaration)
      {
        var multiple = MultipleLocalVariableDeclarationNavigator.GetByDeclarator(variableDeclaration);
        if (multiple != null && multiple.Declarators[0] == variableDeclaration)
        {
          var documentRange = multiple.GetTypeRange();
          range = documentRange.SetEndTo(nameEndOffset);
          return variableDeclaration;
        }

        return null;
      }

      if (declaration is IRegularParameterDeclaration parameterDeclaration)
      {
        if (range.TextRange.Length < 3)
          range = parameterDeclaration.TypeUsage.GetDocumentRange().SetEndTo(nameEndOffset);

        return parameterDeclaration;
      }

      if (declaration is IAnonymousMethodParameterDeclaration anonymousParameter)
      {
        range = anonymousParameter.TypeUsage.GetDocumentRange().SetEndTo(nameEndOffset);
        return anonymousParameter;
      }

      return declaration;
    }

    private static TreeOffset GetCaptureStartOffset([NotNull] IDeclaredElement capture)
    {
      var declarations = capture.GetDeclarations();
      if (declarations.Count == 0) return TreeOffset.Zero;

      return declarations[0].GetTreeStartOffset();
    }

    private static void ReportClosurelessAllocations(
      [NotNull] ITreeNode element, [NotNull] ClosuresInspector inspector, [NotNull] IHighlightingConsumer consumer)
    {
      // note: Roslyn compiler implements caching of such closures
      if (!element.IsCSharp6Supported()
          && inspector.TopLevelParametersOwner is ITypeParametersOwner typeParametersOwner
          && typeParametersOwner.TypeParameters.Count > 0)
      {
        foreach (var closure in (IEnumerable<ICSharpClosure>) inspector.CapturelessClosures)
        {
          if (IsExpressionTreeClosure(closure)) continue;

          var closureRange = GetClosureRange(closure);
          if (!closureRange.IsValid()) continue;

          consumer.AddHighlighting(
            new DelegateAllocationHighlighting(closure, "from generic anonymous function (always non cached)"), closureRange);
        }
      }

      foreach (var closure in (IEnumerable<ICSharpClosure>) inspector.CapturelessClosures)
      {
        if (!IsExpressionTreeClosure(closure)) continue;

        var closureRange = GetClosureRange(closure);
        if (closureRange.IsValid())
        {
          consumer.AddHighlighting(
            new ObjectAllocationHighlighting(closure, "expression tree construction"), closureRange);
        }
      }
    }

    [Pure]
    private static DocumentRange GetClosureRange([CanBeNull] ICSharpClosure closure)
    {
      switch (closure)
      {
        case ILambdaExpression lambdaExpression:
          return lambdaExpression.LambdaArrow.GetDocumentRange();

        case IAnonymousMethodExpression anonymousMethodExpression:
          return anonymousMethodExpression.DelegateKeyword.GetDocumentRange();

        case ILocalFunctionDeclaration localFunctionDeclaration:
          return localFunctionDeclaration.GetNameDocumentRange();

        case IQueryParameterPlatform queryParameterPlatform:
        {
          var previousToken = queryParameterPlatform.GetPreviousMeaningfulToken();
          if (previousToken != null && previousToken.GetTokenType().IsKeyword)
            return previousToken.GetDocumentRange();

          var queryClause = queryParameterPlatform.GetContainingNode<IQueryClause>();
          if (queryClause != null)
            return queryClause.FirstKeyword.GetDocumentRange();

          goto default;
        }

        default:
          return DocumentRange.InvalidRange;
      }
    }

    private static void ReportAnonymousTypes([NotNull] ClosuresInspector inspector, [NotNull] IHighlightingConsumer consumer)
    {
      foreach (var queryClause in inspector.AnonymousTypes)
      {
        var highlighting = new ObjectAllocationHighlighting(queryClause, "transparent identifier anonymous type instantiation");
        consumer.AddHighlighting(highlighting, queryClause.GetDocumentRange());
      }
    }

    [Pure]
    private static bool IsExpressionTreeClosure([NotNull] ICSharpClosure closure)
    {
      switch (closure)
      {
        case ILambdaExpression lambdaExpression:
          return lambdaExpression.IsLinqExpressionTreeLambda();
        case IQueryParameterPlatform parameterPlatform:
          return parameterPlatform.IsLinqExpressionTreeQuery();
        default:
          return false;
      }
    }
  }
}
