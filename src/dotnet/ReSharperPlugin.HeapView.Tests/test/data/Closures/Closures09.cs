using System;
using System.Linq;
using System.Linq.Expressions;

int x = 0;
Func<int> func = () => x;
var funcAnon = () => x;
Expression exprAnon = () => x;
Expression<Func<int>> exprFunc = () => x;
var ys = from _ in args where x > 0 select x;