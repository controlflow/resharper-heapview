using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Collections;
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
  // todo: https://sharplab.io/#v2:EYLgtghgzgLgpgJwD4AEBMBGAsAKFygZgAJ0iBhIgb1yNpOJQBYiBZACgEoqa7eBLAHYwiADyIBeIgAYA3D1615Cpb2o4FGonwBmRNmPGTGaDis2VeZzXTEBqW3PXXNAcwD2MN0QA2EYHG9HZzoAXytlJ2Dwy0jnX39vEGi6NWCFQWEATwlpILS6FABWAB4MgD4ibQBXAQBjHM4JCrsiTLz8ogB6TvdPHz8A9ucw2N4R63ClEZCgA===

  public sealed class ClosuresInspector : IRecursiveElementProcessor
  {
    [NotNull] private readonly ICSharpDeclaration myDeclaration;
    [CanBeNull] private readonly IParametersOwner myTopLevelParametersOwner;
    [NotNull] private readonly Stack<ICSharpClosure> myCurrentClosures;
    [NotNull] private readonly OneToSetMap<ICSharpClosure, IDeclaredElement> myCaptures;

    [NotNull] private readonly OneToSetMap<ILocalFunction, ILocalFunction> myLocalInvocations;
    [NotNull] private readonly HashSet<ILocalFunction> myDelayedFunctions;

    private int myDisplayClassCounter;

    public ClosuresInspector([NotNull] ICSharpDeclaration declaration, [CanBeNull] IParametersOwner topLevelParameterOwner)
    {
      myDeclaration = declaration;
      myTopLevelParametersOwner = topLevelParameterOwner;
      myCurrentClosures = new Stack<ICSharpClosure>();

      myCaptures = new OneToSetMap<ICSharpClosure, IDeclaredElement>();
      DisplayClasses = new Dictionary<IScope, DisplayClassInfo>();

      myLocalInvocations = new OneToSetMap<ILocalFunction, ILocalFunction>();
      myDelayedFunctions = new HashSet<ILocalFunction>();

      AnonymousTypes = new HashSet<IQueryRangeVariableDeclaration>();
      CapturelessClosures = new List<ICSharpClosure>();
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
        case IFieldDeclaration { Initial: { } }:
        case IEventDeclaration { Initial: { } }:
        case IPropertyDeclaration { Initial: { } }:
          return new ClosuresInspector(declaration, topLevelParameterOwner: null);

        default:
          return null;
      }
    }

    public void Run()
    {
      myDeclaration.ProcessThisAndDescendants(this);

      ConnectDisplayClassesToParentOnes();
      OptimizeDisplayClasses();
    }

    [CanBeNull] public IParametersOwner TopLevelParametersOwner => myTopLevelParametersOwner;

    [NotNull] public Dictionary<IScope, DisplayClassInfo> DisplayClasses { get; }
    [NotNull] public List<ICSharpClosure> CapturelessClosures { get; }
    [NotNull] public HashSet<IQueryRangeVariableDeclaration> AnonymousTypes { get; }

    #region Display classes

    public sealed class DisplayClassInfo
    {
      public DisplayClassInfo(int index)
      {
        Index = index;
        Closures = new OneToSetMap<ICSharpClosure, IDeclaredElement>();
        ScopeMembers = new HashSet<IDeclaredElement>();
        Kind = DisplayClassKind.Class;
      }

      public int Index { get; }
      public HashSet<IDeclaredElement> ScopeMembers { get; }
      public OneToSetMap<ICSharpClosure, IDeclaredElement> Closures { get; }

      // we can only know it when we are exiting

      [CanBeNull] public DisplayClassInfo ParentDisplayClass { get; set; }

      public TreeTextRange FirstCapturedVariableLocation { get; private set; }

      public DisplayClassKind Kind { get; set; }

      public void AddCapture([NotNull] IDeclaredElement capture)
      {
        ScopeMembers.Add(capture);

        // update 'FirstCapturedVariableLocation'
      }

      public HashSet<IDeclaredElement> GetDangerousToCaptureElements()
      {
        var captureElements = new HashSet<IDeclaredElement>();

        foreach (var member in ScopeMembers)
        {
          // todo: include structs, unconstrained generics
          if (member is ITypeOwner { Type: { Classify: TypeClassification.REFERENCE_TYPE } })
          {
            captureElements.Add(member);
          }
        }

        return captureElements;
      }
    }

    public enum DisplayClassKind
    {
      Class,
      Struct,
      ClassInstance
    }

    private void CreateOrUpdateDisplayClassForCapture([NotNull] IDeclaredElement capture)
    {
      var captureScope = GetScopeForCapture(capture);

      if (myCurrentClosures.TryPeek() is { } currentClosure && currentClosure.Contains(captureScope))
      {
        return; // not a capture
      }

      if (!DisplayClasses.TryGetValue(captureScope, out var displayClass))
      {
        displayClass = new DisplayClassInfo(myDisplayClassCounter++);
        DisplayClasses.Add(captureScope, displayClass);
      }

      displayClass.AddCapture(capture);
    }

    private void ConnectDisplayClassesToParentOnes()
    {
      foreach (var (scope, displayClass) in DisplayClasses)
      foreach (var (_, captures) in displayClass.Closures)
      {
        if (captures.IsSubsetOf(displayClass.ScopeMembers)) continue;

        // connect to containing display class containing closures
        foreach (var containingScope in scope.ContainingScopes(returnThis: false))
        {
          if (DisplayClasses.TryGetValue(containingScope, out var parentDisplayClass))
          {
            displayClass.ParentDisplayClass = parentDisplayClass;
            break;
          }
        }
      }
    }

    private void OptimizeDisplayClasses()
    {
      if (myTopLevelParametersOwner != null)
      {
        var topScope = GetScopeForCapture(myTopLevelParametersOwner);

        if (DisplayClasses.TryGetValue(topScope, out var topDisplayClass))
        {
          if (topDisplayClass.ScopeMembers.Count == 1
              && topDisplayClass.ScopeMembers.Contains(myTopLevelParametersOwner))
          {
            topDisplayClass.Kind = DisplayClassKind.ClassInstance;
          }
        }
      }
    }

    [NotNull, Pure]
    private IScope GetScopeForCapture([NotNull] IDeclaredElement capture)
    {
      if (capture is IParameter { IsValueVariable: true } valueParameter)
      {
        var accessor = (IAccessor) valueParameter.ContainingParametersOwner.NotNull();
        var accessorDeclaration = accessor.GetSingleDeclaration<IAccessorDeclaration>().NotNull();

        return (IScope) accessorDeclaration;
      }

      if (capture.Equals(myTopLevelParametersOwner))
      {
        switch (myDeclaration)
        {
          case ICSharpFunctionDeclaration { Body: { } body }:
            return (IScope) body;
          case IExpressionBodyOwnerDeclaration { ArrowClause: { } arrowClause }:
            return (IScope) arrowClause;
        }

        Assertion.Fail("Should not be reachable");
      }

      var firstDeclaration = capture.GetFirstDeclaration<ICSharpDeclaration>().NotNull();
      var containingScope = firstDeclaration.GetContainingScope(returnThis: false).NotNull();

      return containingScope;
    }

    #endregion
    #region Before interior

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

    private void ProcessExpression([NotNull] ICSharpExpression expression)
    {
      switch (expression)
      {
        case IThisExpression _:
        case IBaseExpression _:
        {
          AddThisCapture();
          break;
        }

        case IReferenceExpression { QualifierExpression: null } referenceExpression:
        {
          ProcessNotQualifiedReferenceExpression(referenceExpression);
          break;
        }
      }
    }

    private void ProcessNotQualifiedReferenceExpression([NotNull] IReferenceExpression referenceExpression)
    {
      if (referenceExpression.IsNameofOperatorArgumentPart()) return;

      var (declaredElement, _) = referenceExpression.Reference.Resolve();

      if (declaredElement is ILocalFunction function)
      {
        ProcessLocalFunctionUsage(function, referenceExpression);
      }

      if (myCurrentClosures.Count > 0 && declaredElement != null)
      {
        ProcessElementUsedByNonQualifiedReferenceExpressionInsideClosure(declaredElement, referenceExpression);
      }
    }

    private void ProcessElementUsedByNonQualifiedReferenceExpressionInsideClosure(
      [NotNull] IDeclaredElement declaredElement, [NotNull] IReferenceExpression referenceExpression)
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

    #endregion
    #region Captures

    private void AddParameterCapture([NotNull] IParameter parameter)
    {
      var parameterOwner = parameter.ContainingParametersOwner;
      if (parameterOwner == null) return; // should not happen anyway

      CreateOrUpdateDisplayClassForCapture(parameter);

      foreach (var closure in myCurrentClosures)
      {
        if (ReferenceEquals(parameterOwner, closure)) break;

        myCaptures.Add(closure, parameter);
      }
    }

    private void AddLocalVariableCapture([NotNull] ILocalVariable localVariable)
    {
      if (localVariable.IsConstant) return;

      CreateOrUpdateDisplayClassForCapture(localVariable);

      var variableDeclaration = localVariable.GetSingleDeclaration<ICSharpDeclaration>().NotNull();

      foreach (var closure in myCurrentClosures)
      {
        if (closure.Contains(variableDeclaration)
            // todo: query inside query?
            && !(closure is IQueryParameterPlatform)) break;

        myCaptures.Add(closure, localVariable);
      }
    }

    private void AddLocalFunctionCapture([NotNull] ILocalFunction localFunction)
    {
      if (localFunction.IsStatic) return; // todo: is this correct?

      CreateOrUpdateDisplayClassForCapture(localFunction);

      var localFunctionDeclaration = localFunction.GetSingleDeclaration<ILocalFunctionDeclaration>().NotNull();

      foreach (var closure in myCurrentClosures)
      {
        if (closure.Contains(localFunctionDeclaration)) break;

        myCaptures.Add(closure, localFunction);
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

    private void AddThisCapture()
    {
      if (myCurrentClosures.Count == 0) return;

      var parametersOwner = myTopLevelParametersOwner;
      if (parametersOwner == null) return;

      if (parametersOwner is ITypeMember { IsStatic: true }) return;

      CreateOrUpdateDisplayClassForCapture(parametersOwner);

      foreach (var closure in myCurrentClosures)
      {
        myCaptures.Add(closure, parametersOwner);
      }
    }

    #endregion
    #region After interior

    public void ProcessAfterInterior(ITreeNode element)
    {
      if (element is ICSharpClosure closure)
      {
        ProcessClosureAfterInterior(closure);
      }

      if (element is IScope scope)
      {
        ProcessDisplayClassAfterScopeInterior(scope);
      }
    }

    private void ProcessClosureAfterInterior([NotNull] ICSharpClosure closure)
    {
      var lastClosure = myCurrentClosures.Pop();
      Assertion.Assert(lastClosure == closure, "lastClosure == closure");

      var captures = myCaptures[closure];
      if (captures.Count > 0)
      {
        AppendClosureToContainingDisplayClass();
        myCaptures.RemoveKey(closure);
      }
      else
      {
        CapturelessClosures.Add(closure);
      }

      void AppendClosureToContainingDisplayClass()
      {
        foreach (var containingScope in closure.ContainingScopes(returnThis: false))
        {
          if (!DisplayClasses.TryGetValue(containingScope, out var displayClass)) continue;

          // attach only if captures have intersection with some containing display class
          if (!captures.Overlaps(displayClass.ScopeMembers)) continue;

          // copy data
          displayClass.Closures.AddRange(closure, captures);
          return;
        }

        Assertion.Fail("Should not be reachable");
      }
    }

    private void ProcessDisplayClassAfterScopeInterior([NotNull] IScope scope)
    {
      if (!scope.IsActiveScope()) return;

      var displayClass = DisplayClasses.TryGetValue(scope);
      if (displayClass == null) return;

      var dangerousCaptures = displayClass.GetDangerousToCaptureElements();

      foreach (var closure in displayClass.Closures)
      {
        

        // can compute implicit capture
        //   compute "dangerous" members first
        //   - base display class members can be "dangerous" as well :/
        //   compute intersection with "dangerous"
        //   note somewhere
        // can compute outer capture



        // todo: do this later?
        // Captures.RemoveKey(closure);
      }
    }

    #endregion
    #region Local functions

    private void ProcessLocalFunctionUsage([NotNull] ILocalFunction localFunction, [NotNull] IReferenceExpression referenceExpression)
    {
      var containingExpression = referenceExpression.GetContainingParenthesizedExpression();
      var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(containingExpression);
      if (invocationExpression == null)
      {
        // note: nameof(LocalFunc) already filtered here
        myDelayedFunctions.Add(localFunction);
      }
      else
      {
        // todo: should not be applied inside delayed func itself
        if (IsInsideDelayedBodyClosure())
        {
          myDelayedFunctions.Add(localFunction);
        }
        else
        {
          if (myCurrentClosures.Count > 0)
          {
            var currentClosure = myCurrentClosures.Peek();
            if (currentClosure is ILocalFunctionDeclaration { DeclaredElement: var currentLocalFunction })
            {
              myLocalInvocations.Add(localFunction, currentLocalFunction);
            }
          }
        }
      }
    }

    // todo: should not work for local functions
    [Pure]
    private bool IsInsideDelayedBodyClosure()
    {
      if (myCurrentClosures.Count == 0) return false;

      var closure = myCurrentClosures.Peek();
      if (closure is ILocalFunctionDeclaration { DeclaredElement: var localFunction })
      {
        return localFunction.IsIterator || localFunction.IsAsync;
      }

      return false;
    }

    

    

    #endregion

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
  }
}