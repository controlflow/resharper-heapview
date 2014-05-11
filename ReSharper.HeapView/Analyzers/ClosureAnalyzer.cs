using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Tree.Query;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.HeapView.Analyzers
{
  [ElementProblemAnalyzer(
    elementTypes: new[] {
      typeof(ICSharpFunctionDeclaration),
      typeof(IFieldDeclaration)
    },
    HighlightingTypes = new[] {
      typeof(HeapAllocationHighlighting),
      typeof(SlowDelegateCreationHighlighting),
    })]
  public class ClosureAnalyzer : ElementProblemAnalyzer<ITreeNode>
  {
    protected override void Run(ITreeNode element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      IFunction thisElement = null;
      ILocalScope topScope = null;
      var function = element as ICSharpFunctionDeclaration;
      if (function != null)
      {
        thisElement = function.DeclaredElement;
        topScope = function.Body as ILocalScope;
      }

      var inspector = new ClosureInspector(element, thisElement);
      element.ProcessDescendants(inspector);

      // report closures allocations
      if (inspector.Closures.Count > 0)
      {
        ReportClosureAllocations(
          element, thisElement, topScope, inspector, consumer);
      }

      // report non-cached generic lambda expressions
      if (function != null && inspector.ClosurelessLambdas.Count > 0)
      {
        ReportClosurelessAllocations(function, inspector, consumer);
      }

      // report anonymous types in query expressions
      if (inspector.AnonymousTypes.Count > 0)
      {
        ReportAnonymousTypes(inspector, consumer);
      }
    }

    private static void ReportClosureAllocations(
      [NotNull] ITreeNode topDeclaration, [CanBeNull] IFunction thisElement,
      [CanBeNull] ILocalScope topScope, [NotNull] ClosureInspector inspector,
      [NotNull] IHighlightingConsumer consumer)
    {
      var scopesMap = new Dictionary<IDeclaredElement, ILocalScope>();
      var captureScopes = new Dictionary<ILocalScope, JetHashSet<IDeclaredElement>>();

      // group captures by their scope, report non-cached delegates
      foreach (var closure in inspector.Closures)
      {
        foreach (var capture in closure.Value)
        {
          ILocalScope scope = null;
          if (capture is IFunction) scope = topScope; // 'this' capture
          else
          {
            var declarations = capture.GetDeclarations();
            if (declarations.Count == 0) // accessors 'value' parameter
            {
              var accessor = thisElement as IAccessor;
              if (accessor != null && Equals(accessor.ValueVariable, capture))
                scope = topScope;
            }
            else
            {
              foreach (var declaration in declarations)
              {
                if (declaration is IRegularParameterDeclaration) scope = topScope;
                else scope = declaration.GetContainingNode<ILocalScope>();
                break;
              }
            }
          }

          if (scope == null) continue;

          JetHashSet<IDeclaredElement> captures;
          if (!captureScopes.TryGetValue(scope, out captures))
            captureScopes[scope] = captures = new JetHashSet<IDeclaredElement>();

          captures.Add(capture);
          scopesMap[capture] = scope;
        }


        {
          var highlightingRange = GetClosureRange(closure.Key);
          if (highlightingRange.IsValid())
          {
            if (IsExpressionLambda(closure.Key))
            {
              consumer.AddHighlighting(
                new HeapAllocationHighlighting(closure.Key, "expression tree construction"),
                highlightingRange);
            }
            else
            {
              consumer.AddHighlighting(
                new HeapAllocationHighlighting(closure.Key,
                  string.Format("delegate instantiation (capture of {0})",
                  FormatClosureDescription(closure.Value))),
                highlightingRange);
            }
          }
        }
      }

      // highlight first captured entity per every scope
      foreach (var scopeToCaptures in captureScopes)
      {
        var firstOffset = TreeOffset.MaxValue;
        IDeclaredElement firstCapture = null;

        foreach (var capture in scopeToCaptures.Value)
        {
          if (capture is IFunction) continue;

          var offset = GetCaptureStartOffset(capture);
          if (offset < firstOffset)
          {
            firstOffset = offset;
            firstCapture = capture;
          }
        }

        var scopeClosure = FormatClosureDescription(scopeToCaptures.Value);

        // collect outer captures
        JetHashSet<IDeclaredElement> outerCaptures = null;
        foreach (var closureToCaptures in inspector.Closures)
        {
          if (!scopeToCaptures.Key.Contains(closureToCaptures.Key)) continue;
          foreach (var capture in closureToCaptures.Value)
          {
            ILocalScope scope;
            if (!scopesMap.TryGetValue(capture, out scope)) continue;
            if (scopeToCaptures.Key.Contains(scope)) continue;

            outerCaptures = outerCaptures ?? new JetHashSet<IDeclaredElement>();
            outerCaptures.Add(capture);
          }
        }

        if (outerCaptures != null)
        {
          scopeClosure += string.Format(" + (outer closure of {0})",
            FormatClosureDescription(outerCaptures));
        }

        if (firstCapture != null)
        {
          DocumentRange highlightingRange;
          var anchor = GetCaptureHighlightingRange(
            topDeclaration, thisElement, firstCapture, out highlightingRange);
          if (anchor != null && highlightingRange.IsValid())
          {
            consumer.AddHighlighting(
              new HeapAllocationHighlighting(anchor,
                string.Format("closure instantiation ({0})", scopeClosure)),
              highlightingRange);
          }
        }
      }
    }

    [NotNull]
    private static string FormatClosureDescription(
      [NotNull] JetHashSet<IDeclaredElement> elements)
    {
      int parameters = 0, vars = 0;
      var hasThis = false;

      foreach (var element in elements)
      {
        if (element is IParameter ||
            element is IQueryAnonymousTypeProperty) parameters++;
        else if (element is ILocalVariable) vars++;
        else if (element is IFunction) hasThis = true;
      }

      var buf = new StringBuilder();
      if (parameters > 0)
      {
        buf.Append(FormatOrderedByStartOffset(elements.Where(element =>
             element is IParameter || element is IQueryAnonymousTypeProperty)))
           .Append(' ')
           .Append(NounUtil.ToPluralOrSingular("parameter", parameters));
      }

      if (vars > 0)
      {
        if (parameters > 0) buf.Append(hasThis ? ", " : " and ");

        buf.Append(FormatOrderedByStartOffset(
             elements.Where(element => element is ILocalVariable)))
           .Append(' ')
           .Append(NounUtil.ToPluralOrSingular("variable", vars));
      }

      if (hasThis)
      {
        if (parameters > 0 || vars > 0) buf.Append(" and ");
        buf.Append("'this' reference");
      }

      return buf.ToString();
    }

    [NotNull]
    private static string FormatOrderedByStartOffset([NotNull] IEnumerable<IDeclaredElement> elements)
    {
      return elements
        // ReSharper disable once ConvertClosureToMethodGroup
        .OrderBy(element => GetCaptureStartOffset(element))
        .AggregateString(", ", (builder, element) =>
          builder.Append("'").Append(element.ShortName).Append("'"));
    }

    [CanBeNull]
    private static ITreeNode GetCaptureHighlightingRange(
      [NotNull] ITreeNode topDeclaration, [CanBeNull] IFunction thisElement,
      [NotNull] IDeclaredElement capture, out DocumentRange range)
    {
      var declarations = capture.GetDeclarations();
      if (declarations.Count == 0) // accessors 'value' parameter
      {
        var accessor = thisElement as IAccessor;
        if (accessor != null && Equals(accessor.ValueVariable, capture))
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
      var nameEndOffset = range.EndOffsetRange().TextRange.EndOffset;

      var variable = declaration as ILocalVariableDeclaration;
      if (variable != null)
      {
        var multiple = MultipleLocalVariableDeclarationNavigator.GetByDeclarator(variable);
        if (multiple != null && multiple.Declarators[0] == variable)
        {
          var documentRange = multiple.GetTypeRange();
          range = documentRange.SetEndTo(nameEndOffset);
          return variable;
        }

        return null;
      }

      var parameter = declaration as IRegularParameterDeclaration;
      if (parameter != null)
      {
        range = parameter.TypeUsage.GetDocumentRange().SetEndTo(nameEndOffset);
        return parameter;
      }

      var anonymousParameter = declaration as IAnonymousMethodParameterDeclaration;
      if (anonymousParameter != null)
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
      [NotNull] ICSharpFunctionDeclaration declaration, [NotNull] ClosureInspector inspector,
      [NotNull] IHighlightingConsumer consumer)
    {
      var parametersOwner = declaration.DeclaredElement as ITypeParametersOwner;
      if (parametersOwner != null && parametersOwner.TypeParameters.Count != 0)
      {
        foreach (var lambda in inspector.ClosurelessLambdas)
        {
          if (IsExpressionLambda(lambda)) continue;

          var highlightingRange = GetClosureRange(lambda);
          if (highlightingRange.IsValid())
          {
            consumer.AddHighlighting(
              new HeapAllocationHighlighting(lambda,
                "delegate instantiation from generic " +
                "anonymous function (always non cached)"),
              highlightingRange);

            consumer.AddHighlighting(
              new SlowDelegateCreationHighlighting(lambda,
                "anonymous function in generic method is generic itself"),
              highlightingRange);
          }
        }
      }

      foreach (var lambda in inspector.ClosurelessLambdas)
      {
        if (!IsExpressionLambda(lambda)) continue;

        var highlightingRange = GetClosureRange(lambda);
        if (highlightingRange.IsValid())
        {
          consumer.AddHighlighting(
            new HeapAllocationHighlighting(lambda, "expression tree construction"),
            highlightingRange);
        }
      }
    }

    private static DocumentRange GetClosureRange([CanBeNull] ITreeNode function)
    {
      var lambda = function as ILambdaExpression;
      if (lambda != null)
        return lambda.LambdaArrow.GetDocumentRange();

      var anonymousMethod = function as IAnonymousMethodExpression;
      if (anonymousMethod != null)
        return anonymousMethod.DelegateKeyword.GetDocumentRange();

      var platform = function as IQueryParameterPlatform;
      if (platform != null)
      {
        var token = platform.GetPreviousMeaningfulToken();
        if (token != null && token.GetTokenType().IsKeyword)
          return token.GetDocumentRange();

        var clause = platform.GetContainingNode<IQueryClause>();
        if (clause != null)
          return clause.FirstKeyword.GetDocumentRange();
      }

      return DocumentRange.InvalidRange;
    }

    private static void ReportAnonymousTypes(
      [NotNull] ClosureInspector inspector, [NotNull] IHighlightingConsumer consumer)
    {
      foreach (var queryClause in inspector.AnonymousTypes)
      {
        consumer.AddHighlighting(
          new HeapAllocationHighlighting(queryClause,
            "transparent identifier anonymous type instantiation"),
          queryClause.GetDocumentRange());
      }
    }

    private static bool IsExpressionLambda([NotNull] ITreeNode function)
    {
      var lambdaExpression = function as ILambdaExpression;
      if (lambdaExpression != null)
      {
        var type = lambdaExpression.GetImplicitlyConvertedTo() as IDeclaredType;
        if (type != null && !type.IsUnknown) return type.IsExpression();
      }

      var parameterPlatform = function as IQueryParameterPlatform;
      if (parameterPlatform != null)
      {
        var matchingParameter = parameterPlatform.MatchingParameter;
        if (matchingParameter != null)
        {
          var type = matchingParameter.Substitution[matchingParameter.Element.Type];
          if (!type.IsUnknown) return type.IsExpression();
        }
      }

      return false;
    }

    private sealed class ClosureInspector : IRecursiveElementProcessor
    {
      [NotNull] private readonly Stack<ITreeNode> myClosures;
      [CanBeNull] private readonly IFunction myFunction;

      public ClosureInspector([NotNull] ITreeNode topLevelNode, [CanBeNull] IFunction thisElement)
      {
        myFunction = thisElement;
        myClosures = new Stack<ITreeNode>();
        myClosures.Push(topLevelNode);
        Closures = new Dictionary<ITreeNode, JetHashSet<IDeclaredElement>>();
        AnonymousTypes = new JetHashSet<IQueryRangeVariableDeclaration>();
        ClosurelessLambdas = new List<ITreeNode>();
      }

      [NotNull] public Dictionary<ITreeNode, JetHashSet<IDeclaredElement>> Closures { get; private set; }
      [NotNull] public List<ITreeNode> ClosurelessLambdas { get; private set; }
      [NotNull] public JetHashSet<IQueryRangeVariableDeclaration> AnonymousTypes { get; private set; }

      public bool InteriorShouldBeProcessed(ITreeNode element) { return true; }
      public bool ProcessingIsFinished { get { return false; } }

      public void ProcessAfterInterior(ITreeNode element)
      {
        if (element is IAnonymousFunctionExpression || element is IQueryParameterPlatform)
        {
          var lambda = myClosures.Pop();
          Assertion.Assert(lambda == element, "lambda == element");

          if (!Closures.ContainsKey(lambda))
            ClosurelessLambdas.Add(lambda);
        }
      }

      public void ProcessBeforeInterior(ITreeNode element)
      {
        var expression = element as ICSharpExpression;
        if (expression != null)
        {
          ProcessExpression(expression);
          return;
        }

        var platform = element as IQueryParameterPlatform;
        if (platform != null)
        {
          myClosures.Push(platform);
        }
      }

      private void AddThisCapture()
      {
        if (myFunction == null) return;
        foreach (var closureKey in myClosures)
        {
          AddCapture(closureKey, myFunction);
        }
      }

      private void AddCapture(
        [NotNull] ITreeNode closure, [NotNull] IDeclaredElement element)
      {
        JetHashSet<IDeclaredElement> captures;
        if (!Closures.TryGetValue(closure, out captures))
          Closures.Add(closure, captures = new JetHashSet<IDeclaredElement>());

        captures.Add(element);
      }

      private void ProcessExpression([NotNull] ICSharpExpression element)
      {
        var thisExpression = element as IThisExpression;
        if (thisExpression != null)
        {
          AddThisCapture();
          return;
        }

        var baseExpression = element as IBaseExpression;
        if (baseExpression != null)
        {
          AddThisCapture();
          return;
        }

        var reference = element as IReferenceExpression;
        if (reference != null && reference.QualifierExpression == null)
        {
          ProcessReferenceExpression(reference);
          return;
        }

        var anonymousFunction = element as IAnonymousFunctionExpression;
        if (anonymousFunction != null)
        {
          myClosures.Push(anonymousFunction);
        }
      }

      private void ProcessReferenceExpression(IReferenceExpression reference)
      {
        var declaredElement = reference.Reference.Resolve().DeclaredElement;

        var parameter = declaredElement as IParameter;
        if (parameter != null)
        {
          var parametersOwner = parameter.ContainingParametersOwner;
          if (parametersOwner == null) return;

          foreach (var closure in myClosures)
          {
            var queryPlatform = closure as IQueryParameterPlatform;
            if (queryPlatform != null)
            {
              var platform = parametersOwner as IQueryParameterPlatform;
              if (platform != null && queryPlatform == platform) return;

              // outer query parameter
              AddCapture(closure, parameter);
            }
            else
            {
              // anonymous parameter access
              if (ReferenceEquals(parametersOwner, closure)) return;

              if (myClosures.Count == 1)
              {
                if (parametersOwner.Equals(myFunction)) return; // simple parameter access

                var accessor = myFunction as IAccessor;
                if (accessor != null && accessor.OwnerMember.Equals(parametersOwner))
                  return; // indexer parameter access
              }

              AddCapture(closure, parameter);
            }
          }

          return;
        }

        var variable = declaredElement as ILocalVariable;
        if (variable != null)
        {
          if (variable.IsStatic) return;
          if (variable.IsConstant) return;

          var declarations = variable.GetDeclarations();
          if (declarations.Count == 1)
          {
            var declaration = declarations[0];
            foreach (var closure in myClosures)
            {
              if (!(closure is IQueryParameterPlatform)
                 && closure.Contains(declaration)) return;

              AddCapture(closure, variable);
            }
          }

          return;
        }

        var typeMember = declaredElement as ITypeMember;
        if (typeMember != null) // .this closure
        {
          if (typeMember is ITypeElement) return;
          if (typeMember.IsStatic) return;

          var field = typeMember as IField;
          if (field != null && field.IsConstant) return;

          AddThisCapture();
          return;
        }

        var anonymousProperty = declaredElement as IQueryAnonymousTypeProperty;
        if (anonymousProperty != null)
        {
          foreach (var anonymousTypeProperty in anonymousProperty.ContainingType.Properties)
          {
            var property = (IQueryAnonymousTypeProperty) anonymousTypeProperty;
            var declaration = property.Declaration;

            if (QueryFirstFromNavigator.GetByDeclaration(declaration) == null &&
                QueryContinuationNavigator.GetByDeclaration(declaration) == null)
            {
              AnonymousTypes.Add(declaration);
            }
          }
        }
      }
    }
  }
}
