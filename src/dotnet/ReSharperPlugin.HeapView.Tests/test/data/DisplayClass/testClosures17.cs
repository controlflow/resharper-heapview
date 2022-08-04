using System;

class Closures
{
  public void M12()
  {
    var i = I;
    {
      var j = I;
      {
        F(() =>
        {
          M();
          return i + j;
        });
      }
    }
  }

  private int M() => 0;
  private static int I { get; set; }
  private static int F<T>(Func<T> f) => 0;
}