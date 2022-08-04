using System;

class Closures
{
  public void M15(int arg)
  {
    var y = F(() => X + arg);
    F(z => y);
  }

  private int X { get; set; }
  private static void F(Action f) { }
  private static int F<T>(Func<T> f) => 0;
  private static void F(Func<int, int> f) { }
}