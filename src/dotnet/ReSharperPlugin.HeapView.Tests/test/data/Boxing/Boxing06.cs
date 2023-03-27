struct Foo : IFoo
{
  void Method<TFoo>(TFoo tFoo)
  {
    var foo1 = this as IFoo;
    var foo2 = tFoo as IFoo;
  }
}

interface IFoo { }