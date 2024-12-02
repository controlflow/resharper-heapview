using System.Collections.Generic;
// ReSharper disable RedundantExplicitParamsArrayCreation

ParamsList<int>();
ParamsList<int>([]);
ParamsList(1);
ParamsList([1]);
ParamsList(1, 2, 3);
ParamsList([1, 2, 3]);
ParamsList("abc");
ParamsList(["abc"]);
//ParamsList(new List<string>(args));
ParamsList("abc", args[0]);
ParamsList(["abc", args[0]]);
return;

void ParamsList<T>(params List<T> xs)
{
  _ = xs;
}

void Generics<T>(params T x) where T : IAddable<int>, new()
{
  T xs1 = [];
  T xs2 = [1];
  Generics<T>();
  Generics<T>(1);
}

interface IAddable<T> : IEnumerable<T>
{
  void Add(T t);
}