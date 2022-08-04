using System;

class Closures
{
  public void M10(bool t)
  {
    if (t)
    {
      var a = I;
      F(() => a);

      {
        var c = I;
        F(() => a + c);

        var d = I;
        F(() => a + d);
      }
    }
    else
    {
      var b = I;
      F(() => b + 1);
      F(() => b + 2);
    }
  }

  private static int I { get; set; }
  private static int F<T>(Func<T> f) => 0;
}