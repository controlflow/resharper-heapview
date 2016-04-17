using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.HeapView.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Tree.Query;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resolve;
// ReSharper disable ConvertClosureToMethodGroup
// ReSharper disable RedundantExplicitParamsArrayCreation

namespace JetBrains.ReSharper.HeapView.Analyzers
{
  [ElementProblemAnalyzer(
    new[] {
      typeof(ICSharpFunctionDeclaration), // constructors, methods, operators, accessors
      typeof(IFieldDeclaration), typeof(IEventDeclaration), // field/event initializers
      typeof(IExpressionBodyOwnerDeclaration), // C# 6.0 expression-bodied members
      typeof(IPropertyDeclaration), // C# 6.0 property initializers
    },
    HighlightingTypes = new[] {
      typeof(ObjectAllocationHighlighting),
      typeof(ObjectAllocationEvidentHighlighting),
      typeof(ObjectAllocationPossibleHighlighting),
      typeof(ClosureAllocationHighlighting),
      typeof(DelegateAllocationHighlighting)
    })]
  public class ClosureAnalyzer : ElementProblemAnalyzer<ITreeNode>
  {
    protected override void Run(ITreeNode element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      IFunction function = null;
      ILocalScope topScope = null;

      var functionDeclaration = element as ICSharpFunctionDeclaration;
      if (functionDeclaration != null)
      {
        function = functionDeclaration.DeclaredElement;
        topScope = functionDeclaration.Body as ILocalScope;
      }

      var expressionBodyOwner = element as IExpressionBodyOwnerDeclaration;
      if (expressionBodyOwner != null)
      {
        var arrowExpression = expressionBodyOwner.ArrowExpression;
        if (arrowExpression != null)
        {
          function = expressionBodyOwner.GetFunction();
          topScope = arrowExpression;
        }
        else
        {
          if (element is IAccessorOwnerDeclaration) return;
        }
      }

      var inspector = new ClosureInspector(element, function);
      element.ProcessDescendants(inspector);

      // report closures allocations
      if (inspector.Closures.Count > 0)
      {
        ReportClosureAllocations(element, function, topScope, inspector, consumer);
      }

      // report non-cached generic lambda expressions
      if (function != null && inspector.ClosurelessLambdas.Count > 0)
      {
        ReportClosurelessAllocations(element, function, inspector, consumer);
      }

      // report anonymous types in query expressions
      if (inspector.AnonymousTypes.Count > 0)
      {
        ReportAnonymousTypes(inspector, consumer);
      }
    }

    private static void ReportClosureAllocations(
      [NotNull] ITreeNode topDeclaration, [CanBeNull] IFunction thisElement, [CanBeNull] ILocalScope topScope,
      [NotNull] ClosureInspector inspector, [NotNull] IHighlightingConsumer consumer)
    {
      var scopesMap = new Dictionary<IDeclaredElement, ILocalScope>();
      var captureScopes = new Dictionary<ILocalScope, JetHashSet<IDeclaredElement>>();

      // group captures by their scope, report non-cached delegates
      foreach (var closure in inspector.Closures)
      {
        foreach (var capture in closure.Value)
        {
          ILocalScope scope = null;

          if (capture is IFunction)
          {
            scope = topScope; // 'this' capture
          }
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
                if (declaration is IRegularParameterDeclaration)
                {
                  scope = topScope;
                }
                else
                {
                  scope = declaration.GetContainingNode<ILocalScope>();
                }

                break;
              }
            }
          }

          if (scope == null) continue;

          JetHashSet<IDeclaredElement> captures;
          if (!captureScopes.TryGetValue(scope, out captures))
          {
            captureScopes[scope] = captures = new JetHashSet<IDeclaredElement>();
          }

          captures.Add(capture);
          scopesMap[capture] = scope;
        }

        {
          var highlightingRange = GetClosureRange(closure.Key);
          if (highlightingRange.IsValid())
          {
            if (IsExpressionLambda(closure.Key))
            {
              var highlighting = new ObjectAllocationHighlighting(closure.Key, "expression tree construction");
              consumer.AddHighlighting(highlighting, highlightingRange);
            }
            else
            {
              var description = FormatClosureDescription(closure.Value);
              var highlighting = new DelegateAllocationHighlighting(closure.Key, "capture of " + description);
              consumer.AddHighlighting(highlighting, highlightingRange);

              ReportClosurelessOverloads(closure.Key, consumer);
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
          var description = FormatClosureDescription(outerCaptures);
          scopeClosure += string.Format(" + (outer closure of {0})", description);
        }

        if (firstCapture != null)
        {
          DocumentRange highlightingRange;
          var anchor = GetCaptureHighlightingRange(topDeclaration, thisElement, firstCapture, out highlightingRange);
          if (anchor != null && highlightingRange.IsValid())
          {
            consumer.AddHighlighting(new ClosureAllocationHighlighting(anchor, scopeClosure), highlightingRange);
          }
        }
      }
    }

    private static void ReportClosurelessOverloads([NotNull] ITreeNode closureNode, [NotNull] IHighlightingConsumer consumer)
    {
      var closureExpression = closureNode as ICSharpExpression;
      if (closureExpression == null) return;

      var invocationReference = ClosurelessOverloadSearcher.FindMethodInvocation(closureExpression);
      if (invocationReference == null) return;

      var parameterInstance = ClosurelessOverloadSearcher.FindClosureParameter(closureExpression);
      if (parameterInstance == null) return;

      var overload = ClosurelessOverloadSearcher.FindOverloadByParameter(parameterInstance);
      if (overload != null)
      {
        var highlighting = new CanEliminateClosureCreationHighlighting(closureExpression);
        consumer.AddHighlighting(highlighting, invocationReference.GetDocumentRange());
      }
    }

