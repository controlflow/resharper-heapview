using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.DeclaredElements;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Tree.Query;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using JetBrains.Util.DataStructures.Collections;

namespace ReSharperPlugin.HeapView.Analyzers;

public sealed class DisplayClassStructure : IRecursiveElementProcessor, IDisposable
{
  private readonly ITreeNode myDeclaration;
  private readonly ITreeNode? myThisReferenceCaptureScopeNode;

  private readonly PooledStack<ICSharpClosure> myCurrentClosures = PooledStack<ICSharpClosure>.GetInstance();
  private readonly PooledList<ICSharpClosure> myAllFoundClosures = PooledList<ICSharpClosure>.GetInstance();
  private readonly PooledDictionary<ITreeNode, DisplayClass> myScopeToDisplayClass = PooledDictionary<ITreeNode, DisplayClass>.GetInstance();
  private readonly PooledDictionary<ICSharpClosure, ClosureCaptures> myClosureToCaptures = PooledDictionary<ICSharpClosure, ClosureCaptures>.GetInstance();
  private readonly PooledHashSet<ILocalFunction> myLocalFunctionsConvertedToDelegates = PooledHashSet<ILocalFunction>.GetInstance();

  private bool myIsScanningNonMainPart;
  private PooledHashSet<string>? myCaptureNamesToLookInOtherParts;

  private DisplayClassStructure(ITreeNode declaration)
  {
    myDeclaration = declaration;
    myThisReferenceCaptureScopeNode = FindThisCaptureScope(declaration);
  }

  public static DisplayClassStructure? Build(ITreeNode declaration)
  {
    DisplayClassStructure? structure = null;

    try
    {
      switch (declaration)
      {
        // record R(int X) : B(...) { int Member = ...; }
        case IClassLikeDeclaration classLikeDeclaration:
        {
          TryBuildFromInitializerScope(classLikeDeclaration, ref structure);
          break;
        }

        // void Method() { }
        // int Property { get { } }
        // public Ctor() : base(...) { }
        case ICSharpFunctionDeclaration { Body: { } bodyBlock } functionDeclaration:
        {
          structure = new DisplayClassStructure(declaration);

          if (functionDeclaration is IConstructorDeclaration { Initializer: { } constructorInitializer })
          {
            constructorInitializer.ProcessThisAndDescendants(structure);
          }

          bodyBlock.ProcessThisAndDescendants(structure);
          break;
        }

        // int ExpressionBodiedProperty => expr;
        case IExpressionBodyOwnerDeclaration { ArrowClause: { } arrowClause }:
        {
          structure = new DisplayClassStructure(declaration);
          arrowClause.ProcessThisAndDescendants(structure);
          break;
        }

        // TopLevelCode();
        case ITopLevelCode topLevelCode:
        {
          structure = new DisplayClassStructure(declaration);
          topLevelCode.ProcessThisAndDescendants(structure);
          break;
        }
      }

      structure?.FinalizeStructure();

      return structure;
    }
    catch
    {
      structure?.Dispose();
      throw;
    }
  }

  private static void TryBuildFromInitializerScope(IClassLikeDeclaration classLikeDeclaration, ref DisplayClassStructure? structure)
  {
    var seenBodies = false;

    // if type declaration is partial and has primary parameters in some part
    // we need to scan the other parts to find possible captures of such parameters
    if (classLikeDeclaration is
        {
          IsPartial: true,
          DeclaredElement: IRecord { PrimaryConstructor.Parameters: { Count: > 0 } parameters } record
        })
    {
      var allDeclarations = record.GetDeclarations();
      if (allDeclarations.Count > 1)
      {
        var parameterNames = PooledHashSet<string>.GetInstance();
        foreach (var parameter in parameters)
          parameterNames.Add(parameter.ShortName);

        structure = new DisplayClassStructure(classLikeDeclaration);
        structure.myCaptureNamesToLookInOtherParts = parameterNames;

        foreach (var partDeclaration in allDeclarations)
        {
          if (partDeclaration is IClassLikeDeclaration thisPartDeclaration)
          {
            structure!.myIsScanningNonMainPart = thisPartDeclaration != classLikeDeclaration;

            ScanInitializerScope(thisPartDeclaration, classLikeDeclaration, ref structure, ref seenBodies);
          }
        }

        if (!seenBodies)
        {
          structure!.Dispose();
          structure = null;
        }

        return;
      }
    }

    ScanInitializerScope(classLikeDeclaration, classLikeDeclaration, ref structure, ref seenBodies);
    return;

    static void ScanInitializerScope(
      IClassLikeDeclaration classLikeDeclaration,
      IClassLikeDeclaration originalDeclaration,
      ref DisplayClassStructure? structure,
      ref bool seenBodies)
    {
      // looks for closures in 'record R() : B(...)'
      var extendsList = classLikeDeclaration.ExtendsList;
      if (extendsList != null)
      {
        foreach (var extendedType in extendsList.ExtendedTypesEnumerable)
        {
          var argumentList = extendedType.ArgumentList;
          if (argumentList != null)
          {
            structure ??= new DisplayClassStructure(originalDeclaration);
            argumentList.ProcessThisAndDescendants(structure);
            seenBodies = true;
            break;
          }
        }
      }

      // check closures in 'class C { int Field = ...; }'
      foreach (var memberDeclaration in classLikeDeclaration.MemberDeclarations)
      {
        if (memberDeclaration is IInitializerOwnerDeclaration { Initializer: IVariableInitializer initializer })
        {
          // optimization: there can be no references to primary parameters from static member initializers
          if (structure is { myIsScanningNonMainPart: true } && memberDeclaration.IsStatic) continue;

          structure ??= new DisplayClassStructure(originalDeclaration);
          initializer.ProcessThisAndDescendants(structure);
          seenBodies = true;
        }
      }
    }
  }

