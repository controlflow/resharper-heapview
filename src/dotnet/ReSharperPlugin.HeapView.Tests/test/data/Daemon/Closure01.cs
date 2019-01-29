using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApplication2
{
  class B
  {
    public int X { get; set; }
    protected int myField = 2;
    protected static readonly int myStatic = 1;
    protected static void S() { }
  }

  class A : B
  {
    public string P { get; set; }

    public void Foo(int bbb)
    {
      const int aaa = 0;
      int cc = 2 + 2;
      Action f = () =>
      {
        S();
        Console.WriteLine(aaa);
        Console.WriteLine(cc);
        Console.WriteLine(bbb + 1);
        Console.WriteLine("sdsd {0}", 123);

        var s = 1 + "sdsds";
        Action g = delegate {
          Console.WriteLine(s);
          Console.WriteLine(s + "!");
        };

        foreach (var x in Enumerable.Range(0, 100))
          Console.WriteLine(x);

        this.Foo(bbb + 2);

        Console.WriteLine(myField);
        Console.WriteLine(myStatic);
        Foo(2);
        g();

        base.X = 1;
        P = s;
      };
    }
  }
}