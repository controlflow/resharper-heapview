A a = (1, 2);
M((1, 2));
var t = (1, 2);
M(t);
var aa = (A) t;
var ab = (A) (1, 2);
void M(A x) { }

class A {
  public static implicit operator A((object, int) t) => null;
}