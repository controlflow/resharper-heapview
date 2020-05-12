using System;

public class SomeClass : BaseClass {
  public SomeClass(string cp)
    : base(() => cp + "ctor initializer")
    => F(() => cp + "ctor body");

  public string Method(string mp) {
    F(() => mp + "method");
    void Local(string lp) => F(() => mp + lp + "local func");
    string OtherLocal() {
      return nameof(mp) + nameof(mp.Length);
    }
    return mp + nameof(mp);
  }

  public string Property {
    get => "property.get";
    set { F(() => value + "property.set"); }
  }

  public string this[string ip] => F(() => ip + "indexer");
  public string this[string ip2, int x] {
    get { return F(() => ip2 + "indexer2.get"); }
    set => F(() => ip2 + value + "indexer2.get");
  }

  public static implicit operator string(SomeClass op)
    => F(() => op + "operator");

  public static string F(Func<string> func) => func();
}

public class BaseClass {
  public BaseClass(Func<string> func) { }
}