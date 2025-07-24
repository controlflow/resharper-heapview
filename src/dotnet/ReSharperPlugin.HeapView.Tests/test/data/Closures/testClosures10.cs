using System;

public class C
{
  public void M(Func<int> a) { }

  public void M(out int x)
  {
    for (;; M(out var y), M(() => y++))
    {
      // ..
    }
  }
}