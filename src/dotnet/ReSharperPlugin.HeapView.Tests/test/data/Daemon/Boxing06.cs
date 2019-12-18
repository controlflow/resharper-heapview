public class C {
  public static implicit operator (int, (int, int)) (C c) => (1, (2, 3));
  public void M(C c) {
    (object, (int, int)) oo1 = c;
    (int, (object, int)) oo2 = c;
    (object, (int, int)) oo3 = (1, (2, 3));
    (int, (object, int)) oo4 = (1, (2, 3));
  }
}