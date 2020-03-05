using System;
// ReSharper disable ConvertClosureToMethodGroup

public class Initializers {
  public Func<int, Func<int>> Field = x => () => x;
  public static event Func<int, Func<int>> Event = x => () => x;
  public Func<int, Func<int>> AutoProperty { get; } = x => () => x;
}

public class BlockBodies {
  static BlockBodies() {
    var x = 0;
    F(() => x);
  }

  public BlockBodies(int x) {
    F(() => x);
  }

  public void Method(int x) {
    F(() => x);
  }

  public Func<int> Property {
    get {
      var x = 0;
      return () => x;
    }
    set {
      F(() => value());
    }
  }

  public int this[int x] {
    get {
      F(() => x);
      return x;
    }
    set {
      F(() => value);
    }
  }

  public static implicit operator BlockBodies(int x) {
    F(() => x);
    return null;
  }

  public static Func<int> operator+(BlockBodies _, int x) {
    return () => x;
  }

  private static extern void F(Func<int> f);
}

public class ExpressionBodies {
  static ExpressionBodies() => F(x => () => x);
  public ExpressionBodies(int x) => F(() => x);

  public void Method(int x) => F(() => x);

  public int Property => F(x => () => x);
  public int this[int x] => F(() => x);

  public int GetSetProperty {
    get => F(x => () => x);
    set => F(() => value);
  }

  public int this[int x, int y] {
    get => F(() => x);
    set => F(() => value + y);
  }

  public static implicit operator int(ExpressionBodies _) => F(x => () => x);
  public static int operator +(ExpressionBodies _, int x) => F(() => x);

  private static extern int F(Func<int> f);
  private static extern int F(Func<int, Func<int>> f);
}