using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.DeclaredElements;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using JetBrains.Util.DataStructures.Collections;
using ReSharperPlugin.HeapView.Highlightings;
using ReSharperPlugin.HeapView.Settings;

namespace ReSharperPlugin.HeapView.Analyzers;

[ElementProblemAnalyzer(
  ElementTypes: new[]
  {
    // initializer scope
    typeof(IClassLikeDeclaration),
    // methods, properties, indexers
    typeof(ICSharpFunctionDeclaration),
    typeof(IExpressionBodyOwnerDeclaration),
    // top-level code
    typeof(ITopLevelCode)
  },
  HighlightingTypes = new[]
  {
    typeof(ClosureAllocationHighlighting),
    typeof(DelegateAllocationHighlighting),
    typeof(ObjectAllocationHighlighting),
    typeof(ImplicitCaptureWarning),
    typeof(CanEliminateClosureCreationHighlighting)
  })]
public class AllocationOfClosuresAnalyzer : HeapAllocationAnalyzerBase<ITreeNode>
{
  protected override void Run(ITreeNode treeNode, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    using var structure = DisplayClassStructure.Build(treeNode);
    if (structure == null) return;

    foreach (var displayClass in structure.NotOptimizedDisplayClasses)
    {
      ReportDisplayClassAllocation(displayClass, consumer);
    }

    foreach (var closure in structure.AllClosures)
    {
      ReportClosureInvocation(closure, structure, data, consumer);
    }
  }

  private static readonly ObjectPool<PooledList<IDeclaredElement>> SortedMembersPool = PooledList<IDeclaredElement>.CreatePool();
  private static readonly ObjectPool<PooledHashSet<IDeclaredElement>> CapturesPool = PooledHashSet<IDeclaredElement>.CreatePool();

