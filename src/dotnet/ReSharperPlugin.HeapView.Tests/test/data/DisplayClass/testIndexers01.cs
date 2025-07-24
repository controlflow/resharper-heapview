using System;

class C
{
  public Func<int> this[int p]
  {
    get => () => p;
    set { var f = () => p; }
  }
}