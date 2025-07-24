class Foo {
  int ThisCapture(int param) {
    var g = () => param;

    {
      var t = 0;
      var f = () => ThisCapture(param) + t;
      return f();
    }
  }
}