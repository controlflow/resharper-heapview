using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;

namespace ReSharperPlugin.HeapView
{
  public static class CommonUtils
  {
    [Pure]
    public static bool IsStringConcatOperatorReference([CanBeNull] this IReference reference)
    {
      if (reference?.Resolve() is (ISignOperator { IsPredefined: true, Parameters: { Count: 2 } parameters }, _))
      {
        var lhsType = parameters[0].Type;
        var rhsType = parameters[1].Type;

        if (lhsType.IsString()) return rhsType.IsString() || rhsType.IsObject();
        if (rhsType.IsString()) return lhsType.IsString() || lhsType.IsObject();
      }

      return false;
    }
  }
}