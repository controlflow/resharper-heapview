using System;

public class DisplayClasses {
  public void M(int p) {
    if (p == 42) {
      // shared display class
      var x = "local1";
      var y = "local2";
      F(() => "no capture");
      F(() => "no capture2");
      F(() => x);
      F(() => y);
    }
    
    if (p == 43) {
      var z = "aaa";
      F(() => "no capture3");

      LocalFunc1();
      string LocalFunc1() => z + "a";
        
      Other(); // preserves optimization
      string Other() { z += "a2"; return LocalFunc1(); }
    }
      
    if (p == 44) {
      var w = "bbb";
      // this breaks display class optimization
      F(() => LocalFunc2());
        
      string LocalFunc2() => w + "b";
    }
  }

  public void S() {
    // containing display class
    for (var i = ""; i.Length < 100; i += "a") {
      if (i.Length % 2 == 0) {
        F(() => i + "aa"); // containing
      } else {
        // inner display class
        var j = i.Trim();
        F(() => i + j + "bb"); // inner
        F(() => i + "cc"); // containing
      }
    }
  }

  public void S2() {
    var x = "aaa";
    if (x.Length > 0) {
      var y = "bbb";
      F(() => {
        F(() => x);
        return y;
      });
    }
  }

  public void S3() { // display 0
    var x = "aaa";
    if (x.Length > 0) { // display 1
      var y = "bbb";
      F(() => { // display 2
        var z = "ccc";
        F(() => x + z); // 0+2 = 2
        return y;
      }); // 0+1 = 1
        
      F(() => {
        return F(() => y); // 1
      });
    }
  }

  public static string F(Func<string> func) => func();
}