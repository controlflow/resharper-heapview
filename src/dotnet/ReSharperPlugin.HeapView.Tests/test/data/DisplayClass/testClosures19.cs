using System;

class Closures
{
  public void M13()
  {
    F(() =>
    {
      F(() =>
      {
        M();
        M();
      });
    });
  }

  private int M() => 0;
  private static int I { get; set; }
  private static void F(Action f) { }
  private static int F<T>(Func<T> f) => 0;
}