using System;

class C1(int primary)
{
  private Func<int> Field = () => primary; // only delegate alloc, this reference
  public int Capture => primary;
}

class C2(int primary1, int primary2) // display class here, this + primary2
{
  private Func<int> Field = () => primary1 + primary2; // delegate
  public int Capture => primary1;
  public Func<int> Capture2 => () => primary1; // delegate alloc, this reference
}