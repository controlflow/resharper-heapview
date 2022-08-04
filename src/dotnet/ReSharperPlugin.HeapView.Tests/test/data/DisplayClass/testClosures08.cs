using System;

class Closures
{
  public void M4()
  {
    int i = I, j = I;
    F(() => M() + X);
    F(() => M() + X + i);
    F(() => X + j);
  }

  private static int I { get; set; }
  private int X { get; set; }
  private int M() => 0;
  private static int F<T>(Func<T> f) => 0;
}