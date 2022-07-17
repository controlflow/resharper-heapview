using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.DeclaredElements;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Tree.Query;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using ReSharperPlugin.HeapView.Highlightings;

// ReSharper disable ConvertClosureToMethodGroup
// ReSharper disable RedundantExplicitParamsArrayCreation

namespace ReSharperPlugin.HeapView.Analyzers;
// todo: generics can be introduces by local functions - not a problem in Roslyn
// todo: report delegate allocations from method group
// todo: implement "Implicitly captured closure"

[ElementProblemAnalyzer(
  ElementTypes: new[] {
    // constructors, methods, operators, accessors
    typeof(ICSharpFunctionDeclaration),
    // expression-bodied properties/indexers
    typeof(IExpressionBodyOwnerDeclaration),
    // field/event/auto-property initializers
    typeof(IFieldDeclaration),
    typeof(IEventDeclaration),
    typeof(IPropertyDeclaration),
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
    var bodyToAnalyze = TryGetCodeBodyToAnalyze(declaration);
    if (bodyToAnalyze == null) return;

    var function = declaration.DeclaredElement as IParametersOwner;
    var topScope = bodyToAnalyze as ILocalScope;

    // collect closures and their captures first
    var inspector = new ClosuresInspector(function, topScope);
    bodyToAnalyze.ProcessDescendants(inspector);

    // report closures allocations
    if (inspector.Captures.Count > 0)
    {
      ReportClosureAllocations(declaration, function, inspector, consumer);
    }

    // todo: not the case in CS 6+?
    // report non-cached generic lambda expressions
    if (function != null && inspector.CapturelessClosures.Count > 0)
    {
      ReportClosurelessAllocations(declaration, function, inspector, consumer);
    }

    // report anonymous types in query expressions
    if (inspector.AnonymousTypes.Count > 0)
    {
      ReportAnonymousTypes(inspector, consumer);
    }
  }

  [CanBeNull, Pure]
  private static ITreeNode TryGetCodeBodyToAnalyze(ITreeNode declaration)
  {
    switch (declaration)
    {
      case ICSharpFunctionDeclaration { Body: { } bodyBlock }:
        return bodyBlock;
      case IExpressionBodyOwnerDeclaration { ArrowClause: { } arrowClause }:
        return arrowClause;
      case IInitializerOwnerDeclaration { Initializer: { } initializer }:
        return initializer;
      default:
        return null;
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
    [NotNull] ITreeNode element, [NotNull] IParametersOwner function, [NotNull] ClosuresInspector inspector, [NotNull] IHighlightingConsumer consumer)
  {
    if (function is ITypeParametersOwner typeParametersOwner && typeParametersOwner.TypeParameters.Count > 0)
    {
      foreach (var closure in inspector.CapturelessClosures)
      {
        if (IsExpressionTreeClosure(closure)) continue;

        var closureRange = GetClosureRange(closure);
        if (!closureRange.IsValid()) continue;

        // note: Roslyn compiler implements caching of such closures
        if (!element.IsCSharp6Supported())
        {
          consumer.AddHighlighting(
            new DelegateAllocationHighlighting(closure, "from generic anonymous function (always non cached)"), closureRange);
        }
      }
    }

    foreach (var closure in inspector.CapturelessClosures)
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

  private sealed class ClosuresInspector : IRecursiveElementProcessor
  {
    [CanBeNull] private readonly IParametersOwner myTopLevelParametersOwner;
    [CanBeNull] private readonly ILocalScope myTopScope;
    [NotNull] private readonly Stack<ICSharpClosure> myCurrentClosures = new();

    public ClosuresInspector([CanBeNull] IParametersOwner topLevelParameterOwner, [CanBeNull] ILocalScope topScope)
    {
      myTopLevelParametersOwner = topLevelParameterOwner;
      myTopScope = topScope;
    }

    [NotNull] public OneToSetMap<ICSharpClosure, IDeclaredElement> Captures { get; } = new();
    [Obsolete] [NotNull] public OneToSetMap<ILocalScope, IDeclaredElement> CapturesOfScope { get; } = new();
    [Obsolete] [NotNull] public OneToSetMap<ILocalScope, ICSharpClosure> ClosuresOfScope { get; } = new();

    public Dictionary<ILocalScope, DisplayClassInfo> DisplayClasses { get; } = new();

    [NotNull] public List<ICSharpClosure> CapturelessClosures { get; } = new();
    [NotNull] public HashSet<IQueryRangeVariableDeclaration> AnonymousTypes { get; } = new();
    [NotNull] public OneToListMap<ILocalFunction, IReferenceExpression> DelayedUseLocalFunctions { get; } = new();

    public sealed class DisplayClassInfo
    {
      public HashSet<IDeclaredElement> Captures { get; } = new();
      public HashSet<ICSharpClosure> Closures { get; } = new();
      public DisplayClassInfo ParentDisplayClass { get; private set; }

      public TreeTextRange FirstCapturedVariableLocation { get; private set; }





    }

    public bool ProcessingIsFinished => false;
    public bool InteriorShouldBeProcessed(ITreeNode element) => true;

    public void ProcessBeforeInterior(ITreeNode element)
    {
      if (element is ICSharpClosure closure)
      {
        myCurrentClosures.Push(closure);
      }

      if (element is ICSharpExpression expression)
      {
        ProcessExpression(expression);
      }
    }

    public void ProcessAfterInterior(ITreeNode element)
    {
      if (element is ICSharpClosure closure)
      {
        ProcessClosureAfterInterior(closure);
      }
    }

    private void ProcessClosureAfterInterior([NotNull] ICSharpClosure closure)
    {
      var lastClosure = myCurrentClosures.Pop();
      Assertion.Assert(lastClosure == closure);

      if (!Captures.ContainsKey(closure))
      {
        CapturelessClosures.Add(closure);
      }
    }

    private void ProcessExpression(ICSharpExpression expression)
    {
      switch (expression)
      {
        case IThisExpression:
        case IBaseExpression:
          if (myCurrentClosures.Count > 0) AddThisCapture();
          break;

        case IReferenceExpression { QualifierExpression: null } referenceExpression:
          ProcessNonQualifiedReferenceExpression(referenceExpression);
          break;
      }
    }

    private void AddThisCapture()
    {
      var topLevelParametersOwner = myTopLevelParametersOwner;
      if (topLevelParametersOwner == null) return;



      NoteCaptureInTopLevelScope(topLevelParametersOwner);

      foreach (var closure in myCurrentClosures)
      {
        Captures.Add(closure, topLevelParametersOwner);
      }
    }

    private void NoteCaptureInTopLevelScope([NotNull] IDeclaredElement capturedElement)
    {
      Assertion.Assert(myCurrentClosures.Count > 0);

      if (myTopScope != null)
      {
        CapturesOfScope.Add(myTopScope, capturedElement);
      }
    }

    private void ProcessNonQualifiedReferenceExpression([NotNull] IReferenceExpression referenceExpression)
    {
      if (referenceExpression.IsNameofOperatorArgumentPart()) return;

      var (declaredElement, _) = referenceExpression.Reference.Resolve();

      if (declaredElement is ILocalFunction function)
      {
        ProcessLocalFunctionUsage(function, referenceExpression);
      }

      if (myCurrentClosures.Count > 0 && declaredElement != null)
      {
        ProcessElementUsedByNonQualifiedReferenceExpressionInClosure(declaredElement);
      }
    }

    private void ProcessElementUsedByNonQualifiedReferenceExpressionInClosure([NotNull] IDeclaredElement declaredElement)
    {
      switch (declaredElement)
      {
        case IParameter parameter:
          AddParameterCapture(parameter);
          return;

        case ILocalVariable localVariable:
          AddLocalVariableCapture(localVariable);
          break;

        case ILocalFunction localFunction:
          AddLocalFunctionCapture(localFunction);
          break;

        case ITypeMember typeMember:
          AddThisCaptureViaMemberUsage(typeMember);
          break;

        case IQueryAnonymousTypeProperty anonymousTypeProperty:
          ProcessAnonymousProperty(anonymousTypeProperty);
          break;

        // note: ITypeParameter capture do not introduces allocations in Roslyn-generated code
      }
    }

    private void AddParameterCapture([NotNull] IParameter parameter)
    {
      var parametersOwner = parameter.ContainingParametersOwner;
      if (parametersOwner == null) return; // should not happen anyway

      foreach (var closure in myCurrentClosures)
      {
        if (ReferenceEquals(parametersOwner, closure))
        {
          // todo: test with query parameter platforms
          var parameterScope = closure.GetContainingScope<ILocalScope>(returnThis: true);
          if (parameterScope != null)
          {
            CapturesOfScope.Add(parameterScope, parameter);
            ClosuresOfScope.Add(parameterScope, closure); // todo: only this one? NO
          }

          return;
        }

        Captures.Add(closure, parameter);
      }

      // todo: test with indexer parameters/value parameter
      if (parametersOwner.Equals(myTopLevelParametersOwner))
      {
        NoteCaptureInTopLevelScope(parameter);
      }
    }

    private void AddLocalVariableCapture([NotNull] ILocalVariable localVariable)
    {
      if (localVariable.IsConstant) return;

      var variableDeclaration = localVariable.GetSingleDeclaration<IDeclaration>();
      if (variableDeclaration == null) return;

      var variableScope = variableDeclaration.GetContainingScope<ILocalScope>(returnThis: true);
      Assertion.AssertNotNull(variableScope, "Local scope expected");

      // todo: test with out vars/pattern variables in weird scopes
      CapturesOfScope.Add(variableScope, localVariable);

      foreach (var closure in myCurrentClosures)
      {
        if (closure.Contains(variableDeclaration) && !(closure is IQueryParameterPlatform)) break;

        Captures.Add(closure, localVariable);
        ClosuresOfScope.Add(variableScope, closure);
      }
    }

    private void AddLocalFunctionCapture([NotNull] ILocalFunction localFunction)
    {
      var localFunctionDeclaration = localFunction.GetSingleDeclaration<ILocalFunctionDeclaration>();
      if (localFunctionDeclaration == null) return;

      var functionScope = localFunctionDeclaration.GetContainingScope<ILocalScope>(returnThis: true);
      Assertion.AssertNotNull(functionScope, "Local scope expected");

      CapturesOfScope.Add(functionScope, localFunction);

      foreach (var closure in myCurrentClosures)
      {
        if (closure.Contains(localFunctionDeclaration)) break;

        Captures.Add(closure, localFunction);
        ClosuresOfScope.Add(functionScope, closure);
      }
    }

    private void AddThisCaptureViaMemberUsage([NotNull] ITypeMember typeMember)
    {
      if (typeMember is ITypeElement) return;
      if (typeMember.IsStatic) return;

      if (typeMember is IField { IsField: false }) return;

      // todo: test with indexer's parameters (on expr-bodied indexers + accessors)
      // todo: test on setter's value parameter

      AddThisCapture();
    }

    private void ProcessLocalFunctionUsage([NotNull] ILocalFunction localFunction, [NotNull] IReferenceExpression referenceExpression)
    {
      var containingExpression = referenceExpression.GetContainingParenthesizedExpression();
      var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(containingExpression);
      if (invocationExpression == null)
      {
        // note: nameof(LocalFunc) already filtered here

        DelayedUseLocalFunctions.Add(localFunction, referenceExpression);
      }
    }

    // note: invoked only inside closures
    private void ProcessAnonymousProperty([NotNull] IQueryAnonymousTypeProperty anonymousProperty)
    {
      foreach (var anonymousTypeProperty in anonymousProperty.ContainingType.Properties)
      {
        var property = (IQueryAnonymousTypeProperty) anonymousTypeProperty;
        var declaration = property.Declaration;

        if (QueryFirstFromNavigator.GetByDeclaration(declaration) == null
            && QueryContinuationNavigator.GetByDeclaration(declaration) == null)
        {
          AnonymousTypes.Add(declaration);
        }
      }
    }

    public bool IsDisplayClassForScopeCanBeLoweredToStruct([NotNull] ILocalScope scope)
    {
      foreach (var closure in ClosuresOfScope[scope])
      {
        if (closure is ILocalFunctionDeclaration localFunctionDeclaration)
        {
          if (DelayedUseLocalFunctions.ContainsKey(localFunctionDeclaration.DeclaredElement))
          {
            return false;
          }
        }
        else // lambdas, query - all delayed
        {
          return false;
        }
      }

      return true;
    }
  }
}