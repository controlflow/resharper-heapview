using System;

interface I { int P { get; } }

struct S : I {
  public int P => 42;

  public void M(S s) {
    if (s is object) { }
    if (s is ValueType _) { }
    if (s is I { }) { }

    // boxing
    if (s is object o) { }
    if (s is ValueType v) { }
    if (s is I i) { }
    if (s is I { P: 42 }) { }
    if ((s, s) is (I _, I { } u)) { }
  }
    
  public void G<T>(T t) {
    if (t is object) { }
    if (t is ValueType _) { }
    if (t is I { }) { }

    // possible boxing
    if (t is object o) { }
    if (t is ValueType v) { }
    if (t is Enum e) { }
    if (t is I i) { }
    if (t is I { P: 42 }) { }
    if ((t, t) is (I _, I { } u)) { }
  }
}