using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.DeclaredElements;
using JetBrains.ReSharper.Psi.CSharp.Impl;
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

  public DisplayClassStructure([NotNull] ITreeNode declaration)
  {
    myDeclaration = declaration;
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

      foreach (var anonymousMethodParameter in anonymousMethod.Parameters)
      {
        builder.Append(CSharpDeclaredElementPresenter.ParameterKindText(anonymousMethodParameter.Kind, appendSpaceIfByRef: true));
        builder.Append(anonymousMethodParameter.Type.GetPresentableName(language, OwnerPresenterStyle.TypePresentationStyle));
        builder.Append(' ');
        builder.Append(anonymousMethodParameter.ShortName);
      }

      builder.Append(')');
    }
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

    if (myClosuresList.Count > 0)
    {
      builder.AppendLine("Closures:");

      foreach (var closure in myClosuresList)
      {
        builder.AppendLine($"> {GetOwnerPresentation(closure)}");
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

  private readonly List<ICSharpClosure> myClosuresList = new();

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
