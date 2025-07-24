// ReSharper disable UseArrayEmptyMethod
// ReSharper disable UnusedVariable

var empty = new int[0];
_ = new Foo(111);
_ = new Foo(111, 222);
_ = new Foo(111, empty);
Foo foo1 = new(111);
Foo foo3 = new(111, 222);
Foo foo4 = new() { Prop = 1 };
var foo5 = new Foo { Prop = 1 };

class Foo
{
  public int Prop { get; set; }

  public Foo(params int[] xs) { }
  public Foo(int x, params int[] xs){ }

  public Foo(bool flag) : this() { } // alloc
  public Foo(long l) : this(1, 2) { } // alloc
}