using System.Collections.Generic;
// ReSharper disable RedundantExplicitParamsArrayCreation

ParamsEnumerable<int>(); // no Array.Empty<T>()
ParamsEnumerable<int>([]); // no Array.Empty<T>()
ParamsEnumerable(1);
ParamsEnumerable([1]);
ParamsEnumerable(1, 2, 3);
ParamsEnumerable([1, 2, 3]);
ParamsEnumerable("abc");
ParamsEnumerable(["abc"]);
ParamsEnumerable(new List<string> { "aa" });
ParamsEnumerable("abc", args[0]);
ParamsEnumerable(["abc", args[0]]);
return;

void ParamsEnumerable<T>(params IEnumerable<T> xs)
{
  _ = xs;
}