    [NotNull]
    private static string FormatClosureDescription([NotNull] JetHashSet<IDeclaredElement> declaredElements)
    {
      int parameters = 0, vars = 0;
      var hasThis = false;

      foreach (var element in declaredElements)
      {
        if (IsParameter(element)) parameters++;
        else if (element is ILocalVariable) vars++;
        else if (element is IFunction) hasThis = true;
      }

      var buf = new StringBuilder();
      if (parameters > 0)
      {
        var parameterElements = declaredElements.Where(element => IsParameter(element));
        buf.Append(FormatOrderedByStartOffset(parameterElements)).Append(' ')
           .Append(NounUtil.ToPluralOrSingular("parameter", parameters));
      }

      if (vars > 0)
      {
        if (parameters > 0) buf.Append(hasThis ? ", " : " and ");

        var localElements = declaredElements.Where(element => element is ILocalVariable);
        buf.Append(FormatOrderedByStartOffset(localElements)).Append(' ')
           .Append(NounUtil.ToPluralOrSingular("variable", vars));
      }

      if (hasThis)
      {
        if (parameters > 0 || vars > 0) buf.Append(" and ");
        buf.Append("'this' reference");
      }

      return buf.ToString();
    }

    private static bool IsParameter([NotNull] IDeclaredElement declaredElement)
    {
      return declaredElement is IParameter
          || declaredElement is IQueryAnonymousTypeProperty;
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
      [NotNull] ITreeNode topDeclaration, [CanBeNull] IFunction thisElement, [NotNull] IDeclaredElement capture, out DocumentRange range)
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
        if (range.TextRange.Length < 3)
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
      [NotNull] ITreeNode element, [NotNull] IFunction function, [NotNull] ClosureInspector inspector, [NotNull] IHighlightingConsumer consumer)
    {
      var typeParametersOwner = function as ITypeParametersOwner;
      if (typeParametersOwner != null && typeParametersOwner.TypeParameters.Count != 0)
      {
        foreach (var lambda in inspector.ClosurelessLambdas)
        {
          if (IsExpressionLambda(lambda)) continue;

          var closureRange = GetClosureRange(lambda);
          if (!closureRange.IsValid()) continue;

          // note: Roslyn compiler implements caching of such closures
          if (!element.IsCSharp6Supported())
          {
            consumer.AddHighlighting(
              new DelegateAllocationHighlighting(lambda, "from generic anonymous function (always non cached)"), closureRange);
          }
        }
      }

      foreach (var lambda in inspector.ClosurelessLambdas)
      {
        if (!IsExpressionLambda(lambda)) continue;

        var closureRange = GetClosureRange(lambda);
        if (closureRange.IsValid())
        {
          consumer.AddHighlighting(
            new ObjectAllocationHighlighting(lambda, "expression tree construction"), closureRange);
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

    private static void ReportAnonymousTypes([NotNull] ClosureInspector inspector, [NotNull] IHighlightingConsumer consumer)
    {
      foreach (var queryClause in inspector.AnonymousTypes)
      {
        var highlighting = new ObjectAllocationHighlighting(queryClause, "transparent identifier anonymous type instantiation");
        consumer.AddHighlighting(highlighting, queryClause.GetDocumentRange());
      }
    }

    private static bool IsExpressionLambda([NotNull] ITreeNode function)
    {
      var lambdaExpression = function as ILambdaExpression;
      if (lambdaExpression != null)
      {
        return lambdaExpression.IsLinqExpressionTreeLambda();
      }

      var parameterPlatform = function as IQueryParameterPlatform;
      if (parameterPlatform != null)
      {
        return parameterPlatform.IsLinqExpressionTreeQuery();
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

      private void AddCapture([NotNull] ITreeNode closure, [NotNull] IDeclaredElement element)
      {
        JetHashSet<IDeclaredElement> captures;
        if (!Closures.TryGetValue(closure, out captures))
        {
          Closures.Add(closure, captures = new JetHashSet<IDeclaredElement>());
        }

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
          ProcessParameter(parameter);
          return;
        }

        var variable = declaredElement as ILocalVariable;
        if (variable != null)
        {
          ProcessLocalVariable(variable);
          return;
        }

        var typeMember = declaredElement as ITypeMember;
        if (typeMember != null) // .this closure
        {
          ProcessTypeMember(typeMember);
          return;
        }

        var anonymousProperty = declaredElement as IQueryAnonymousTypeProperty;
        if (anonymousProperty != null)
        {
          ProcessAnonymousProperty(anonymousProperty);
        }
      }

      private void ProcessParameter([NotNull] IParameter parameter)
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
              if (parametersOwner.Equals(myFunction)) return;

              var accessor = myFunction as IAccessor;
              if (accessor != null && accessor.OwnerMember.Equals(parametersOwner)) return;
            }

            AddCapture(closure, parameter);
          }
        }
      }

      private void ProcessLocalVariable([NotNull] ILocalVariable variable)
      {
        if (variable.IsStatic) return;
        if (variable.IsConstant) return;

        var declarations = variable.GetDeclarations();
        if (declarations.Count == 1)
        {
          var declaration = declarations[0];
          foreach (var closure in myClosures)
          {
            if (closure.Contains(declaration) && !(closure is IQueryParameterPlatform)) continue;

            AddCapture(closure, variable);
          }
        }
      }

      private void ProcessTypeMember([NotNull] ITypeMember typeMember)
      {
        if (typeMember is ITypeElement) return;
        if (typeMember.IsStatic) return;

        var field = typeMember as IField;
        if (field != null && field.IsConstant) return;

        AddThisCapture();
      }

      private void ProcessAnonymousProperty([NotNull] IQueryAnonymousTypeProperty anonymousProperty)
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
