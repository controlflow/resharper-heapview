public class StructDisplayClass {
  int Method() {
    int x = 0;
    int Local() => x + Method();
    return Local();
  }

  int Other() {
    int x = 0;
    int Local() => x + Method();
    var f = Local;
    return f();
  }
}