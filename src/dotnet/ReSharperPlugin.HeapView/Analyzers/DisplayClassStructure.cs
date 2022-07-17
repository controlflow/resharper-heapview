using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.DeclaredElements;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;

namespace ReSharperPlugin.HeapView.Analyzers;

public sealed class DisplayClassStructure : IRecursiveElementProcessor
{
  [NotNull] private readonly ITreeNode myDeclaration;

  private static readonly DeclaredElementPresenterStyle OwnerPresenterStyle = new()
  {
    ShowEntityKind = EntityKindForm.NORMAL,
    ShowNameInQuotes = true,
    ShowName = NameStyle.QUALIFIED,
    ShowType = TypeStyle.DEFAULT,
    TypePresentationStyle = new TypePresentationStyle { Options = TypePresentationOptions.None },
    ShowParameterNames = true,
    ShowParameterTypes = true,
    ShowTypeParameters = TypeParameterStyle.FULL
  };

  public DisplayClassStructure([NotNull] ITreeNode declaration)
  {
    myDeclaration = declaration;
  }

  [NotNull]
  private static string GetOwnerPresentation([NotNull] ITreeNode treeNode)
  {
    return treeNode switch
    {
      ITopLevelCode => "top-level code",
      IDeclaration { DeclaredElement: { } declaredElement } declaration =>
        DeclaredElementPresenter.Format(declaration.Language, OwnerPresenterStyle, declaredElement).Text,
      IExtendedType extendedType
        when ClassLikeDeclarationNavigator.GetByExtendsList(
          ExtendsListNavigator.GetByExtendedType(extendedType)) is { PrimaryConstructorDeclaration: { } primaryConstructorDeclaration }
        => "extended type of " + GetOwnerPresentation(primaryConstructorDeclaration),
      IExtendedType => "extended type",
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  [CanBeNull]
  public static DisplayClassStructure Build(ITreeNode declaration)
  {
    var bodyToAnalyze = TryGetCodeBodyToAnalyze(declaration);
    if (bodyToAnalyze == null) return null;

    var structure = new DisplayClassStructure(declaration);

    bodyToAnalyze.ProcessThisAndDescendants(structure);
    structure.PropagateLocalFunctionsDelayedUse();

    return structure;
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

  [CanBeNull, Pure]
  private static ITreeNode TryGetCodeBodyToAnalyze(ITreeNode declaration)
  {
    return declaration switch
    {
      // void Method() { }
      // int Property { get { } }
      ICSharpFunctionDeclaration { Body: { } bodyBlock } => bodyBlock,
      // int ExpressionBodiedProperty => expr;
      IExpressionBodyOwnerDeclaration { ArrowClause: { } arrowClause } => arrowClause,
      // int FieldInitializer = expr;
      IFieldDeclaration { Initial: { } initializer } => initializer,
      // int AutoPropertyInitializer { get; } = expr;
      IPropertyDeclaration { Initial: { } initializer } => initializer,
      // event EventHandler FieldLikeEvent = expr;
      IEventDeclaration { Initial: { } initializer } => initializer,
      // TopLevelCode();
      ITopLevelCode topLevelCode => topLevelCode,
      // record R() : B(expr);
      IExtendedType { ArgumentList: { } argumentList } => argumentList,
      _ => null
    };
  }

  [NotNull]
  public string Dump()
  {
    var builder = new StringBuilder();
    builder.AppendLine($"Owner: {GetOwnerPresentation(myDeclaration)}");

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

  [NotNull] private readonly Stack<ClosureInfo> myClosures = new();
  [CanBeNull] private HashSet<ILocalFunctionDeclaration> myDelayedUseLocalFunctions;
  [CanBeNull] private OneToSetMap<ILocalFunctionDeclaration, ILocalFunctionDeclaration> myDirectInvocationsBetweenLocalFunctions;

  bool IRecursiveElementProcessor.InteriorShouldBeProcessed(ITreeNode element)
  {
    switch (element)
    {
      case ITypeUsage:
      case IInvocationExpression invocationExpression when invocationExpression.IsNameofOperator():
        return false;

      default:
        return true;
    }
  }

  bool IRecursiveElementProcessor.ProcessingIsFinished => false;

  void IRecursiveElementProcessor.ProcessBeforeInterior(ITreeNode element)
  {
    if (element is ICSharpClosure closure)
    {
      myClosures.Push(new ClosureInfo(closure));
    }

    if (element is IReferenceExpression referenceExpression)
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

            if (myClosures.Count > 0)
            {
              var currentClosure = myClosures.Peek();
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

        case ILocalVariable or IParameter:
        {
          // todo: get or create a scope for a variable
          // todo: args parameter, value parameter

          break;
        }
      }
    }
  }

  private static bool IsDirectInvocation([NotNull] IReferenceExpression referenceExpression)
  {
    var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(
      referenceExpression.GetContainingParenthesizedExpression());
    return invocationExpression != null;
  }

  void IRecursiveElementProcessor.ProcessAfterInterior(ITreeNode element)
  {
    if (element is ICSharpClosure closure)
    {
      var poppedClosure = myClosures.Pop();
      Assertion.Assert(closure == poppedClosure.Closure);
    }

    // todo: handle exit from scope?
  }

  private readonly struct ClosureInfo
  {
    public ClosureInfo([NotNull] ICSharpClosure closure)
    {
      Closure = closure;
      HasDelayedBody = closure is not ILocalFunctionDeclaration { IsAsync: false, IsIterator: false };
    }

    [CanBeNull] public readonly ICSharpClosure Closure;
    public readonly bool HasDelayedBody;
  }
}
