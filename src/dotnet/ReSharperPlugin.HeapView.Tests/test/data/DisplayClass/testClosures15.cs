using System;

class Closures
{
  public void M11(int arg)
  {
    F(() =>
    {
      // note: Action<int> delegate is cached in instance field of containing display class
      //       nothing additional is leaked through this delegate field
      F(delegate(int u)
      {
        F(t => arg + t + u);
      });
    });
  }

  private static int I { get; set; }
  private static void F(Action f) { }
  private static int F<T>(Func<T> f) => 0;
  private static void F(Action<int> f) { }
  private static void F(Func<int, int> f) { }
}