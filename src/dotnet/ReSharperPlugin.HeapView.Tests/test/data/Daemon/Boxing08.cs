using System;

public struct S {
  void M() {
    Action a = this.M, // alloc
           a2 = M; // alloc
    string s = nameof(this.M);
    Action b = this.E; // error
  }

  void Generic<T>(T t) where T : I {
    Action a = t.M; // possible
    string s = nameof(t.M);
    Action b = t.E2; // error
  }

  void Generic2<T>(T t) where T : class, I {
    Action a = t.M;
  }

  void Generic3<T>(T t) where T : struct, I {
    Action a = t.M; // alloc
  }
}

public static class X {
  public static void E(this S s) { }
  public static void E2<T>(this T t) { }
}

public interface I {
  void M();
}