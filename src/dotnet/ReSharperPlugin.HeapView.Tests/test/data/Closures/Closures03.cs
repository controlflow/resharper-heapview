using System;

public class SomeClass {
  public void Method() {
    var local = "local";
    const string constant = "constant";
    F(() => {
      var local2 = "local2";
      F(() => local2 + local);
      return local + constant;
    });
  }

  public static string F(Func<string> func) => func();
}