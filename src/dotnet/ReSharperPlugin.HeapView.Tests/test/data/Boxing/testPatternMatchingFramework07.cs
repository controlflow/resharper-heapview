using System;
using System.Linq.Expressions;

public class C<TUnconstrained>
{
  public Expression<Func<TUnconstrained, bool>> Expr = t => t is int;
  public Func<TUnconstrained, bool> Func => t => t is int;
}