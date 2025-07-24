public class C {
  public void M1() {
    var s = new S();
    s.InstanceMethod();
    s.GetType();
    s.GetHashCode();
    s.Equals(null);
  }

  public void M2<T>(T t) where T : struct, I {
    t.InstanceMethod();
    t.GetType();
    t.GetHashCode();
    t.Equals(null);
  }

  public void M3<T>(T t) where T : I {
    t.InstanceMethod();
    t.GetType();
    t.GetHashCode();
    t.Equals(null);
  }

  public struct S : I {
    public void InstanceMethod() { }
    public override bool Equals(object obj) {
      return base.Equals(null);
    }
  }

  public interface I {
    void InstanceMethod();
  }
}