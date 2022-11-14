#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.Util.DataStructures.Collections;
using ReSharperPlugin.HeapView.Highlightings;

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
    typeof(DelegateAllocationHighlighting)
  })]
public class AllocationOfClosuresAnalyzer : HeapAllocationAnalyzerBase<ITreeNode>
{
  protected override void Run(
    ITreeNode treeNode, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
  {
    using var structure = DisplayClassStructure.Build(treeNode);
    if (structure == null) return;

    foreach (var displayClass in structure.NotOptimizedDisplayClasses)
    {
      ReportDisplayClassAllocation(displayClass, consumer);
    }
  }

  private static void ReportDisplayClassAllocation(IDisplayClass displayClass, IHighlightingConsumer consumer)
  {
    if (displayClass.Members.Count == 0)
      return; // note: should not be possible

    using var sortedMembers = PooledList<IDeclaredElement>.GetInstance();

    sortedMembers.AddRange(displayClass.Members);
    sortedMembers.Sort(DisplayClassMembersDeclarationOrderComparer.Instance);

    var description = CreateDisplayClassDescription();

    var firstDisplayClassMemberDeclaration = TryGetMemberDeclaration(sortedMembers[0]);
    var declarationNode = firstDisplayClassMemberDeclaration != null
      ? GetAllocationNodeFromMemberDeclaration(firstDisplayClassMemberDeclaration)
      : GetAllocationLocationNodeFromScope();

    if (declarationNode != null)
    {
      consumer.AddHighlighting(new ClosureAllocationHighlighting(declarationNode, description));
    }

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
      var parameters = 0;
      var variables = 0;

      using var pooledBuilder = PooledStringBuilder.GetInstance();
      var builder = pooledBuilder.Builder;
      builder.Append("capture of");

      foreach (var declaredElement in sortedMembers)
      {
        if (declaredElement is ITypeElement)
        {
          // 'this' capture
        }
        else if (declaredElement is IParameter)
        {
          parameters++;
        }
        else
        {
          variables++;
        }
      }

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
        for (var index = 0; index < sortedMembers.Count; index++)
        {
          builder.Append(Environment.NewLine).Append("    ");

          AppendMember(sortedMembers[index]);
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

        var sortedMembers = PooledList<IDeclaredElement>.GetInstance();

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
}