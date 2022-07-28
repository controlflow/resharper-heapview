using System;

record R(int X, int Y)
{
  public bool Member = F(() => X + Y + 2);
  public bool OtherMember { get; } = X is var x && F(() => x + X);

  private static bool F(Func<int> f) => true;
}