using System.Collections.Immutable;
// ReSharper disable RedundantExplicitParamsArrayCreation

var array = ImmutableArray.Create(1);

ParamsBuilder<int>();
ParamsBuilder<int>([]);
ParamsBuilder(1);
ParamsBuilder([1]);
ParamsBuilder(1, 2, 3);
ParamsBuilder([1, 2, 3]);
ParamsBuilder("abc");
ParamsBuilder(["abc"]);
ParamsBuilder(array);
ParamsBuilder("abc", args[0]);
ParamsBuilder(["abc", args[0]]);
return;

void ParamsBuilder<T>(params ImmutableArray<T> xs)
{
  _ = xs;
}