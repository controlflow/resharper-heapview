using System;

public class ThisCapture {
  public string Text => "aaa";

  public void M() {
    F(() => Text);
    F(() => this.ToString());
    var x = "xxx";
    if (x.Length > 0) {
      var y = x + "yyy";
      F(() => x + y + Text);
      F(() => {
        var z = "zzz";
        string L() => "L";
        return z + L();
      });
      x = Text;
    } 
  }

  public Func<string, string> P => x => x + Text;

  public static string F(Func<string> func) => func();
}