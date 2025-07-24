// ReSharper disable UnusedVariable

class ArrayInitializers
{
  public int[] Array01 = { 1, 2, 3 }; // yes
  public int[] Array02 = { }, // yes
               Array03 = { 0, 0, 0 }; // yes
  public int[] Array04 { get; } = { }; // yes
  public int[,] Array05 = { { 1, 2 }, { 3, 4 } }; // yes, outer
  public Unresolved Array06 = { };
  public Unresolved[] Array07 = { }; // yes
  public string[] Array08 =
  {
    "only",
    "brace",
    "highlighted"
  };

  public void Method(int x)
  {
    if (x == 42)
    {
      string[] xs = { "aaa", "bbb" };
      throw new System.Exception();
    }

    string[] ys = { "aaa", "bbb" }; // yes
  }
}