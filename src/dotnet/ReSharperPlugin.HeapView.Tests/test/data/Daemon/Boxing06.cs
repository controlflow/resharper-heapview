public class C {
  public static implicit operator (int, (int, int)) (C c) => (1, (2, 3));
  public static implicit operator C ((object, int) c) => null;
    
  public void M(C c, (int, int) it) {
    C i1 = (1, 2);
    C i2 = it;
    (object, (int, int)) oo1 = c;
    (int, (object, int)) oo2 = c;
    (object, (int, int)) oo3 = (1, (2, 3));
    (int, (object, int)) oo4 = (1, (2, 3));
  }
}