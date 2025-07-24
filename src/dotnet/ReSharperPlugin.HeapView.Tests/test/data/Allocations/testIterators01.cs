using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

// ReSharper disable ValueParameterNotUsed

class Iterators
{
  public IEnumerable IteratorProperty01
  {
    get { yield break; }
    set { }
  }

  public IEnumerable<int> IteratorProperty02 { get { yield break; } }
  public static IEnumerable IteratorMethod01() { yield break; }
  public static IEnumerable<int> IteratorMethod02() { yield break; }

  public void Usage()
  {
    IEnumerable IteratorFunction01() { yield break; }
    IEnumerable<int> IteratorFunction02() { yield break; }

    _ = IteratorProperty01; // alloc
    IteratorProperty01 = null!;
    _ = nameof(IteratorProperty01);
    _ = nameof(IteratorProperty01.GetEnumerator);

    _ = IteratorProperty02; // alloc
    _ = IteratorMethod01;
    _ = IteratorMethod01(); // alloc
    _ = IteratorMethod02(); // alloc
    _ = IteratorFunction01(); // alloc
    _ = IteratorFunction02(); // alloc

    Expression unused = () => IteratorProperty01;
  }
}