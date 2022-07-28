using System;

record Base
{
  public Base(Func<int> func) { }
}

record R(int X, int Y) : Base(() => X + Y + 1)
{
  public bool Member = F(() => X + Y + 2);
  public bool OtherMember { get; } = X is var x && F(() => x + X);

  private static bool StaticMember1 = F(() => 3);
  private static bool StaticMember2 = StaticMember1 is var x && F(() => StaticMember1 + x);

  private static bool F(Func<int> f) => true;
}