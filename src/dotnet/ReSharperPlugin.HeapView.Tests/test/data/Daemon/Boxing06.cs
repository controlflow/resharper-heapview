public class C {
  public static implicit operator (int, (int, int)) (C c) => (1, (2, 3));
  public static implicit operator C ((object, int) c) => null;
  public void Deconstruct(out bool x, out short y) { x = true; y = 0; }
    
  public void Convert(C c, (int, int) it) {
    C i1 = (1, 2);
    C i2 = it;
    (object, (int, int)) oo1 = c;
    (int, (object, int)) oo2 = c;
    (object, (int, int)) oo3 = (1, (2, 3));
    (int, (object, int)) oo4 = (1, (2, 3));
  }

  public void Deconstruct(C c, (int, int) it) {
    // boxing in declaration:
    (object o1, int x1) = (1, 2);
    (object o2, int x2) = it;
    (object o3, int x3) = c;

    (object, int) t;
    t = c; // error
    t = it; // boxing

    object ox;
    int ix;
    (ox, ix) = c;
    (ox, ix) = it;

    var tt1 = (ox, _) = c;
    var tt2 = (ox, _) = it;
  }
}

struct S {
  public static explicit operator S((object, int) t) => default;

  public void M((short, byte) t) {
    var s = (S) t;
  }
}