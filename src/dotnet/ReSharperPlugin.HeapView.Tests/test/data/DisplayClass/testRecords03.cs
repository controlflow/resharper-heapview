using System;

record Base
{
  public Base(Func<int> func) { }
}

partial record R(int X, int Y) : Base(() => X + Y + 1)
{
  private static bool F(Func<int> f) => true;
}

partial record R
{
  public bool Member = F(() => X + Y + 2);
  public bool OtherMember { get; } = X is var x && F(() => x + X);
}