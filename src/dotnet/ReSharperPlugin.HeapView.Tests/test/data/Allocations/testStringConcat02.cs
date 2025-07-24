using System;
using System.Linq.Expressions;

class StringConcat
{
  public string Case01(string s, char c)
  {
    s += c + c;
    s += c;
    return s + c;
  }

  public void Case02(string s)
  {
    throw new ArgumentException(message: s + "abc");
  }

  public void Case03()
  {
    Expression expr = (string s) => s + "abc";
  }
}