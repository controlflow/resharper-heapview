using System;

class Closures
{
  public void M6()
  {
    var i = I;
    F(() => i);

    var j = I;
    F(() => j);
  }

  private static int I { get; set; }
  private int M() => 0;
  private static int F<T>(Func<T> f) => 0;
}