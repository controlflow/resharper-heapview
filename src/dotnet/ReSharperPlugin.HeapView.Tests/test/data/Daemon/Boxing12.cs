public interface I { }
public interface I<out T> { }

public class B : I { }
public class C : B, I<string> {
  public object M1<T>(T t) => t; // possible boxing
  public object M2<T>(T t) where T : struct => t; // boxing
  public object M3<T>(T? t) where T : struct => t; // boxing
  public object M4<T>(T t) where T : class => t;
  public object M5<T>(T t) where T : notnull => t; // possible

  // no boxing, all reference types
  public B B1<T>(T t) where T : C => t;
  public C B2<T>(T t) where T : C => t;
  public I B3<T>(T t) where T : C => t;
  public I<object> B4<T>(T t) where T : C => t;    

  // possible
  public I I1<T>(T t) where T : I, I<string> => t;
  public I<object> I2<T>(T t) where T : I, I<string> => t;

  public U G1<T, U>(T t) where T : U => t; // very unlikely
  public U G2<T, U>(T t) where T : struct, U => t; // possibly

  public dynamic D1<T>(T t) where T : struct => t; // boxing
  public dynamic D2<T>(T t) where T : class => t;
  public dynamic D3<T>(T t) => t; // possible
}

class B<T1, T2, T3> {
  // possible, but very unlikely
  public virtual T1 M1<T>(T t) where T : T1 => t;
  public virtual T2 M2<T>(T t) where T : T2 => t;
  public virtual T3 M3<T>(T t) where T : T3 => t;
  public virtual T2 M4<T>(T t) where T : struct, T2 => t;
}

class CC : B<object, System.ValueType, System.Enum> {
  // possible, more likely
  public override object M1<T>(T t) => t;
  public override System.ValueType M2<T>(T t) => t;
  public override System.Enum M3<T>(T t) => t;
  public override System.ValueType M4<T>(T t) => t; // boxing
}