using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.DeclaredElements;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
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

  public DisplayClassStructure([NotNull] ITreeNode declaration)
  {
    myDeclaration = declaration;
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

  [NotNull] private readonly List<ICSharpClosure> myClosuresList = new();
  [NotNull] private readonly Stack<ClosureInfo> myClosuresStack = new();
  [NotNull] private readonly Dictionary<ITreeNode, DisplayClass> myScopeToDisplayClass = new();
  [NotNull] private readonly Dictionary<ICSharpClosure, Captures> myClosureToCaptures = new();
  [CanBeNull] private HashSet<ILocalFunctionDeclaration> myDelayedUseLocalFunctions;
  [CanBeNull] private OneToSetMap<ILocalFunctionDeclaration, ILocalFunctionDeclaration> myDirectInvocationsBetweenLocalFunctions;

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
    if (element is ICSharpClosure closure)
    {
      myClosuresStack.Push(new ClosureInfo(closure));
      myClosuresList.Add(closure);
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

            if (myClosuresStack.Count > 0)
            {
              var currentClosure = myClosuresStack.Peek();
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

        case ILocalVariable { IsConstant: false } localVariable:
        {
          if (myClosuresStack.Count > 0)
          {
            var variableDeclaration = localVariable.GetSingleDeclaration<ICSharpDeclaration>();
            if (variableDeclaration != null)
            {
              var localScope = variableDeclaration.GetContainingScope<ILocalScope>();
              if (localScope != null)
              {
                if (IsCapturedFromOutside(localScope))
                {
                  var displayClass = myScopeToDisplayClass.GetOrCreateValue(localScope, static key => new DisplayClass(key));
                  displayClass.AddMember(localVariable);

                  var captures = myClosureToCaptures.GetOrCreateValue(myClosuresStack.Peek().Closure, static () => new Captures());
                  captures.CapturedEntities.Add(localVariable);
                  captures.CapturedDisplayClasses.Add(displayClass);
                }
              }
            }
          }

          break;
        }

        case IParameter parameter:
        {
          if (myClosuresStack.Count > 0)
          {
            var parameterDeclaration = parameter.GetSingleDeclaration<ICSharpDeclaration>();
            if (parameterDeclaration != null)
            {
              var localScope = parameterDeclaration.GetContainingScope<ILocalScope>();
              if (localScope != null)
              {
                if (IsCapturedFromOutside(localScope))
                {

                }
              }
            }
            else
            {
              // args parameter
              // value parameter in setters/adders/removers

              // get scope?
            }
          }

          break;
        }
      }
    }
  }

  [Pure]
  private bool IsCapturedFromOutside(ITreeNode scope)
  {
    Assertion.Assert(myClosuresStack.Count > 0);

    var currentClosure = myClosuresStack.Peek().Closure;
    return currentClosure != null && !currentClosure.Contains(scope);
  }

  [Pure]
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
      var poppedClosure = myClosuresStack.Pop();
      Assertion.Assert(closure == poppedClosure.Closure);

      if (myClosureToCaptures.TryGetValue(closure, out var captures))
      {
        Assertion.Assert(captures.CapturedEntities.Count > 0);
        Assertion.Assert(captures.CapturedDisplayClasses.Count > 0);

        if (captures.CapturedDisplayClasses.Count > 1)
        {
          using var displayClasses = PooledList<DisplayClass>.GetInstance();
          displayClasses.AddRange(captures.CapturedDisplayClasses);
          displayClasses.Sort();

          displayClasses[0].AttachClosure(closure);

          // join them togeter

        }
        else
        {
          foreach (var single in captures.CapturedDisplayClasses)
          {
            single.AttachClosure(closure);
            break;
          }
        }

        //
      }
      else
      {
        // captureless closure
      }
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

  private class DisplayClass : IComparable<DisplayClass>
  {
    [NotNull] private readonly ITreeNode myScopeNode;

    public DisplayClass([NotNull] ITreeNode scopeNode)
    {
      myScopeNode = scopeNode;
    }

    [NotNull] public HashSet<IDeclaredElement> Members { get; } = new();
    [NotNull] public List<ICSharpClosure> Closures { get; } = new();

    public void AddMember([NotNull] IDeclaredElement localEntity)
    {
      Members.Add(localEntity);
    }

    public void AttachClosure([NotNull] ICSharpClosure closure)
    {
      Closures.Add(closure);
    }

    public int CompareTo(DisplayClass other)
    {
      var otherScope = other.myScopeNode;

      if (myScopeNode == otherScope)
        return 0;

      if (myScopeNode.Contains(otherScope))
        return +1;

      Assertion.Assert(otherScope.Contains(myScopeNode), "Scopes must be contained inside each other");
      return -1;
    }
  }

  private class Captures
  {
    [NotNull] public HashSet<DisplayClass> CapturedDisplayClasses { get; } = new();
    [NotNull] public HashSet<IDeclaredElement> CapturedEntities { get; } = new();

    [CanBeNull] public DisplayClass ContainingDisplayClass { get; private set; }

    public void AddDisplayClassCapture()
    {

    }
  }

  #region Dump

  [NotNull]
  public string Dump()
  {
    var builder = new StringBuilder();
    builder.AppendLine($"Owner: {GetOwnerPresentation(myDeclaration)}");

    if (myClosuresList.Count > 0)
    {
      builder.AppendLine("Closures:");
      foreach (var closure in myClosuresList)
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

        builder.AppendLine("    Members:");
        foreach (var capturedEntity in displayClass.Members)
        {
          builder.AppendLine($"    > {PresentCapture(capturedEntity)}");
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

  [NotNull]
  private static string GetOwnerPresentation([NotNull] ITreeNode treeNode)
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

    [NotNull]
    static string PresentLambdaExpression([NotNull] ILambdaExpression lambdaExpression)
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

    [NotNull]
    static string PresentAnonymousMethodExpression([NotNull] IAnonymousMethodExpression anonymousMethodExpression)
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

    [NotNull]
    static string PresentQueryParameterPlatform([NotNull] IQueryParameterPlatform parameterPlatform)
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
      [NotNull] StringBuilder builder, [NotNull] IAnonymousMethod anonymousMethod, [NotNull] PsiLanguageType language)
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
        builder.Append(anonymousMethodParameter.ShortName);
      }

      builder.Append(')');
    }
  }

  [NotNull]
  private string PresentScope([NotNull] ITreeNode treeNode)
  {
    var nodeText = treeNode.GetText().ReplaceNewLines().FullReplace("  ", " ").TrimToSingleLineWithMaxLength(43);
    return $"{treeNode.GetType().Name} '{nodeText}'";
  }

  [NotNull]
  private string PresentCapture([NotNull] IDeclaredElement declaredElement)
  {
    return DeclaredElementPresenter.Format(myDeclaration.Language, CapturePresenterStyle, declaredElement).Text;
  }

  #endregion
}


