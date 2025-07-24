using System;
using System.Linq.Expressions;

var e = ConsoleKey.Clear;
Expression<Func<int>> expr = () => e.GetHashCode();