(int, object) a = new A();
M(new A());
void M((int, object) x) { }

struct A {
  public static implicit operator (int, int)(A t) => default;
}