  private static void ReportDisplayClassAllocation(IDisplayClass displayClass, IHighlightingConsumer consumer)
  {
    if (displayClass.Members.Count == 0)
      return; // note: should not be possible

    using var sortedMembers = SortedMembersPool.Allocate();

    sortedMembers.AddRange(displayClass.Members);
    sortedMembers.Sort(DisplayClassMembersDeclarationOrderComparer.Instance);

    var firstDisplayClassMemberDeclaration = TryGetMemberDeclaration(sortedMembers[0]);
    var declarationNode = firstDisplayClassMemberDeclaration != null
      ? GetAllocationNodeFromMemberDeclaration(firstDisplayClassMemberDeclaration)
      : GetAllocationLocationNodeFromScope();

    if (declarationNode == null || declarationNode.IsInTheContextWhereAllocationsAreNotImportant())
      return;

    var description = CreateDisplayClassDescription();
    consumer.AddHighlighting(new ClosureAllocationHighlighting(declarationNode, description));

    [Pure]
    ITreeNode? GetAllocationNodeFromMemberDeclaration(ICSharpDeclaration displayClassMemberDeclaration)
    {
      switch (displayClassMemberDeclaration)
      {
        // int foo, bar;
        //     ^^^
        case ILocalVariableDeclaration localVariableDeclaration:
        {
          return localVariableDeclaration.NameIdentifier;
        }

        // void M(int foo)
        //            ^^^
        case ICSharpParameterDeclaration parameterDeclaration:
        {
          // if indexer contains explicit accessors - use accessor name instead
          // (usually to avoid ambiguity between closures in getter and setter)
          if (IndexerDeclarationNavigator.GetByParameterDeclaration(parameterDeclaration) is { } indexerDeclaration)
          {
            foreach (var accessorDeclaration in indexerDeclaration.AccessorDeclarationsEnumerable)
            {
              if (accessorDeclaration.Contains(displayClass.ScopeNode))
                return accessorDeclaration.NameIdentifier;
            }
          }

          return parameterDeclaration.NameIdentifier;
        }
      }

      return displayClassMemberDeclaration;
    }

    [Pure]
    ITreeNode? GetAllocationLocationNodeFromScope()
    {
      var scopeNode = displayClass.ScopeNode;
      var accessorDeclaration = AccessorDeclarationNavigator.GetByBody(scopeNode as IBlock)
                                ?? AccessorDeclarationNavigator.GetByArrowClause(scopeNode as IArrowExpressionClause);
      if (accessorDeclaration != null)
      {
        return accessorDeclaration.NameIdentifier;
      }

      var firstMeaningfulChild = scopeNode.GetNextMeaningfulChild(child: null);
      if (firstMeaningfulChild != null)
      {
        var node = firstMeaningfulChild as ITokenNode ?? firstMeaningfulChild.GetFirstTokenIn();
        return node;
      }

      return null;
    }

    string CreateDisplayClassDescription()
    {
      using var pooledBuilder = PooledStringBuilder.GetInstance();
      var builder = pooledBuilder.Builder;
      builder.Append("capture of");

      var containingClosure = displayClass.ContainingDisplayClass;
      var lastMemberIndex = sortedMembers.Count - (containingClosure != null ? 0 : 1);

      // capture of 'a' parameter
      // capture of 'a' parameter and 'b' variable
      // capture of 'a' parameter, 'b' variable and 'this' reference
      if (lastMemberIndex < 4)
      {
        builder.Append(' ');

        for (var index = 0; index < sortedMembers.Count; index++)
        {
          if (index > 0)
            builder.Append(index == lastMemberIndex ? " and " : ", ");

          AppendMember(sortedMembers[index]);
        }

        if (containingClosure != null)
        {
          builder.Append(" and containing closure (");
          PresentContainingClosureReference(containingClosure, builder);
          builder.Append(")");
        }
      }
      else
      {
        foreach (var t in sortedMembers)
        {
          builder.Append(Environment.NewLine).Append("    ");

          AppendMember(t);
        }

        if (containingClosure != null)
        {
          builder.Append(Environment.NewLine).Append("    ");

          builder.Append("containing closure (");
          PresentContainingClosureReference(containingClosure, builder);
          builder.Append(")");
        }
      }

      return builder.ToString();

      void AppendMember(IDeclaredElement declaredElement)
      {
        if (declaredElement is ITypeElement)
        {
          builder.Append("'this' reference");
        }
        else
        {
          builder.Append('\'').Append(declaredElement.ShortName).Append("\' ");
          builder.Append(declaredElement is IParameter ? "parameter" : "variable");
        }
      }
    }

    static void PresentContainingClosureReference(IDisplayClass? displayClass, StringBuilder builder)
    {
      var first = true;

      for (; displayClass != null; displayClass = displayClass.ContainingDisplayClass)
      {
        if (displayClass.IsAllocationOptimized)
          continue;

        using var sortedMembers = SortedMembersPool.Allocate();

        sortedMembers.AddRange(displayClass.Members);
        sortedMembers.Sort(DisplayClassMembersDeclarationOrderComparer.Instance);

        foreach (var member in sortedMembers)
        {
          if (first)
          {
            first = false;
          }
          else
          {
            builder.Append(", ");
          }

          builder.Append('\'');
          builder.Append(member is ITypeElement ? "this" : member.ShortName);
          builder.Append('\'');
        }
      }
    }
  }

  private static ICSharpDeclaration? TryGetMemberDeclaration(IDeclaredElement displayClassMember)
  {
    if (displayClassMember is ITypeElement) return null;

    return displayClassMember.GetSingleDeclaration<ICSharpDeclaration>();
  }

  private sealed class DisplayClassMembersDeclarationOrderComparer : IComparer<IDeclaredElement>
  {
    private DisplayClassMembersDeclarationOrderComparer() { }

    public static IComparer<IDeclaredElement> Instance { get; } = new DisplayClassMembersDeclarationOrderComparer();