  private static readonly ObjectPool<PooledHashSet<IDeclaredElement>> CapturesPool = PooledHashSet<IDeclaredElement>.CreatePool();

  private void FinalizeStructure()
  {
    // 1. Iteratively propagate local function captures into dislay class captures
    bool modified;

    do
    {
      modified = false;

      using var additionalCapturesByLocalFunction = CapturesPool.Allocate();

      foreach (var (_, currentCaptures) in myClosureToCaptures)
      {
        additionalCapturesByLocalFunction.Clear();

        foreach (var capturedEntity in currentCaptures.CapturedEntities)
        {
          if (capturedEntity is ILocalFunction capturedLocalFunction)
          {
            var localFunctionDeclaration = capturedLocalFunction.GetSingleDeclaration<ILocalFunctionDeclaration>();
            if (localFunctionDeclaration != null
                && myClosureToCaptures.TryGetValue(localFunctionDeclaration, out var localFunctionCaptures))
            {
              foreach (var otherDisplayClass in localFunctionCaptures.CapturedDisplayClasses)
              {
                modified |= currentCaptures.CapturedDisplayClasses.Add(otherDisplayClass);
              }

              // include local function's captures into other's closure direct captures
              foreach (var addictionalCapture in localFunctionCaptures.CapturedEntities)
              {
                if (addictionalCapture is not ILocalFunction)
                  additionalCapturesByLocalFunction.Add(addictionalCapture);
              }
            }
          }
        }

        foreach (var additionalCapture in additionalCapturesByLocalFunction)
        {
          modified |= currentCaptures.CapturedEntities.Add(additionalCapture);
        }
      }
    } while (modified);

    // 2. Attach closures to the appropriate display classes
    foreach (var (closure, captures) in myClosureToCaptures)
    {
      Assertion.Assert(captures.CapturedEntities.Count > 0);

      var canTakeRefDisplayClass = CanTakeDisplayClassViaRefParameter(closure);

      var capturedDisplayClasses = captures.CapturedDisplayClasses;
      if (capturedDisplayClasses.Count == 0)
      {
        // only references local functions
      }
      else if (capturedDisplayClasses.Count == 1)
      {
        foreach (var singleDisplayClass in capturedDisplayClasses)
        {
          captures.AssignToDisplayClass(singleDisplayClass, canTakeRefDisplayClass);
          break;
        }
      }
      else
      {
        using var displayClasses = PooledList<DisplayClass>.GetInstance();

        displayClasses.AddRange(capturedDisplayClasses);
        displayClasses.Sort();

        captures.AssignToDisplayClass(displayClasses[0], canTakeRefDisplayClass);

        // join display classes together
        for (var index = 1; index < displayClasses.Count; index++)
        {
          var inner = displayClasses[index - 1];
          var containing = displayClasses[index];

          inner.SetContainingDisplayClassReference(containing);
        }
      }
    }

    // 3. Additional fixup for struct lowering optimization
    do
    {
      modified = false;

      foreach (var (_, displayClass) in myScopeToDisplayClass)
      {
        // if some non-struct display class references parent struct display class,
        // we must promote the parent to non-struct kind as well
        if (!displayClass.IsStruct)
        {
          var containingDisplayClass = displayClass.ContainingDisplayClass;
          if (containingDisplayClass is { IsStruct: true })
          {
            containingDisplayClass.IsStruct = false;
            modified = true;
          }
        }
      }
    } while (modified);

    // 4. Apply 'this' capture optimization
    if (myThisReferenceCaptureScopeNode != null)
    {
      // check top-level display class to only contain 'this' reference member
      if (myScopeToDisplayClass.TryGetValue(myThisReferenceCaptureScopeNode, out var thisCaptureDisplayClass)
          && thisCaptureDisplayClass.ContainingDisplayClass == null
          && thisCaptureDisplayClass.Members.SingleItem() is ITypeElement)
      {
        thisCaptureDisplayClass.IsReplacedWithInstanceMethods = true;

        // remove the reference to this display class from all other classes
        foreach (var (_, displayClass) in myScopeToDisplayClass)
        {
          if (displayClass.ContainingDisplayClass == thisCaptureDisplayClass)
          {
            displayClass.ContainingDisplayClass = null;
          }
        }
      }
    }

    [Pure]
    bool CanTakeDisplayClassViaRefParameter(ICSharpClosure closure)
    {
      if (closure is ILocalFunctionDeclaration localFunctionDeclaration)
      {
        if (localFunctionDeclaration.IsAsync) return false;
        if (localFunctionDeclaration.IsIterator) return false;

        if (myLocalFunctionsConvertedToDelegates.Contains(localFunctionDeclaration.DeclaredElement))
          return false;

        return true; // only directly invoked
      }

      // all other closure kinds always implicitly converted to delegate instance
      return false;
    }
  }

