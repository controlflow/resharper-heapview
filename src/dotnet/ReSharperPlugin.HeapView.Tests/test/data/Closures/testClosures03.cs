using System;

var f1 = new Func<string[]>(() => args);
var f2 = new Func<string[]>(delegate { return args; });
Func<string[]> f3 = () => args;
Func<string[]> f4 = new (() => args);