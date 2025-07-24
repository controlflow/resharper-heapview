using System.Collections;
using System.Collections.Generic;

class Foreach
{
  public void Method(string str, string[] array, string[,] multiDimArray, dynamic dyn)
  {
    foreach (var _ in str) { }
    foreach (var _ in dyn) { } // alloc, IEnumerator
    foreach (var _ in array) { }
    foreach (var _ in multiDimArray) { }
    foreach (var _ in List()) { }
    foreach (var _ in Enumerable()) { } // alloc
    foreach (var _ in IteratorMethod()) { }
    foreach (var _ in IteratorFunction()) { }
    foreach (var _ in MyCollection()) { } // alloc

    IEnumerable<string> IteratorFunction() { yield break; }
  }

  public void Method2()
  {
    foreach (var _ in Enumerable()) { }
    throw null!;
  }

  private List<string> List() => null!;
  private IEnumerable<string> Enumerable() => null!;
  private IEnumerable<string> IteratorMethod() { yield break; }
  private MyCollection MyCollection() => null!;
}

class MyCollection : IEnumerable<string>
{
  public IEnumerator<string> GetEnumerator()
  {
    yield break;
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}