  public void Dispose()
  {
    foreach (var (_, displayClass) in myScopeToDisplayClass)
    {
      displayClass.Free();
    }

    foreach (var (_, captures) in myClosureToCaptures)
    {
      captures.Free();
    }

    myCurrentClosures.Dispose();
    myAllFoundClosures.Dispose();
    myScopeToDisplayClass.Dispose();
    myClosureToCaptures.Dispose();
    myLocalFunctionsConvertedToDelegates.Dispose();
    myCaptureNamesToLookInOtherParts?.Dispose();
  }

  bool IRecursiveElementProcessor.InteriorShouldBeProcessed(ITreeNode element)
  {
    switch (element)
    {
      case ITypeUsage:
      case IInvocationExpression invocationExpression when invocationExpression.IsNameofOperator():
      case IAttributeSectionList: // attrs from local functions and lambdas
        return false;

      default:
        return true;
    }
  }

  bool IRecursiveElementProcessor.ProcessingIsFinished => false;

  void IRecursiveElementProcessor.ProcessBeforeInterior(ITreeNode element)
  {
    switch (element)
    {
      case ICSharpClosure closure:
      {
        myCurrentClosures.Push(closure);

        if (!myIsScanningNonMainPart)
        {
          myAllFoundClosures.Add(closure);
        }

        break;
      }

      case IReferenceExpression { IsQualified: false } referenceExpression:
      {
        if (myIsScanningNonMainPart)
          ProcessReferenceExpressionInOtherTypePart(referenceExpression);
        else
          ProcessReferenceExpression(referenceExpression);

        break;
      }

      case IThisExpression or IBaseExpression:
      {
        if (myThisReferenceCaptureScopeNode != null)
        {
          ProcessThisReferenceCapture((ICSharpExpression)element);
        }

        break;
      }
    }
  }

  private void ProcessReferenceExpression(IReferenceExpression referenceExpression)
  {
    // we need to process all the local function invocations, not only inside the closures
    var resolveResult = referenceExpression.Reference.Resolve();
    if (resolveResult.DeclaredElement is ILocalFunction { IsStatic: false } localFunction)
    {
      if (!IsDirectInvocation(referenceExpression))
      {
        myLocalFunctionsConvertedToDelegates.Add(localFunction);
      }
    }

    if (myCurrentClosures.Count > 0)
    {
      ProcessReferenceExpressionInsideClosure(referenceExpression, resolveResult);
    }
  }

  private void ProcessReferenceExpressionInOtherTypePart(IReferenceExpression referenceExpression)
  {
    Assertion.Assert(myIsScanningNonMainPart);

    if (myCurrentClosures.Count > 0)
    {
      var reference = referenceExpression.Reference;

      if (myCaptureNamesToLookInOtherParts != null
          && myCaptureNamesToLookInOtherParts.Contains(reference.GetName()))
      {
        var resolveResult = reference.Resolve();
        if (resolveResult.DeclaredElement is IParameter { ContainingParametersOwner: IPrimaryConstructor } primaryParameter)
        {
          // note: use current type as a scope for primary parameter captures in other type parts
          Assertion.Assert(myDeclaration is IClassLikeDeclaration);

          var displayClass = myScopeToDisplayClass.GetOrCreateValue(myDeclaration, static key => DisplayClass.Create(key));
          displayClass.AddMember(primaryParameter);
        }
      }
    }
  }

