using System;

abstract class B<U>
{
  protected abstract void M<T>(T t) where T : U;
}

class D : B<ValueType>
{
  protected override void M<T>(T t)
  {
    M<Enum>(null);
    M(42); // boxing
  
    ValueType v = t;
  }
}

class E : B<Enum>
{
  protected override void M<T>(T t)
  {
    M<Enum>(null);
    M(ConsoleKey.A); // boxing

    Enum e = t;
  }
}