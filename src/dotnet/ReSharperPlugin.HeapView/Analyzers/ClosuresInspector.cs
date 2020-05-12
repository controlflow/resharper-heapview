using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.DeclaredElements;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Tree.Query;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace ReSharperPlugin.HeapView.Analyzers
{
  // todo: check ctor initializers

  public sealed class ClosuresInspector : IRecursiveElementProcessor
  {
    [NotNull] private readonly ICSharpDeclaration myDeclaration;
    [CanBeNull] private readonly ILocalScope myTopScope;
    [CanBeNull] private readonly IParametersOwner myTopLevelParametersOwner;
    [NotNull] private readonly Stack<ICSharpClosure> myCurrentClosures;

    public ClosuresInspector([NotNull] ICSharpDeclaration declaration, [CanBeNull] IParametersOwner topLevelParameterOwner)
    {
      myDeclaration = declaration;
      myTopScope = declaration as ILocalScope;
      myTopLevelParametersOwner = topLevelParameterOwner;
      myCurrentClosures = new Stack<ICSharpClosure>();

      Captures = new OneToSetMap<ICSharpClosure, IDeclaredElement>();
      CapturesOfScope = new OneToSetMap<ILocalScope, IDeclaredElement>();
      ClosuresOfScope = new OneToSetMap<ILocalScope, ICSharpClosure>();
      DisplayClasses = new Dictionary<ILocalScope, DisplayClassInfo>();

      AnonymousTypes = new HashSet<IQueryRangeVariableDeclaration>();
      CapturelessClosures = new List<ICSharpClosure>();
      DelayedUseLocalFunctions = new OneToListMap<ILocalFunction, IReferenceExpression>();
    }

    [CanBeNull, Pure]
    public static ClosuresInspector TryBuild([NotNull] ICSharpDeclaration declaration)
    {
      switch (declaration)
      {
        case ICSharpClosure _:
          return null; // only top-level declarations

        // block-bodied methods, constructors, accessors, operators, finalizer
        case ICSharpFunctionDeclaration { Body: { }, DeclaredElement: var parametersOwner }:
          return new ClosuresInspector(declaration, parametersOwner);

        // expression-bodied accessors + get-only properties/indexers
        case IExpressionBodyOwnerDeclaration { ArrowClause: { }, DeclaredElement: IParametersOwner parametersOwner }:
          return new ClosuresInspector(declaration, parametersOwner);

        // field/event/auto-property initializer
        case IInitializerOwnerDeclaration { Initializer: { } }:
          return new ClosuresInspector(declaration, null);

        default:
          return null;
      }
    }

    public void Run()
    {
      myDeclaration.ProcessDescendants(this);
    }

    [CanBeNull] public IParametersOwner TopLevelParametersOwner => myTopLevelParametersOwner;
    [NotNull] public OneToSetMap<ICSharpClosure, IDeclaredElement> Captures { get; }
    [Obsolete] [NotNull] public OneToSetMap<ILocalScope, IDeclaredElement> CapturesOfScope { get; }
    [Obsolete] [NotNull] public OneToSetMap<ILocalScope, ICSharpClosure> ClosuresOfScope { get; }

    public Dictionary<ILocalScope, DisplayClassInfo> DisplayClasses { get; }

    [NotNull] public List<ICSharpClosure> CapturelessClosures { get; }
    [NotNull] public HashSet<IQueryRangeVariableDeclaration> AnonymousTypes { get; }
    [NotNull] public OneToListMap<ILocalFunction, IReferenceExpression> DelayedUseLocalFunctions { get; }

    public sealed class DisplayClassInfo
    {
      public HashSet<IDeclaredElement> Captures { get; } = new HashSet<IDeclaredElement>();
      public HashSet<ICSharpClosure> Closures { get; } = new HashSet<ICSharpClosure>();
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
      Assertion.Assert(lastClosure == closure, "lastClosure == closure");

      if (!Captures.ContainsKey(closure))
      {
        CapturelessClosures.Add(closure);
      }
    }

    private void ProcessExpression(ICSharpExpression expression)
    {
      switch (expression)
      {
        case IThisExpression _:
        case IBaseExpression _:
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
      Assertion.Assert(myCurrentClosures.Count > 0, "myCurrentClosures.Count > 0");

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