  private void ProcessReferenceExpressionInsideClosure(
    IReferenceExpression referenceExpression, ResolveResultWithInfo resolveResult)
  {
    switch (resolveResult.DeclaredElement)
    {
      case ICSharpLocalVariable { IsConstant: false, ReferenceKind: ReferenceKind.VALUE } localVariable:
      {
        var variableDeclaration = localVariable.GetSingleDeclaration<ICSharpDeclaration>();
        if (variableDeclaration == null) return; // should never happen

        var localScopeNode = variableDeclaration.GetContainingScope<ILocalScope>();
        if (localScopeNode != null)
        {
          NoteCapture(localScopeNode, localVariable);
        }

        break;
      }

      case IParameter { Kind: ParameterKind.VALUE } parameter:
      {
        var parameterScopeNode = FindParameterScopeNode(parameter, referenceExpression);
        if (parameterScopeNode != null)
        {
          NoteCapture(parameterScopeNode, parameter);
        }

        break;
      }

      case ILocalFunction { IsStatic: false } localFunction:
      {
        var localFunctionDeclaration = localFunction.GetSingleDeclaration<ILocalFunctionDeclaration>();
        if (localFunctionDeclaration == null) return; // should never happen

        var localScopeNode = localFunctionDeclaration.GetContainingScope<ILocalScope>();
        if (localScopeNode != null)
        {
          NoteLocalFunctionUsage(localFunctionDeclaration, localScopeNode);
        }

        break;
      }

      case ITypeMember { IsStatic: false }:
      {
        ProcessThisReferenceCaptureInsideClosure(referenceExpression);
        break;
      }
    }

    void NoteCapture(ITreeNode localScope, ITypeOwner capturedEntity)
    {
      var currentClosure = myCurrentClosures.Peek();
      if (currentClosure.Contains(localScope)) return;

      var captures = myClosureToCaptures.GetOrCreateValue(currentClosure, static key => ClosureCaptures.Create(key));
      captures.CapturedEntities.Add(capturedEntity);

      var displayClass = myScopeToDisplayClass.GetOrCreateValue(localScope, static key => DisplayClass.Create(key));
      displayClass.AddMember(capturedEntity);

      captures.CapturedDisplayClasses.Add(displayClass);
    }

    void NoteLocalFunctionUsage(ILocalFunctionDeclaration localFunctionDeclaration, ILocalScope localScopeNode)
    {
      var currentClosure = myCurrentClosures.Peek();
      if (ReferenceEquals(currentClosure, localFunctionDeclaration)) return;
      if (currentClosure.Contains(localScopeNode)) return;

      var captures = myClosureToCaptures.GetOrCreateValue(currentClosure, static key => ClosureCaptures.Create(key));

      captures.CapturedEntities.Add(localFunctionDeclaration.DeclaredElement);
    }

    [Pure]
    static ITreeNode? FindParameterScopeNode(IParameter parameter, IReferenceExpression referenceExpression)
    {
      // primary constructor can be declared in other type part
      if (parameter.ContainingParametersOwner is IPrimaryConstructor)
      {
        return referenceExpression.GetContainingNode<IClassLikeDeclaration>();
      }

      var parameterDeclaration = parameter.GetSingleDeclaration<ICSharpDeclaration>();
      if (parameterDeclaration != null)
      {
        // query range variables are "visible" as a parameters for multiple closure nodes
        if (parameterDeclaration is IQueryRangeVariableDeclaration rangeVariableDeclaration)
        {
          return FindParameterPlatformOfRangeVariableInContext(rangeVariableDeclaration, referenceExpression);
        }

        // indexer parameters are outside of accessor declaration nodes
        var indexerDeclaration = IndexerDeclarationNavigator.GetByParameterDeclaration(parameterDeclaration as ICSharpParameterDeclaration);
        if (indexerDeclaration != null)
        {
          foreach (var accessorDeclaration in indexerDeclaration.AccessorDeclarationsEnumerable)
          {
            if (accessorDeclaration.Contains(referenceExpression))
            {
              return FromFunctionDeclaration(accessorDeclaration);
            }
          }

          // int this[int index] => F(() => index);
          return indexerDeclaration.ArrowClause;
        }

        var parametersOwnerDeclaration = CSharpParametersOwnerDeclarationNavigator.GetByParameterDeclaration(parameterDeclaration as ICSharpParameterDeclaration);
        if (parametersOwnerDeclaration != null)
        {
          // constructors introduce additional scope to support constructor initializers
          if (parametersOwnerDeclaration is IConstructorDeclaration constructorDeclaration)
          {
            return constructorDeclaration;
          }

          // use body nodes for other members
          switch (parametersOwnerDeclaration)
          {
            case IExpressionBodyOwnerDeclaration { ArrowClause: { } arrowClause }:
              return arrowClause;
            case ICSharpFunctionDeclaration functionDeclaration:
              return functionDeclaration.Body;
            case ILocalFunctionDeclaration localFunctionDeclaration:
              return localFunctionDeclaration.Body;
            default:
              return null;
          }
        }

        // use block for block-bodied lambdas, otherwise lambda node itself is a scope
        var lambdaExpression = LambdaExpressionNavigator.GetByParameterDeclaration(parameterDeclaration as ILocalRegularParameterDeclaration);
        if (lambdaExpression != null)
        {
          return lambdaExpression.BodyBlock ?? (ITreeNode) lambdaExpression;
        }

        var anonymousMethodExpression = AnonymousMethodExpressionNavigator.GetByParameterDeclaration(parameterDeclaration as ILocalParameterDeclaration);
        if (anonymousMethodExpression != null)
        {
          return anonymousMethodExpression.Body;
        }
      }
      else
      {
        // implicit 'args' parameter
        if (parameter.ContainingParametersOwner is ITopLevelEntryPoint topLevelEntryPoint)
        {
          return topLevelEntryPoint.GetSingleDeclaration<ITopLevelCode>();
        }

        // implicit 'value' parameter in accessors
        if (parameter.IsValueVariable)
        {
          if (parameter.ContainingParametersOwner is IAccessor accessor)
          {
            var accessorDeclaration = accessor.GetSingleDeclaration<IAccessorDeclaration>();
            if (accessorDeclaration != null)
            {
              return FromFunctionDeclaration(accessorDeclaration);
            }
          }

          return null;
        }
      }

      return null;
    }

    [Pure]
    static ITreeNode? FromFunctionDeclaration(ICSharpFunctionDeclaration functionDeclaration)
    {
      var blockBody = functionDeclaration.Body;
      if (blockBody != null) return blockBody;

      return functionDeclaration.ArrowClause;
    }

    [Pure]
    static IQueryParameterPlatform? FindParameterPlatformOfRangeVariableInContext(IQueryRangeVariableDeclaration rangeVariableDeclaration, ITreeNode context)
    {
      // note: multiple query parameter platforms can have the same range variable as a parameter

      foreach (var containingPlatform in context.ContainingNodes<IQueryParameterPlatform>())
      foreach (var queryVariable in containingPlatform.GetVariables())
      {
        if (queryVariable.Declaration == rangeVariableDeclaration)
          return containingPlatform;
      }

      return null;
    }
  }

