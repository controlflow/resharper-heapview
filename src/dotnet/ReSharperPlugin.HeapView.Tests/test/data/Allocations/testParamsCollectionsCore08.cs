using System.Collections.Generic;
// ReSharper disable RedundantExplicitParamsArrayCreation

ParamsCollection<int>();
ParamsCollection<int>([]);
ParamsCollection(1);
ParamsCollection([1]);
ParamsCollection(1, 2, 3);
ParamsCollection([1, 2, 3]);
ParamsCollection("abc");
ParamsCollection(["abc"]);
ParamsCollection(new List<string> { "aa" });
ParamsCollection("abc", args[0]);
ParamsCollection(["abc", args[0]]);
return;

void ParamsCollection<T>(params ICollection<T> xs)
{
  _ = xs;
}