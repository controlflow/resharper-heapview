using System.Collections.Generic;
using System.Linq;

class Closures
{
  public void M()
  {
    var i = I;
    var xs = XS;
    var t1 = from x1 in xs
             select i + 123; // closure
    var t2 = from x2 in xs
             select x2 + 124;
    var t3 = from x3 in xs
             from y3 in xs // closure
             select i;     // closure
    var t4 = from x4 in xs
             from y4 in XS
             select y4 + x4;
    var t5 = from x5 in xs
             from y5 in XS // anonymous type
             from z5 in YS
             select x5 + y5 + z5;
    var t6 = from x6 in xs
             let y6 = 123 + i     // anonymous + closure
             from z6 in XS
             select x6 + y6 + z6;
    var t7 = from x7 in xs
             from y7 in XS // anonymous type
             where x7 > 0
             select x7 + y7;
    var t8 = from x8 in xs
             from y8 in XS // anonymous type
             from z8 in YS // anonymous type
             from w8 in YS
             select x8 + y8 + z8;
  }

  private static int I { get; set; }
  private static List<int> XS { get; set; }
  private static List<int> YS { get; set; }
}