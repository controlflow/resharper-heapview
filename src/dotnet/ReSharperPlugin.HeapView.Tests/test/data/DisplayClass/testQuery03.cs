using System.Collections.Generic;
using System.Linq;

class Closures
{
  public void M()
  {
    var i = I;
    var t1 = from x1 in XS
             from y1 in XS // anonymous type
             from z1 in XS // anonymous type
             where x1 > 0
             select x1 + z1;
    var t2 = from x2 in XS
             from y2 in YS.Concat(t1) // closure
             select x2 into y22
             select y22 + i; // closure
    var t3 = from x3 in XS
             group x3 by x3 / 10 + i; // closure
    var t4 = from x4 in XS
             group x4 + i by x4 / 10;
    var t5 = from x5 in XS
             group x5 by x5 / 10 into g
             select g.Key + i; // closure
    var t6 = from x6 in XS
             join y6 in YS on x6 + i // closure
                           equals y6 + i // closure
             select x6 + y6 + i; // closure
    var t7 = from x7 in XS
             join y7 in YS on x7 + i // closure
                           equals y7
             into z7 select x7;
  
    var t8 = from x8 in XS select (
               from y8 in YS select y8);
    var t9 = from x9 in XS select ( // closure
               from y9 in YS.Concat(t7)
               select y9 + x9 + 123); // closure
  }

  private static int I { get; set; }
  private static List<int> XS { get; set; }
  private static List<int> YS { get; set; }
}