#nullable enable
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
using JetBrains.Util.DataStructures.Collections;

namespace ReSharperPlugin.HeapView.Analyzers;

// todo: args can be captured - allocation on the first statement
// todo: prettify data structures use code, probably pool some things

public sealed class DisplayClassStructure : IRecursiveElementProcessor
{
  private readonly ITreeNode myDeclaration;
  private readonly ITreeNode? myThisReferenceCaptureScopeNode;

  private readonly Stack<ClosureInfo> myCurrentClosures = new();

  private readonly List<ICSharpClosure> myAllFoundClosures = new();
  private readonly Dictionary<ITreeNode, DisplayClass> myScopeToDisplayClass = new();
  private readonly Dictionary<ICSharpClosure, Captures> myClosureToCaptures = new();


  private HashSet<ILocalFunctionDeclaration>? myDelayedUseLocalFunctions;
  private OneToSetMap<ILocalFunctionDeclaration, ILocalFunctionDeclaration>? myDirectInvocationsBetweenLocalFunctions;

  private bool myIsScanningNonMainPart;
  private HashSet<string>? myCaptureNamesToLookInOtherParts;

  private DisplayClassStructure(ITreeNode declaration)
  {
    myDeclaration = declaration;
    myThisReferenceCaptureScopeNode = FindThisCaptureScope(declaration);
  }