  private void ProcessThisReferenceCapture(ICSharpExpression thisOrBaseExpression)
  {
    if (myCurrentClosures.Count > 0)
    {
      ProcessThisReferenceCaptureInsideClosure(thisOrBaseExpression);
    }
  }

  private void ProcessThisReferenceCaptureInsideClosure(ICSharpExpression thisReferenceExpression)
  {
    Assertion.Assert(myCurrentClosures.Count > 0);
    Assertion.AssertNotNull(myThisReferenceCaptureScopeNode);

    if (thisReferenceExpression.GetContainingTypeDeclaration() is { DeclaredElement: { } thisTypeElement })
    {
      var currentClosure = myCurrentClosures.Peek();

      var displayClass = myScopeToDisplayClass.GetOrCreateValue(myThisReferenceCaptureScopeNode, static key => DisplayClass.Create(key));
      displayClass.AddMember(thisTypeElement);

      var captures = myClosureToCaptures.GetOrCreateValue(currentClosure, static key => ClosureCaptures.Create(key));
      captures.CapturedEntities.Add(thisTypeElement);
      captures.CapturedDisplayClasses.Add(displayClass);
    }
  }

  [Pure]
  private static ITreeNode? FindThisCaptureScope(ITreeNode declaration)
  {
    switch (declaration)
    {
      case ICSharpTypeMemberDeclaration { DeclaredElement.ContainingType: IClass } typeMemberDeclaration:
      {
        if (typeMemberDeclaration.IsStatic) return null;

        // constructors introduce additional scope to support constructor initializers
        if (typeMemberDeclaration is IConstructorDeclaration constructorDeclaration)
        {
          return constructorDeclaration;
        }

        // use body nodes for other members
        switch (typeMemberDeclaration)
        {
          case IExpressionBodyOwnerDeclaration { ArrowClause: { } arrowClause }:
            return arrowClause;
          case ICSharpFunctionDeclaration functionDeclaration:
            return functionDeclaration.Body;
          default:
            return null;
        }
      }

      case IAccessorDeclaration { DeclaredElement.ContainingType: IClass } accessorDeclaration:
      {
        if (accessorDeclaration.IsStatic) return null;

        return accessorDeclaration.Body ?? (ITreeNode) accessorDeclaration.ArrowClause;
      }

      default: return null;
    }
  }

  void IRecursiveElementProcessor.ProcessAfterInterior(ITreeNode element)
  {
    if (element is ICSharpClosure closure)
    {
      var poppedClosure = myCurrentClosures.Pop();
      Assertion.Assert(closure == poppedClosure);
    }
  }

