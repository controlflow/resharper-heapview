using System;

interface I { int P { get; } }

struct S : I {
  public int P => 1;
  public object M(S s) {
    var t = (I)s;
    GC.KeepAlive(t.P);
      
    /*if (s is I { P: var p }) {
      return "";
    }*/
      
    return null;
  }
}