    public int Compare(IDeclaredElement x, IDeclaredElement y)
    {
      if (ReferenceEquals(x, y)) return 0;
      if (ReferenceEquals(null, y)) return 1;
      if (ReferenceEquals(null, x)) return -1;

      var offsetComparison = ToOffset(x).CompareTo(ToOffset(y));
      if (offsetComparison != 0) return offsetComparison;

      return string.Compare(x.ShortName, y.ShortName, StringComparison.Ordinal);

      int ToOffset(IDeclaredElement displayClassMember)
      {
        if (displayClassMember is ITypeElement) return int.MaxValue;

        var memberDeclaration = TryGetMemberDeclaration(displayClassMember);
        if (memberDeclaration == null) return int.MaxValue - 1;

        return memberDeclaration.GetNameRange().StartOffset.Offset;
      }
    }
  }

  private static void ReportClosureInvocation(
    ICSharpClosure closure, DisplayClassStructure structure, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    if (closure.IsInTheContextWhereAllocationsAreNotImportant())
      return;

    if (IsExpressionTreeClosure())
    {
      consumer.AddHighlighting(
        new ObjectAllocationHighlighting(closure, "expression tree construction"),
        GetClosureAllocationRange(closure));

      return;
    }

    var captures = structure.TryGetCapturesOf(closure);
    if (captures == null) return;

    var createdType = TryGetCreatedLambdaType();
    if (createdType != null && createdType.IsDelegateType())
    {
      ReportDelegateAllocation(closure, createdType, captures, consumer, data);
    }

    IType? TryGetCreatedLambdaType()
    {
      switch (closure)
      {
        case IAnonymousFunctionExpression anonymousFunctionExpression:
        {
          var targetType = anonymousFunctionExpression.GetImplicitlyConvertedTo();
          if (!targetType.IsDelegateType())
          {
            return anonymousFunctionExpression.GetExpressionType().ToIType();
          }

          return targetType;
        }

        case IQueryParameterPlatform parameterPlatform:
        {
          return parameterPlatform.GetImplicitlyConvertedTo();
        }

        default:
          return null;
      }
    }

    [Pure]
    bool IsExpressionTreeClosure()
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
  }

