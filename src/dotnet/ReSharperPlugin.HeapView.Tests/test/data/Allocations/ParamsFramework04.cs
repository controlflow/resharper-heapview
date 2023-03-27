using System.Collections.Generic;
// ReSharper disable RedundantCollectionInitializerElementBraces

new MyList
{
  111, // alloc
  {222}, // alloc
  {333, 333}, // alloc
  {"aaa"}, // alloc
  {"aaa", "bbb"} // alloc
};

public class MyList : List<int>
{
  public void Add(int x, params int[] xs) { }
  public void Add(params string[] xs) { }
}