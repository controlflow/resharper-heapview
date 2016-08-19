using System;
using System.Linq.Expressions;

class Foo
{
  public object Property => 42;
  public Func<int> M(int x) => () => x;
  public Func<int> this[int index] => () => index;
  public Expression<Func<int>> E => () => 42;
  public Func<int, Func<int>> D = x => () => x;
  public Func<int, Func<int>> P { get; } = x => () => x;
  public event Func<int, Func<int>> E1 = x => () => x;
}