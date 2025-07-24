using System;

public struct S : IDisposable {
  public void Dispose() { }
    
  public void M() {
    using var s1 = new S(); // constrained. callvirt
    using S s2 = new S();
    using IDisposable s3 = new S(); // boxing
      
    using (var s4 = new S()) { }
    using (IDisposable s5 = new S()) { } // boxing
    using (new S()) { }
  }
}

public struct U : IDisposable {
  void IDisposable.Dispose() { }
    
  public void M() {
    using var u1 = new U(); // constrained. callvirt
    using U u2 = new U();
    using IDisposable u3 = new U(); // boxing
      
    using (var u4 = new U()) { }
    using (IDisposable u5 = new U()) { } // boxing
    using (new U()) { }
  }
}