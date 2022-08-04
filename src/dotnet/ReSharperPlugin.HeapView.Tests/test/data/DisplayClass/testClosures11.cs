using System;

class Closures
{
  public void M7()
  {
    F(() =>
    {
      M();
      var i = I;
      F(() => i);
    });
  }

  private static int I { get; set; }
  private int M() => 0;
  private static int F<T>(Func<T> f) => 0;
  private static void F(Action f) { }
}