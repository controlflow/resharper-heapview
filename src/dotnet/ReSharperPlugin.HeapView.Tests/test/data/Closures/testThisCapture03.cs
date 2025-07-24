class Foo {
  int ThisCapture(int param) {
    if (param < 0) {
      var f = () => ThisCapture(42);
      return f();
    } else {
      var f = () => ThisCapture(param);
      return f();  
    }
  }
}