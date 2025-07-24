void TypeParameters1<T, TBase>(T t) where T : TBase
{
  TBase tb = t; // unlikely
}

void TypeParameters2<T, TBase>(T t) where T : struct, TBase
{
  TBase tb = t; // possible
}

abstract class B<U>
{
  protected abstract void M<T, TBase>(T t) where T : U, TBase;
}

class D1 : B<int>
{
  protected override void M<T, TBase>(T t)
  {
    // T is effectively value type
    TBase tb = t; // possible
  }
}

class D2 : B<string>
{
  protected override void M<T, TBase>(T t)
  {
    TBase tb = t;
  }
}