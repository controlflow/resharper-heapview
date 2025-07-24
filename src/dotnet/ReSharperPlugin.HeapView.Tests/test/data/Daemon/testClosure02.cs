using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

// ReSharper disable TooWideLocalVariableScope
// ReSharper disable UnusedVariable
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable ConvertToLambdaExpression

class Closures
{
  public void M2()
  {
    var i = I;
    var xs = XS;
    var t1 = from x in xs
             select i + 123; // closure
    var t2 = from x in xs
             select x + 123; // nothing
    var t3 = from x in xs
             from y in xs // closure
             select i;    // closure
    var t4 = from x in xs
             from y in XS  // nothing
             select y + x; // nothing
    var t5 = from x in xs
             from y in XS // anonymous type
             from z in YS // nothing
             select x + y + z;
    var t6 = from x in xs
             let y = 123 + i  // anonymous + closure
             from z in XS // nothing
             select x + y + z; // nothing
    var t7 = from x in xs
             from y in XS // anonymous type
             where x > 0  // nothing
             select x + y; // nothing
    var t8 = from x in xs
             from y in XS // anonymous type
             from z in YS // anonymous type
             from w in YS
             select x + y + z;
  }
  
  public void M3()
  {
    var i = I;
    var t1 = from x in XS
             from y in XS // anonymous type
             from z in XS // anonymous type
             where x > 0
             select x + z;
    var t2 = from x in XS
             from y in YS.Concat(t1)
             select x into y
             select y + i;
    var t3 = from x in XS
             group x by x / 10 + i;
    var t4 = from x in XS
             group x + i by x / 10;
    var t5 = from x in XS
             group x by x / 10 into g
             select g.Key + i;
    var t6 = from x in XS
             join y in YS on x + i equals y + i
             select x + y + i;
    var t7 = from x in XS
             join y in YS on x + i equals y
             into z select x;
  
    var t8 = from x in XS select (
               from y in YS select y);
    var t9 = from x in XS select (
               from y in YS.Concat(t7)
               select y + x + 123);
  }

  public void M4()
  {
    int i = I, j = I;
    F(() => M() + X);
    F(() => M() + X + i);
    F(() => X + j);
  }

  public void M5()
  {
    var i = I;
    F(() => i);

    {
      var j = I;
      F(() => j);
    }
  }

  public void M6()
  {
    var i = I;
    F(() => i);

    var j = I;
    F(() => j);
  }

  public void M7()
  {
    F(() =>
    {
      M();
      var i = I;
      F(() => i);
    });
  }

  public void M8(bool b)
  {
    int a;
    Console.WriteLine();
    if (b)
    {
      a = I;
      F(() => a);
      F(() => a);
    }
  }

  public void M9(bool t)
  {
    if (t)
    {
      var a = I;
      F(() => a);
      F(() => a);
    }
    else
    {
      int z = I, b = I;
      F(() => b);
      F(() => b);
    }
  }

  public void M10(bool t)
  {
    if (t)
    {
      var a = I;
      F(() => a);

      {
        var c = I;
        F(() => a + c);

        var d = I;
        F(() => a + d);
      }
    }
    else
    {
      var b = I;
      F(() => b);
      F(() => b);
    }
  }

  public void M11(int arg)
  {
    F(() =>
    {
      F(delegate(int u)
      {
        F(t => arg + t + u);
      });
    });
  }

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

  public void M14<T>()
  {
    F(() => { });

    var xs = from x in XS
             where x > 0
             group x + 1 by x + 2
             into x // should not be highlighted
             join y in YS on x.Key equals y
             let t = 1
             select y + t;
  }

  public void M15(int arg)
  {
    var y = F(() => X + arg);
    F(z => y);
  }

  public Action M16
  {
    get
    {
      return () => {
        var x = X;
      };
    }
    set
    {
      F(() => value);
    }
  }

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

  private IEnumerable<int> xsss = from x in Enumerable.Range(0, 100)
                                  where x < 0
                                  select F(() => x);

  public void M17(int arg)
  {
    Expression<Func<int, int>> e = x => x;

    var xs = Enumerable.Range(0, 10).AsQueryable();
    var ys = from x in xs
             where x > 0
             select x + 1;

    Expression<Func<int>> f = () => arg;
    Expression<Action<int>> g = t => F(() => t + arg);
    Expression<Func<int, int>> u = t => 1;
  }

  private static int I { get; set; }
  private int X { get; set; }
  private static List<int> XS { get; set; }
  private static List<int> YS { get; set; }
  private int M() { return 0; }
  private static void F(Action f) { }
  private static int F<T>(Func<T> f) { return 0; }
  private static void F(Action<int> f) { }
  private static void F(Func<int, int> f) { }
}