using System.Collections;

class Foo : IEnumerable
{
  public Foo(params string[] xs) { }

  public IEnumerator GetEnumerator() { yield break; }
}

delegate void D(params object[] xs);

class Bar : Foo
{
  public Bar() : base() { }
  public Bar(byte x) : base(null) { }
  public Bar(sbyte x) : base(null, null) { }

  public int this[params string[] xs] => 42;
  public int Add(params string[] xs) => 42;

  D Method(params object[] xs)
  {
    Method();
    Method(null);
    Method(null, null);

    Method(null)();
    Method(null)(null);
    Method(null)(null, null);

    new Foo();
    new Foo(null);
    new Foo(null, null);

    var i1 = this[null];
    var i2 = this[null, null];
    var i3 = this[null, null, null];

    new Bar { null };
    new Bar { null, null };
    new Bar { {null, null} };
    new Bar { {null, null, null} };

    return null;
  }
}