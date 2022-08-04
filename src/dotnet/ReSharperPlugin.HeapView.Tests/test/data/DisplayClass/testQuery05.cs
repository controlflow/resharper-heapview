using System;
using System.Collections.Generic;
using System.Linq;

class Closures
{
  public void M14<T>()
  {
    F(() => { });

    var xs = from x in XS
             where x > 0
             group x + 1 by x + 2
             into x
             join y in YS on x.Key equals y
             let t = 1
             select y + t;
  }

  private static List<int> XS { get; set; }
  private static List<int> YS { get; set; }
  private static void F(Action f) { }
}