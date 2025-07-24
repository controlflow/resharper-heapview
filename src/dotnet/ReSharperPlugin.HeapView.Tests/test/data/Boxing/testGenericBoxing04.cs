using System;

abstract class B<U>
{
  protected abstract void M<T>(T t) where T : U;
}

class D : B<int>
{
  protected override void M<T>(T t)
  {
    M(42); // boxing
  
    ValueType v = t;
    IConvertible c = t; // error
  }
}

class E : B<ConsoleKey>
{
  protected override void M<T>(T t)
  {
    M(ConsoleKey.A);

    Enum e = t;
    ValueType v = t;
    IComparable c = t;
    IConvertible cv = t;
    IFormattable f = t;
  }
}