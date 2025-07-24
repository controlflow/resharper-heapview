class Foo {
  void ThisCapture() {
    var f = () => ThisCapture();
    f();
  }
}