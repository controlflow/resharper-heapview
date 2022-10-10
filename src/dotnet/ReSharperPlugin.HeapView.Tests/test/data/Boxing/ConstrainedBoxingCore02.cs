// ReSharper disable RedundantCast

class Constrained
{
  private void Concrete(SomeStruct some)
  {
    ((IFoo)some).Method();
    _ = ((IFoo)some).Property;
    _ = ((IFoo)some)[42];
  }

  private void Generic(SomeStruct some)
  {
    ((IFoo)some).Method();
    _ = ((IFoo)some).Property;
    _ = ((IFoo)some)[42];
  }

  private struct SomeStruct : IFoo
  {
    public int Method() => 42;
    public int Property => 42;
    public int this[int index] => index;
  }

  private interface IFoo
  {
    int Method();
    int Property { get; }
    int this[int index] { get; }
  }
}