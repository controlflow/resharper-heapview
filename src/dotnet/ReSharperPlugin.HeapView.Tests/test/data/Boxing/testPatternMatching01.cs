// ReSharper disable ArrangeRedundantParentheses

class Patterns
{
  public bool ConstantPattern1(object obj) => obj is 42;
  public bool ConstantPattern2(object obj) => obj is (true);
  public bool ConstantPattern3(System.ValueType obj) => obj is > 42;
  public bool ConstantPattern4(object obj) => obj is "abc";
}