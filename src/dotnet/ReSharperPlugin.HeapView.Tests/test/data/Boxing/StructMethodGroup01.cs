using System;
using System.Linq.Expressions;

var e = ConsoleKey.Clear;

if (args.Length > 0)
{
  _ = e.GetHashCode();
  throw null; // boxing above is not important
}

Expression<Func<Func<string>>> expr = () => e.ToString;