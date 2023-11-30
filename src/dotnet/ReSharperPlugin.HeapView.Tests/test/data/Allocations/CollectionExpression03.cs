using System.Collections.Generic;

void Generics<TList, TFoo>()
  where TList : List<int>, new()
  where TFoo : IFoo, new()
{
  TList xs = []; // yes
  TFoo ys = []; // possible
}

interface IFoo : IEnumerable<int>
{
  void Add(int x);
}