  [Pure]
  private static bool IsDirectInvocation(IReferenceExpression referenceExpression)
  {
    var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(
      referenceExpression.GetContainingParenthesizedExpression());
    return invocationExpression != null;
  }

  private sealed class DisplayClass : IComparable<DisplayClass>, IDisplayClass
  {
    private DisplayClass() { }
    private static readonly ObjectPool<DisplayClass> Pool = new(static _ => new());

    [Pure]
    public static DisplayClass Create(ITreeNode scopeNode)
    {
      var displayClass = Pool.Allocate();
      displayClass.ScopeNode = scopeNode;
      displayClass.IsStruct = !HasContainingVarianceAnnotations(scopeNode);
      displayClass.IsReplacedWithInstanceMethods = false;

      return displayClass;
    }

    [Pure]
    private static bool HasContainingVarianceAnnotations(ITreeNode scopeNode)
    {
      var containingTypeDeclaration = scopeNode.GetContainingNode<IClassLikeDeclaration>(returnThis: true);

      for (var containingType = containingTypeDeclaration?.DeclaredElement; containingType != null; containingType = containingType.GetContainingType())
      {
        if (containingType is not IInterface interfaceType) break;

        foreach (var typeParameter in interfaceType.TypeParameters)
        {
          if (typeParameter.Variance != TypeParameterVariance.INVARIANT)
            return true;
        }
      }

      return false;
    }

    public void Free()
    {
      ScopeNode = null!;
      Members.Clear();
      Closures.Clear();
      ContainingDisplayClass = null;

      Pool.Return(this);
    }

    // note: can be IConstructorDeclaration
    // note: can be IBlock bodt of member declarations or oridinary IBlock
    // note: can be IArrowExpressionClause of expression-bodied members
    // note: can be ILambdaExpression if it's expression-bodied
    // note: can be IQueryParameterPlatform
    public ITreeNode ScopeNode { get; private set; } = null!;

    public HashSet<IDeclaredElement> Members { get; } = new();
    public List<ICSharpClosure> Closures { get; } = new();

    public DisplayClass? ContainingDisplayClass { get; set; }

    public bool IsStruct { get; set; }
    public bool IsReplacedWithInstanceMethods { get; set; }
    public bool IsAllocationOptimized => IsStruct || IsReplacedWithInstanceMethods;

    public void AddMember(IDeclaredElement localEntity)
    {
      Members.Add(localEntity);
    }

    public void SetContainingDisplayClassReference(DisplayClass other)
    {
      Assertion.Assert(other != this);

      if (ContainingDisplayClass == null || ContainingDisplayClass.CompareTo(other) > 0)
      {
        ContainingDisplayClass = other;
      }
    }

    public void AttachClosure(ICSharpClosure closure, bool canTakeRefDisplayClass)
    {
      Closures.Add(closure);
      IsStruct &= canTakeRefDisplayClass;
    }

    public int CompareTo(DisplayClass other)
    {
      var otherScope = other.ScopeNode;

      if (ScopeNode == otherScope)
        return 0;

      if (ScopeNode.Contains(otherScope))
        return +1;

      Assertion.Assert(otherScope.Contains(ScopeNode), "Scopes must be contained inside each other");
      return -1;
    }

    IDisplayClass? IDisplayClass.ContainingDisplayClass => ContainingDisplayClass;
  }

  private sealed class ClosureCaptures : IClosureCaptures
  {
    private ClosureCaptures() { }
    private static readonly ObjectPool<ClosureCaptures> Pool = new(static _ => new());

    [Pure]
    public static ClosureCaptures Create(ICSharpClosure closure)
    {
      var captures = Pool.Allocate();
      captures.Closure = closure;
      return captures;
    }

    public void Free()
    {
      Closure = null!;
      CapturedDisplayClasses.Clear();
      CapturedEntities.Clear();
      AttachedDisplayClass = null!;

      Pool.Return(this);
    }

    private ICSharpClosure Closure { get; set; } = null!;

    public HashSet<DisplayClass> CapturedDisplayClasses { get; } = new();

    public HashSet<IDeclaredElement> CapturedEntities { get; } = new();
    private DisplayClass? AttachedDisplayClass { get; set; }

    IDisplayClass IClosureCaptures.AttachedDisplayClass => AttachedDisplayClass.NotNull();

    public void AssignToDisplayClass(DisplayClass displayClass, bool canTakeRefDisplayClass)
    {
      AttachedDisplayClass = displayClass;
      displayClass.AttachClosure(Closure, canTakeRefDisplayClass);
    }
  }

