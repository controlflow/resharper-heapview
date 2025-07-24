using System;

static class Extensions
{
  extension (string p)
  {
    public int Count => p.Length;

    public Func<int> Property => () => p.Length;

    public Func<int> Property2
    {
      get => () => p.Length;
    }

    public Func<int> Property3
    {
      get { return () => p.Length; }
    }

    public Func<string> Method1() => () => p;

    public Func<string> Method2() { return () => p; }
  }
}