  public static DisplayClassStructure? Build(ITreeNode declaration)
  {
    DisplayClassStructure? structure = null;

    switch (declaration)
    {
      // record R(int X) : B(...) { int Member = ...; }
      case IClassLikeDeclaration classLikeDeclaration:
      {
        structure = TryBuildFromInitializerScope(classLikeDeclaration);
        break;
      }

      // void Method() { }
      // int Property { get { } }
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

    structure?.PropagateLocalFunctionsDelayedUse();

    return structure;
  }

  private static DisplayClassStructure? TryBuildFromInitializerScope(IClassLikeDeclaration classLikeDeclaration)
  {
    DisplayClassStructure? structure = null;
    var seenBodies = false;

    // if type declaration is partial and has primary parameters in some part
    // we need to scan the other parts to find possible captures of such parameters
    if (classLikeDeclaration.IsPartial
        && classLikeDeclaration.DeclaredElement is IRecord { PrimaryConstructor.Parameters: { Count: > 0 } parameters } record)
    {
      var allDeclarations = record.GetDeclarations();
      if (allDeclarations.Count > 1)
      {
        var parameterNames = new HashSet<string>(StringComparer.Ordinal);
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

        return seenBodies ? structure : null;
      }
    }

    ScanInitializerScope(classLikeDeclaration, classLikeDeclaration, ref structure, ref seenBodies);
    return structure;

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

  // todo: is this is even needed? probably not
  // todo: local function invocation is the reference to it's display class
  // todo: probably do the same between display classes themselves
  private void PropagateLocalFunctionsDelayedUse()
  {
    // if we found direct invocations from local functions
    if (myDirectInvocationsBetweenLocalFunctions != null
        && myDelayedUseLocalFunctions != null)
    {
      while (PropagateLocalFunctionsDelayedUseOnce()) { }
    }

    bool PropagateLocalFunctionsDelayedUseOnce()
    {
      using var invokedFromDelayed = PooledHashSet<ILocalFunctionDeclaration>.GetInstance();

      foreach (var delayedUseLocalFunction in myDelayedUseLocalFunctions)
      foreach (var targetLocalFunction in myDirectInvocationsBetweenLocalFunctions[delayedUseLocalFunction])
      {
        if (!myDelayedUseLocalFunctions.Contains(targetLocalFunction))
        {
          invokedFromDelayed.Add(targetLocalFunction);
        }
      }

      foreach (var newDelayed in invokedFromDelayed)
      {
        myDelayedUseLocalFunctions.Add(newDelayed);
      }

      return invokedFromDelayed.Count > 0;
    }
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
        myCurrentClosures.Push(new ClosureInfo(closure));

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
    var resolveResult = referenceExpression.Reference.Resolve();

    switch (resolveResult.DeclaredElement)
    {
      case ILocalFunction localFunction:
      {
        var targetDeclaration = localFunction.GetSingleDeclaration<ILocalFunctionDeclaration>();

        if (!IsDirectInvocation(referenceExpression))
        {
          myDelayedUseLocalFunctions ??= new HashSet<ILocalFunctionDeclaration>();
          myDelayedUseLocalFunctions.Add(targetDeclaration);
        }
        else
        {
          // direct invocation

          if (myCurrentClosures.Count > 0)
          {
            var currentClosure = myCurrentClosures.Peek();
            if (currentClosure.HasDelayedBody)
            {
              myDelayedUseLocalFunctions ??= new HashSet<ILocalFunctionDeclaration>();
              myDelayedUseLocalFunctions.Add(targetDeclaration);
            }
            else if (currentClosure.Closure is ILocalFunctionDeclaration fromDeclaration)
            {
              myDirectInvocationsBetweenLocalFunctions ??= new OneToSetMap<ILocalFunctionDeclaration, ILocalFunctionDeclaration>();
              myDirectInvocationsBetweenLocalFunctions.Add(fromDeclaration, targetDeclaration);
            }
          }
        }

        break;
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

          var displayClass = myScopeToDisplayClass.GetOrCreateValue(myDeclaration, static key => new DisplayClass(key));
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

      case ITypeMember { IsStatic: false }:
      {
        ProcessThisReferenceCaptureInsideClosure(referenceExpression);
        break;
      }
    }

    void NoteCapture(ITreeNode localScope, IDeclaredElement capturedEntity)
    {
      var currentClosure = myCurrentClosures.Peek().Closure;
      if (currentClosure != null && !currentClosure.Contains(localScope))
      {
        var displayClass = myScopeToDisplayClass.GetOrCreateValue(localScope, static key => new DisplayClass(key));
        displayClass.AddMember(capturedEntity);

        var captures = myClosureToCaptures.GetOrCreateValue(currentClosure, static () => new Captures());
        captures.CapturedEntities.Add(capturedEntity);
        captures.CapturedDisplayClasses.Add(displayClass);
      }
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
        var lambdaExpression = LambdaExpressionNavigator.GetByParameterDeclaration(parameterDeclaration as ILambdaParameterDeclaration);
        if (lambdaExpression != null)
        {
          return lambdaExpression.BodyBlock ?? (ITreeNode) lambdaExpression;
        }

        var anonymousMethodExpression = AnonymousMethodExpressionNavigator.GetByParameterDeclaration(parameterDeclaration as IAnonymousMethodParameterDeclaration);
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
      var currentClosure = myCurrentClosures.Peek().Closure;
      if (currentClosure != null)
      {
        var displayClass = myScopeToDisplayClass.GetOrCreateValue(myThisReferenceCaptureScopeNode, static key => new DisplayClass(key));
        displayClass.AddMember(thisTypeElement);

        var captures = myClosureToCaptures.GetOrCreateValue(currentClosure, static () => new Captures());
        captures.CapturedEntities.Add(thisTypeElement);
        captures.CapturedDisplayClasses.Add(displayClass);
      }
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
      Assertion.Assert(closure == poppedClosure.Closure);

      if (!myIsScanningNonMainPart)
      {
        ProcessClosureAfterInterior(closure);
      }
    }
  }

  private void ProcessClosureAfterInterior(ICSharpClosure closure)
  {
    if (myClosureToCaptures.TryGetValue(closure, out var captures))
    {
      Assertion.Assert(captures.CapturedEntities.Count > 0);
      Assertion.Assert(captures.CapturedDisplayClasses.Count > 0);

      if (captures.CapturedDisplayClasses.Count == 1)
      {
        foreach (var single in captures.CapturedDisplayClasses)
        {
          single.AttachClosure(closure);
          captures.DisplayClass = single;
          break;
        }
      }
      else
      {
        using var displayClasses = PooledList<DisplayClass>.GetInstance();

        displayClasses.AddRange(captures.CapturedDisplayClasses);
        displayClasses.Sort();

        displayClasses[0].AttachClosure(closure);
        captures.DisplayClass = displayClasses[0];

        // join display classes together
        for (var index = 1; index < displayClasses.Count; index++)
        {
          var inner = displayClasses[index - 1];
          var containing = displayClasses[index];

          inner.AddContainingDisplayClassReference(containing);
        }
      }
    }
    else
    {
      // captureless closure
    }
  }

  [Pure]
  private static bool IsDirectInvocation(IReferenceExpression referenceExpression)
  {
    var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(
      referenceExpression.GetContainingParenthesizedExpression());
    return invocationExpression != null;
  }

  private readonly struct ClosureInfo
  {
    public ClosureInfo(ICSharpClosure closure)
    {
      Closure = closure;
      HasDelayedBody = closure is not ILocalFunctionDeclaration { IsAsync: false, IsIterator: false };
    }

    public readonly ICSharpClosure? Closure;
    public readonly bool HasDelayedBody;
  }

  // todo: ability to find the first capture range
  // note: if there are any members w/o declarations - use special logic (first top level token for 'args', accessor name for 'value' parameter)
  // note: for indexers w/ explicit accessors highlight corresponding accessor name when parameter is captured
  // note: for expression-bodied indexer highlight the parameter itself?

  // todo: display class containing only 'this' reference optimized away into instance members
  private sealed class DisplayClass : IComparable<DisplayClass>
  {
    public DisplayClass(ITreeNode scopeNode)
    {
      ScopeNode = scopeNode;
    }

    // note: can be IConstructorDeclaration
    // note: can be IBlock bodt of member declarations or oridinary IBlock
    // note: can be IArrowExpressionClause of expression-bodied members
    // note: can be ILambdaExpression if it's expression-bodied
    // note: can be IQueryParameterPlatform
    private ITreeNode ScopeNode { get; }

    public HashSet<IDeclaredElement> Members { get; } = new();
    public List<ICSharpClosure> Closures { get; } = new();

    public DisplayClass? ContainingDisplayClass { get; private set; }

    public void AddMember(IDeclaredElement localEntity)
    {
      Members.Add(localEntity);
    }

    public void AddContainingDisplayClassReference(DisplayClass other)
    {
      Assertion.Assert(other != this);

      if (ContainingDisplayClass == null || ContainingDisplayClass.CompareTo(other) > 0)
      {
        ContainingDisplayClass = other;
      }
    }

    public bool IsOptimizedIntoInstanceMethod()
    {
      // todo: what if has local functions attached?

      return ContainingDisplayClass == null
             && Members.SingleItem() is ITypeElement;
    }

    public void AttachClosure(ICSharpClosure closure)
    {
      Closures.Add(closure);
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
  }

  private class Captures
  {
    public HashSet<DisplayClass> CapturedDisplayClasses { get; } = new();
    public HashSet<IDeclaredElement> CapturedEntities { get; } = new();
    public DisplayClass? DisplayClass { get; set; }
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

        if (displayClass.IsOptimizedIntoInstanceMethod())
        {
          builder.AppendLine("    OPTIMIZED: Lowered to instance members");
        }

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

    if (myDelayedUseLocalFunctions != null)
    {
      builder.AppendLine("Delay use local functions:");

      foreach (var declaration in myDelayedUseLocalFunctions.OrderBy(x => x.GetTreeStartOffset()))
      {
        builder.AppendLine($"> {GetOwnerPresentation(declaration)}");
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