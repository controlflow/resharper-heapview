using System;
using System.Collections.Generic;

[Test(
  new [] { typeof(HeapAllocations) },
  new int(), typeof(string))]
class HeapAllocations
{
  // ReSharper disable UnusedVariable
  // ReSharper disable UnusedVariable.Compiler
  // ReSharper disable InconsistentNaming

  public void M1<T, U, V>()
    where T : new()
    where U : class, new()
    where V : struct
  {
    var t0 = new int();
    var t1 = new string('a', 100);
    var t2 = new object();
    var t3 = new T();
    var t4 = new U();
    var t5 = new V();
    var t6 = new {Foo = 123};
  }

  private static readonly int[] xs0 = {1, 2, 3};
  private int[] xs1 = {1, 2, 3};
  private static readonly int[] xs2 = new[] {1, 2, 3};
  private int[] xs3 = new[] {1, 2, 3};

  public void M2()
  {
    var ts0 = new int[] {};
    var ts1 = new int[0];
    var ts2 = new int[100];
    var ts3 = new string[] { };
    var ts4 = new[] {1, 2, 3};
    var ts5 = new int[2, 2];
    int[] ts6 = {},
          ts7 = {1,2,3};
  }

  public void M3()
  {
    P0();
    P0(1, 2);
    P0(1, 2, 3);
    P0(null); // nothing
    P0(xs2); // nothing
    P1(1, "abc");
    P1(1, "abc", 1);
    P1(1, "abc", xs0); // nothing
    P1(1, "abc", null); // nothing
    P2(null); // nothing
    P2(null, null);
    P3<int>(xs0); // nothing
  }

  public void M4()
  {
    var ts0 = I0();
    var ts1 = I1;
    I1 = null; // nothing
  }

  public void M5()
  {
    var xs = new List<int>();
    IList<int> ys = xs;

    foreach (var i in xs) { }
    foreach (var i in ys) { }
  }

  public void M6(string s, int i)
  {
    GC.KeepAlive("aa" + "bb");
    const string bb = "bb";
    GC.KeepAlive("aa" + bb);
    GC.KeepAlive("aa" + null);
    GC.KeepAlive(null + "bb");
    GC.KeepAlive("bb" + s);
    GC.KeepAlive("bb" + s + s);
    GC.KeepAlive("bb" + s + s + s);
    GC.KeepAlive(string.Concat("bb", s, s, s, s));
    GC.KeepAlive("bb" + s + s + s + s);
    GC.KeepAlive("bb" + s + ("bb" + "cvcv" + s) + s);
    GC.KeepAlive("bb" + s + s + s + s + s);
    GC.KeepAlive("bb" + s + s + (s + s + s));
    GC.KeepAlive("bb" + (s + s) + s + (s + s));
    GC.KeepAlive("aa" + i);
    GC.KeepAlive(i + "aa");

    string t = "asas";
    t += "sdsdsd";
    t += "sdsdsd";

    s += "aaa";
    s += "bb";

    GC.KeepAlive(t + s);
  }

  private static void P0(params int[] xs) { }
  private static void P1(int a, string b, params int[] xs) { }
  private static void P2(params string[] xs) { }
  private static void P3<T>(params T[] xs) { }

  private IEnumerable<int> I0() { yield break; }
  private IEnumerable<int> I1 { get { yield break; } set { } }
}

class TestAttribute : Attribute
{
  public TestAttribute(Type[] xs, int a, params Type[] ys) { }
}