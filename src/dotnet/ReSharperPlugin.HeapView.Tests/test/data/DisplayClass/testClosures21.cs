using System;

class Closures
{
  public Action M16
  {
    get
    {
      return () =>
      {
        var x = X;
      };
    }
    set
    {
      F(() => value);
    }
  }

  private int X { get; set; }
  private static int F<T>(Func<T> f) => 0;
}