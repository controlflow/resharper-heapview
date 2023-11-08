// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
using System;
using System.Linq.Expressions;

class NotImportantContexts
{
  public void Throw01() => throw checked(new Exception());
  public void Throw02() { throw new Exception("aa", new Exception()); }
  public string Throw03(object o) => o as string ?? throw new();

  public Func<object> Func => () => new(); // yes

  public Expression Expr01 => () => new object();
  public Expression<Func<object>> Expr02 => () => new();
}