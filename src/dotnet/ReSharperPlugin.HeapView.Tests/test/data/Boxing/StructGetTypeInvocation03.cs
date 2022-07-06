using System;
using System.Linq.Expressions;

var e = ConsoleKey.Clear;
Expression<Func<Type>> expr = () => e.GetType();