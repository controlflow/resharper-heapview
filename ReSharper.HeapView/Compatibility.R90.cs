using JetBrains.Annotations;
using JetBrains.ReSharper.HeapView;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl.DocumentMarkup;

[assembly: RegisterHighlighter(
    Compatibility.BOXING_HIGHLIGHTING_ID,
    EffectColor = "Red",
    EffectType = EffectType.SOLID_UNDERLINE,
    Layer = HighlighterLayer.SYNTAX,
    VSPriority = VSPriority.IDENTIFIERS)]

[assembly: RegisterHighlighter(
    Compatibility.ALLOCATION_HIGHLIGHTING_ID,
    EffectColor = "Yellow",
    EffectType = EffectType.SOLID_UNDERLINE,
    Layer = HighlighterLayer.SYNTAX,
    VSPriority = VSPriority.IDENTIFIERS)]

namespace JetBrains.ReSharper.HeapView
{
  public static class Compatibility
  {
    public const string BOXING_HIGHLIGHTING_ID = "ReSharper HeapView Boxing";
    public const string ALLOCATION_HIGHLIGHTING_ID = "ReSharper HeapView Allocation";

    public static ITokenNode EquivalenceSign([NotNull] this ILocalVariableDeclaration variableDeclaration)
    {
      return variableDeclaration.EquivalenceSign;
    }
  }
}