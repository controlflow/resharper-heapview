class Foo {
  int ThisCapture(int param) {
    var f = () => ThisCapture(param);
    return f();
  }
}