  private static void ReportDelegateAllocation(
    ICSharpClosure closure, IType delegateType, IClosureCaptures captures, IHighlightingConsumer consumer, ElementProblemAnalyzerData data)
  {
    using var builder = PooledStringBuilder.GetInstance();
    builder.Append("new '");
    builder.Append(delegateType.GetPresentableName(closure.Language, CommonUtils.DefaultTypePresentationStyle));
    builder.Append("' instance creation");

    builder.AppendLine();
    builder.Append("Capture of ");
    AppendCapturesDescription(builder.Builder, captures.CapturedEntities);

    using var implicitCaptures = CapturesPool.Allocate();
    CollectImplicitCapturesThatCanContainReferences(implicitCaptures, captures, data);

    var closureRange = GetClosureAllocationRange(closure);

    if (implicitCaptures.Count > 0)
    {
      builder.AppendLine();
      builder.Append("Implicit capture of ");
      AppendCapturesDescription(builder.Builder, implicitCaptures);
      builder.Append(" (can cause memory leaks)");

      var warningSeverity = data.GetImplicitCaptureWarningSeverity();
      if (warningSeverity != Severity.DO_NOT_SHOW)
      {
        consumer.AddHighlightingWithOverrides(
          new ImplicitCaptureWarning(closure, builder.ToString()), closureRange,
          overriddenOverlapResolve: OverlapResolveKind.WARNING);
        TryReportClosurelessOverloads(closure, captures, consumer);
        return;
      }
    }

    consumer.AddHighlighting(
      new DelegateAllocationHighlighting(closure, builder.ToString()), closureRange);
    TryReportClosurelessOverloads(closure, captures, consumer);

    static void CollectImplicitCapturesThatCanContainReferences(
      HashSet<IDeclaredElement> consumer, IClosureCaptures captures, ElementProblemAnalyzerData data)
    {
      for (var displayClass = captures.AttachedDisplayClass; displayClass != null; displayClass = displayClass.ContainingDisplayClass)
      {
        consumer.AddRange(displayClass.Members);
      }

      consumer.ExceptWith(captures.CapturedEntities);

      if (consumer.Count > 0)
      {
        using var toRemove = CapturesPool.Allocate();

        foreach (var declaredElement in consumer)
        {
          if (declaredElement is ITypeOwner typeOwner && !data.CanContainManagedReferences(typeOwner.Type))
          {
            toRemove.Add(typeOwner);
          }
        }

        consumer.ExceptWith(toRemove);
      }
    }

    static void AppendCapturesDescription(StringBuilder builder, HashSet<IDeclaredElement> members)
    {
      using var sortedMembers = SortedMembersPool.Allocate();

      sortedMembers.AddRange(members);
      sortedMembers.RemoveAll(x => x is ILocalFunction);
      sortedMembers.Sort(DisplayClassMembersDeclarationOrderComparer.Instance);

      var hasThisReference = false;
      int parametersCount = 0, variablesCount = 0;

      foreach (var declaredElement in sortedMembers)
      {
        if (declaredElement is ITypeElement)
          hasThisReference = true;
        else if (declaredElement is IParameter)
          parametersCount++;
        else
          variablesCount++;
      }

      var kindsCount = (hasThisReference ? 1 : 0) + (parametersCount > 0 ? 1 : 0) + (variablesCount > 0 ? 1 : 0);
      var kindsBefore = 0;

      if (parametersCount > 0)
      {
        AppendMembersIfKind<IParameter>("parameter", parametersCount);
        kindsBefore++;
      }

      if (variablesCount > 0)
      {
        if (kindsBefore > 0)
          builder.Append(kindsBefore == kindsCount - 1 ? " and " : ", ");

        AppendMembersIfKind<ILocalVariable>("variable", variablesCount);
        kindsBefore++;
      }

      if (hasThisReference)
      {
        if (kindsBefore > 0)
          builder.Append(kindsBefore == kindsCount - 1 ? " and " : ", ");

        builder.Append("'this' reference");
      }

      void AppendMembersIfKind<TElement>(string kindName, int count) where TElement : IDeclaredElement
      {
        Assertion.Assert(count > 0);

        builder.Append(kindName);
        if (count > 1) builder.Append("s");
        builder.Append(' ');

        var hasManyOfDifferentKinds = count > 1 && kindsCount > 1;
        if (hasManyOfDifferentKinds) builder.Append('(');

        foreach (var element in sortedMembers)
        {
          if (element is TElement)
          {
            builder.Append('\'').Append(element.ShortName).Append('\'');

            if (--count > 0)
            {
              var canUseAnd = !hasManyOfDifferentKinds && count <= 1;
              builder.Append(canUseAnd ? " and " : ", ");
            }
          }
        }

        if (hasManyOfDifferentKinds) builder.Append(')');
      }
    }
  }

  private static void TryReportClosurelessOverloads(ICSharpClosure closure, IClosureCaptures captures, IHighlightingConsumer consumer)
  {
    var closureExpression = closure as ICSharpExpression;
    if (closureExpression == null) return;

    foreach (var capturedEntity in captures.CapturedEntities)
    {
      if (capturedEntity is ILocalFunction)
      {
        return; // we can't pass local function as a TState parameter value w/o allocations
      }
    }

    var invocationReference = ClosurelessOverloadSearcher.FindMethodInvocationByArgument(closureExpression);
    if (invocationReference == null) return;

    var parameter = ClosurelessOverloadSearcher.FindClosureParameter(closureExpression);
    if (parameter == null) return;

    var overloadWithStateParameter = ClosurelessOverloadSearcher.FindOverloadByParameter(parameter);
    if (overloadWithStateParameter != null)
    {
      var highlighting = new CanEliminateClosureCreationHighlighting(closureExpression);
      consumer.AddHighlighting(highlighting, invocationReference.GetDocumentRange());
    }
  }

  [Pure]
  private static DocumentRange GetClosureAllocationRange(ICSharpClosure closure)
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

        return DocumentRange.InvalidRange;
      }

      default:
        return DocumentRange.InvalidRange;
    }
  }
}