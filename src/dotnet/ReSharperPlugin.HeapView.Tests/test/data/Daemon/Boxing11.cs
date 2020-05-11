using System;

interface I { int P { get; } }
interface I2 { }

struct S : I {
  public int P => 42;

  public void M(S s) {
    // all statically known, no boxing
    if (s is object) { }
    if (s is ValueType) { }
    if (s is ValueType _) { }
    if (s is I { }) { }
    if (s is I) { }
    if (s is I2) { }
    if (s is int) { }

    // boxing
    if (s is object o) { }
    if (s is ValueType v) { }
    if (s is I i) { }
    if (s is I { P: 42 }) { }
    if ((s, s) is (I _, I { } u)) { }
  }

  public void G<T>(T t) {
    if (t is object) { }
    // possible boxing in .net fw
    if (t is ValueType _) { }
    if (t is ValueType) { }
    if (t is I { }) { }
    if (t is I) { }
    if (t is I2) { }
    if (t is int) { }

    // possible boxing
    if (t is object o) { }
    if (t is ValueType v) { }
    if (t is Enum e) { }
    if (t is I i) { }
    if (t is I { P: 42 }) { }
    if ((t, t) is (I _, I { } u)) { }
    if (t is int x) { }
  }

  public void V<T>(T t) where T : struct {
    if (t is object) { }
    if (t is ValueType) { }
    if (t is ValueType _) { }
    // possible boxing in .net fw
    if (t is I) { }
    if (t is I { } _) { }
    if (t is I2 { }) { }
    if (t is int) { }

    var ss = t switch { I _ => 1, I2 _ => 2, _ => -1 };

    // possible boxing
    if (t is object o) { }
    if (t is ValueType v) { }
    if (t is Enum e) { }
    if (t is I i) { }
    if (t is I { P: 42 }) { }
    if ((t, t) is (I _, I { } u)) { }
    if (t is int x) { }
  }
}