  public IEnumerable<IDisplayClass> NotOptimizedDisplayClasses
  {
    get
    {
      foreach (var (_, displayClass) in myScopeToDisplayClass)
      {
        if (!displayClass.IsAllocationOptimized)
        {
          yield return displayClass;
        }
      }
    }
  }

  public IEnumerable<ICSharpClosure> AllClosures => myAllFoundClosures;

  [Pure]
  public IClosureCaptures? TryGetCapturesOf(ICSharpClosure closure)
  {
    myClosureToCaptures.TryGetValue(closure, out var x);
    return x;
  }

  #region Dump

  public string Dump()
  {
    var builder = new StringBuilder();
    builder.AppendLine($"Owner: {GetOwnerPresentation(myDeclaration)}");

    if (myAllFoundClosures.Count > 0)
    {
      builder.AppendLine("Closures:");
      foreach (var closure in myAllFoundClosures)
      {
        builder.AppendLine($"> {GetOwnerPresentation(closure)}");

        if (myClosureToCaptures.TryGetValue(closure, out var captures))
        {
          builder.AppendLine("    Captures:");
          foreach (var capturedEntity in captures.CapturedEntities)
          {
            builder.AppendLine($"    > {PresentCapture(capturedEntity)}");
          }
        }
      }
    }

    if (myScopeToDisplayClass.Count > 0)
    {
      var orderedDisplayClasses = myScopeToDisplayClass.OrderBy(x => x.Key.GetTreeStartOffset()).ToList();
      var displayClassIndexes = orderedDisplayClasses.WithIndexes().ToDictionary(x => x.Item.Value, x => x.Index + 1);

      builder.AppendLine("Display classes:");
      foreach (var (scopeNode, displayClass) in orderedDisplayClasses)
      {
        builder.AppendLine($"  Display class #{displayClassIndexes[displayClass]}");

        builder.AppendLine($"    Scope: {PresentScope(scopeNode)}");

        if (displayClass.IsReplacedWithInstanceMethods)
          builder.AppendLine("    OPTIMIZED: Closures lowered into instance members");
        else if (displayClass.IsStruct)
          builder.AppendLine("    OPTIMIZED: Lowered into struct type");

        builder.AppendLine("    Members:");
        foreach (var capturedEntity in displayClass.Members)
        {
          builder.AppendLine($"    > {PresentCapture(capturedEntity)}");
        }

        var containingDisplayClass = displayClass.ContainingDisplayClass;
        if (containingDisplayClass != null)
        {
          builder.AppendLine($"    > display class #{displayClassIndexes[containingDisplayClass]}");
        }

        if (displayClass.Closures.Count > 0)
        {
          builder.AppendLine("    Closures:");
          foreach (var closure in displayClass.Closures)
          {
            builder.AppendLine($"    > {GetOwnerPresentation(closure)}");
          }
        }
      }
    }

    return builder.ToString();
  }

