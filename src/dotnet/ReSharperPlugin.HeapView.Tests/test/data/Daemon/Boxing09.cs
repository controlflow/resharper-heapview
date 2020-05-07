using System;

public enum E { }

public struct S {
  public void Implicit(S x, E y, E? z) {
    object o = x;
    ValueType v = x;
    Enum e = y;
    object n = z;
    dynamic d = z;
  }
    
  public void Explicit(S x, E y, E? z) {
    var o = (object) x;
    var v = (ValueType) x;
    var e = (Enum) y;
    var n = (object) z;
    var d = (dynamic) z;
  }
}