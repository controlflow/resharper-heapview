using System.Collections;
using System.Collections.Generic;
// ReSharper disable ValueParameterNotUsed
#pragma warning disable CS1998

/// <summary>
/// <see cref="IteratorProperty01"/>
/// </summary>
public class Iterators
{
  public IEnumerable IteratorProperty01 { get { yield break; } set { } }
  public static async IAsyncEnumerable<int> IteratorMethod01() { yield break; }

  public void Usage()
  {
    static async IAsyncEnumerable<int> IteratorFunction01() { yield break; }

    _ = IteratorProperty01; // alloc
    _ = IteratorMethod01;
    _ = IteratorMethod01(); // alloc
    _ = IteratorFunction01(); // alloc
  }
}