interface I { }

class C {
  // unboxing
  public T M1<T>(C c) where T : C => (T) c;
  public T M2<T>(I i) where T : I => (T) i;
  public T M3<T>(I i) => (T) i;

  public I M4<T>(T t) => (I) t; // possible
  public I M5<T>(T t) where T : struct => (I) t; // boxing
  public U M6<T, U>(T t) where U : T => (U) t; // very unlikely

  // unboxing
  public bool P1<T>(C c) where T : C => c is C x;
  public bool P2<T>(I i) where T : I => i is T t;
  public bool P3<T>(I i) => i is T t;

  public bool P4<T>(T t) => t is I i; // possible
  public bool P5<T>(T t) where T : struct => t is I i; // boxing
  public bool P6<T, U>(T t) where U : T => t is U u; // very unlikely
}