// ReSharper disable ReturnValueOfPureMethodIsNotUsed
// ReSharper disable CheckNamespace
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Global
// ReSharper disable ConvertToConstant.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantBaseQualifier
// ReSharper disable RedundantThisQualifier

using System;

enum E { C }
interface I { void M(); }
struct S : I {
  public void M() { }
  static void T() { }
  public override int GetHashCode() {
    Action f = M, g = T;
    Func<int> g1 = base.GetHashCode,
              g2 = this.GetHashCode,
              g3 = GetHashCode;
    return new[] {
      base.ToString(),
      this.ToString(),
      ToString()
    }.Length;
  }
}

static class X {
  public static void E(this I i) { }
}

static class A {
  static void Main() {
    var s = new S();
    var e = E.C;

    var x = "aa" + s + 'a';
    Object    o1 = s, o2 = (S?) s;
    ValueType v1 = s, v2 = (S?) s;
    Enum      y1 = e, y2 = (E?) e;
    I         i1 = s, i2 = (S?) s;

    Action f = s.M;
    Func<int> g = e.GetHashCode;
    s.E();
    s.Equals(null);
    s.GetType();
    e.ToString();
    e.GetHashCode();

    // no boxing
    s.M();
    s.GetHashCode();
  }

  static void F<T>() where T : struct, I {
    var s = new T();

    Object    o1 = s, o2 = (T?) s;
    ValueType v1 = s, v2 = (T?) s;
    I         i1 = s, i2 = (T?) s;

    Action f = s.M;
    s.E();
    s.GetType();

    // possible
    s.GetHashCode();
    s.ToString();
    s.Equals(null);

    // no boxing
    s.M();
  }
}