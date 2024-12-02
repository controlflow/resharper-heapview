using System.Collections.Generic;
// ReSharper disable RedundantExplicitParamsArrayCreation

ParamsReadOnlyList<int>();
ParamsReadOnlyList<int>([]);
ParamsReadOnlyList(1);
ParamsReadOnlyList([1]);
ParamsReadOnlyList(1, 2, 3);
ParamsReadOnlyList([1, 2, 3]);
ParamsReadOnlyList("abc");
ParamsReadOnlyList(["abc"]);
ParamsReadOnlyList(new List<string> { "aa" });
ParamsReadOnlyList("abc", args[0]);
ParamsReadOnlyList(["abc", args[0]]);
return;

void ParamsReadOnlyList<T>(params IReadOnlyList<T> xs)
{
  _ = xs;
}