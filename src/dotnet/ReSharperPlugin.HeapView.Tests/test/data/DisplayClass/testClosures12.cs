using System;

class Closures
{
  public void M8(bool b)
  {
    int a;
    Console.WriteLine();
    if (b)
    {
      a = I;
      F(() => a + 1);
      F(() => a + 2);
    }
  }

  private static int I { get; set; }
  private static int F<T>(Func<T> f) => 0;
}