  private static string GetOwnerPresentation(ITreeNode treeNode)
  {
    return treeNode switch
    {
      ILambdaExpression lambdaExpression
        => PresentLambdaExpression(lambdaExpression),
      IAnonymousMethodExpression anonymousMethodExpression
        => PresentAnonymousMethodExpression(anonymousMethodExpression),
      IQueryParameterPlatform queryParameterPlatform
        => PresentQueryParameterPlatform(queryParameterPlatform),
      ITopLevelCode
        => "top-level code",
      IDeclaration { DeclaredElement: { } declaredElement } declaration
        => DeclaredElementPresenter.Format(declaration.Language, OwnerPresenterStyle, declaredElement).Text,
      IExtendedType extendedType
        when ClassLikeDeclarationNavigator.GetByExtendsList(
          ExtendsListNavigator.GetByExtendedType(extendedType)) is { PrimaryConstructorDeclaration: { } primaryConstructorDeclaration }
        => "extended type of " + GetOwnerPresentation(primaryConstructorDeclaration),
      IExtendedType
        => "extended type",
      _
        => throw new ArgumentOutOfRangeException()
    };

    static string PresentLambdaExpression(ILambdaExpression lambdaExpression)
    {
      var anonymousMethod = (IAnonymousMethod)lambdaExpression.DeclaredElement.NotNull();
      var builder = new StringBuilder("lambda expression '");

      PresentAnonymousMethodSignature(builder, anonymousMethod, lambdaExpression.Language);

      builder.Append(" => ");

      var bodyBlock = lambdaExpression.BodyExpression ?? (ITreeNode) lambdaExpression.BodyBlock;
      if (bodyBlock != null)
      {
        builder.Append(bodyBlock.GetText().ReplaceNewLines().FullReplace("  ", " ").TrimToSingleLineWithMaxLength(30));
      }

      builder.Append('\'');

      return builder.ToString();
    }

    static string PresentAnonymousMethodExpression(IAnonymousMethodExpression anonymousMethodExpression)
    {
      var anonymousMethod = (IAnonymousMethod)anonymousMethodExpression.DeclaredElement.NotNull();
      var builder = new StringBuilder("anonymous method '");

      PresentAnonymousMethodSignature(builder, anonymousMethod, anonymousMethodExpression.Language);

      builder.Append(" => ");

      var bodyBlock = anonymousMethodExpression.Body;
      if (bodyBlock != null)
      {
        builder.Append(bodyBlock.GetText().ReplaceNewLines().FullReplace("  ", " ").TrimToSingleLineWithMaxLength(30));
      }

      builder.Append('\'');

      return builder.ToString();
    }

    static string PresentQueryParameterPlatform(IQueryParameterPlatform parameterPlatform)
    {
      var anonymousMethod = (IAnonymousMethod)parameterPlatform.DeclaredElement.NotNull();
      var builder = new StringBuilder("query lambda '");

      PresentAnonymousMethodSignature(builder, anonymousMethod, parameterPlatform.Language);

      builder.Append(" => ");

      var expression = parameterPlatform.Value;
      if (expression != null)
      {
        builder.Append(expression.GetText().ReplaceNewLines().FullReplace("  ", " ").TrimToSingleLineWithMaxLength(30));
      }

      builder.Append('\'');

      return builder.ToString();
    }

    static void PresentAnonymousMethodSignature(
      StringBuilder builder, IAnonymousMethod anonymousMethod, PsiLanguageType language)
    {
      builder.Append(CSharpDeclaredElementPresenter.ReturnKindText(anonymousMethod.ReturnKind, appendSpaceIfByRef: true));
      builder.Append(anonymousMethod.ReturnType.GetPresentableName(language, OwnerPresenterStyle.TypePresentationStyle));
      builder.Append(" (");

      foreach (var (anonymousMethodParameter, index) in anonymousMethod.Parameters.WithIndexes())
      {
        if (index > 0) builder.Append(", ");

        builder.Append(CSharpDeclaredElementPresenter.ParameterKindText(anonymousMethodParameter.Kind, appendSpaceIfByRef: true));
        builder.Append(anonymousMethodParameter.Type.GetPresentableName(language, OwnerPresenterStyle.TypePresentationStyle));
        builder.Append(' ');
        builder.Append(anonymousMethodParameter switch
        {
          ITransparentVariable => "transparent_variable",
          _ => anonymousMethodParameter.ShortName
        });
      }

      builder.Append(')');
    }
  }

  private static string PresentScope(ITreeNode treeNode)
  {
    var nodeText = treeNode.GetText().ReplaceNewLines().FullReplace("  ", " ").TrimToSingleLineWithMaxLength(43);

    var nodePresentation = treeNode.ToString();
    var interfaceName = Regex.Match(nodePresentation, "^I\\w+");
    if (interfaceName.Success)
    {
      return $"{interfaceName.Value} '{nodeText}'";
    }

    var typeName = treeNode.GetType().Name;
    return $"{typeName} '{nodeText}'";
  }

  private string PresentCapture(IDeclaredElement declaredElement)
  {
    if (declaredElement is ITypeElement)
    {
      return "'this' reference";
    }

    return DeclaredElementPresenter.Format(myDeclaration.Language, CapturePresenterStyle, declaredElement).Text;
  }

  private static readonly DeclaredElementPresenterStyle OwnerPresenterStyle = new()
  {
    ShowEntityKind = EntityKindForm.NORMAL,
    ShowNameInQuotes = true,
    ShowName = NameStyle.QUALIFIED,
    ShowType = TypeStyle.DEFAULT,
    TypePresentationStyle = new TypePresentationStyle
    {
      Options = TypePresentationOptions.UseKeywordsForPredefinedTypes
                | TypePresentationOptions.IncludeNullableAnnotations
                | TypePresentationOptions.UseTupleSyntax
    },
    ShowParameterNames = true,
    ShowParameterTypes = true,
    ShowTypeParameters = TypeParameterStyle.FULL
  };

  private static readonly DeclaredElementPresenterStyle CapturePresenterStyle = new()
  {
    ShowEntityKind = EntityKindForm.NORMAL,
    ShowNameInQuotes = true,
    ShowName = NameStyle.QUALIFIED,
    ShowType = TypeStyle.NONE
  };

  #endregion
}

public interface IDisplayClass
{
  ITreeNode ScopeNode { get; }
  HashSet<IDeclaredElement> Members { get; }
  IDisplayClass? ContainingDisplayClass { get; }
  bool IsAllocationOptimized { get; }
}

public interface IClosureCaptures
{
  HashSet<IDeclaredElement> CapturedEntities { get; }
  IDisplayClass AttachedDisplayClass { get; }
}
