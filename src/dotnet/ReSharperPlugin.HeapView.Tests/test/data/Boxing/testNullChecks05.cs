// ReSharper disable RedundantCast

class Constrained<T>
{
  public void M(T t)
  {
    _ = (object?) t != null;
  }
}