using System;

class Closures
{
  public void M9(bool t)
  {
    if (t)
    {
      var a = I;
      F(() => a + 1);
      F(() => a + 2);
    }
    else
    {
      int z = I, b = I;
      F(() => b + 1);
      F(() => b + 2);
    }
  }

  private static int I { get; set; }
  private static int F<T>(Func<T> f) => 0;
}