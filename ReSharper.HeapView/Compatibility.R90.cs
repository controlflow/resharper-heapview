using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.HeapView
{
  public static class Compatibility
  {
    public const string BOXING_HIGHLIGHTING_ID = "ReSharper HeapView Boxing";
    public const string ALLOCATION_HIGHLIGHTING_ID = "ReSharper HeapView Allocation";
    public const string STRUCT_COPY_ID = "ReSharper HeapView Struct copy";

    public static ITokenNode EquivalenceSign([NotNull] this ILocalVariableDeclaration variableDeclaration)
    {
      return variableDeclaration.EquivalenceSign;
    }
  }
}
