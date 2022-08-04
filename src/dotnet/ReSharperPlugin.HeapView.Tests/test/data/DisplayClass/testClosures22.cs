using System;

class Closures
{
  public Action this[int i]
  {
    get
    {
      return () =>
        F(() => X + i);
    }
    set
    {
      var y = F(() => X + i);
      F(z => y);
    }
  }

  private Action<int>
    f = x =>
    {

      F(() => x);
    },
    g = x =>
    {
      F(() => x);
    };

  private Action<int, int, int>
    tt = (x, y, z) =>
    {
      F(() => y);
    };

  private int X { get; set; }
  private static int F<T>(Func<T> f) => 0;
  private static void F(Func<int, int> f) { }
}