using System;
using System.Collections;

public class C {
  public IEnumerable M(int p) {
    int x = p;
    void L1() { }
    void L2() { }
    void L3() => x++;

    if (p > 0) {
      int u = x;
      L1();
      L2();
      Func<int> a = () => p + u;
      L3();
      Action l2 = L2;
    }

    yield break; // whole body is delayed
  }
}