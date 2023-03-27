// ReSharper disable ArrangeRedundantParentheses

class StringConcat
{
  public const string? NotEmptyConst = "aa", EmptyConst = "", NullConst = null;

  public string Folding01(string s) => s + NullConst;
  public string Folding02(string s) => NullConst + s;
  public string Folding03(string s) => s + EmptyConst;
  public string Folding04(string s) => EmptyConst + s;
  public string Folding05(string s) => s + NotEmptyConst; // alloc
  public string Folding06(string s) => NotEmptyConst + s; // alloc
  public string Folding07() => NotEmptyConst + NotEmptyConst;
  public string Folding07(string s) => EmptyConst + s + NullConst;
  public string Folding08(string s) => EmptyConst + s + null;
  public string Folding09(string s) => null + EmptyConst + s;

  public string ConstantFolding(int x, int y) => (x + "aaa") + "bbb" + ('9' + y);

  public string CustomStruct(S1 s1, S2 s2)
    => s1
       + // alloc
       ""
       +
       s2; // alloc

  public string GenericStruct<T>(T t) where T : struct
    => t
       + // no alloc
       "";
}

struct S1
{
  public override string ToString() => "";
}

struct S2 { }