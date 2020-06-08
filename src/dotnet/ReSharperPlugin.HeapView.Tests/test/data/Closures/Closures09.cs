using System;

class C {
  public C(Action a) { }

  public C(int x) : this(() => x++) {
    int y = x + 1;
    Action b = () => { y++; x++; };
  }
    
  public C(int x, int z) {
    int y = x + z;
    Action b = () => { y++; x++; };
  }
}