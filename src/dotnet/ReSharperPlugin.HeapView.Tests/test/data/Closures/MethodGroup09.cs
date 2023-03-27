using System;

class C {
  public Action<string> A { get; } // IParametersOwner!
  public void Usage() {
    A?.Invoke("aa");
  }
}