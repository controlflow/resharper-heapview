using System;

[My(42)]
class MyAttribute : Attribute {
  [My(new[] { 1, 2, 3 })]
  public MyAttribute(object o) { }
}

class MyException : Exception {
  public MyException(object box) { }
}

class ThrowContexts {
  public object M1() {
    return new MyException(42); // alloc, box
  }

  public object M2() {
    throw new MyException(42);
  }

  public object M3(object o) {
    return o ?? throw new MyException(42);
  }

  public object M4(object o) {
    if (o is null) {
      var exc = new MyException(42);
      while (o is null) { break; }
      if (o is 42) {  label: ; } else { ; }

      throw exc;
    }

    return o;
  }

  public object M5(object o) {
    var exc = new MyException(42); // alloc, box
    if (o is null) {
      return "aa";
    }

    throw exc;
  }
}