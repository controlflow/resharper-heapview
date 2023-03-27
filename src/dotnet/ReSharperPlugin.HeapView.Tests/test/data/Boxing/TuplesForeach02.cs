// ReSharper disable MoreSpecificForeachVariableTypeAvailable
#nullable enable

int[] xs = null!;
string[] ys = null!;

foreach (object x in xs) { }
foreach (object y in ys) { }
foreach (object _ in xs) { } // optimized
foreach (object _ in xs) { var t = _ }