using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.HeapView
{
  public static class Compatibility
  {
    // ReSharper disable once StringLiteralTypo
    public const string BOXING_HIGHLIGHTING_ID = "ReSharper Boxing Occurrance";
    public const string ALLOCATION_HIGHLIGHTING_ID = "ReSharper Heap Allocation";

    public static ITokenNode EquivalenceSign([NotNull] this ILocalVariableDeclaration variableDeclaration)
    {
      return variableDeclaration.AssignmentSign;
    }

    public static bool IsLinqExpression(this IType type)
    {
      return type.IsExpression();
    }

    // ReSharper disable once UnusedParameter.Global
    public static bool IsCSharp6Supported(this ITreeNode treeNode)
    {
      return false;
    }

    [NotNull]
    public static PredefinedType GetPredefinedType(this ITreeNode treeNode)
    {
      return treeNode.GetPsiModule().GetPredefinedType(treeNode.GetResolveContext());
    }
  }
}