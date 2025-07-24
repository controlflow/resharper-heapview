var withIndexer = new WithIndexer
{
  [111] = 1, // alloc
  [222, 222] = 2, // alloc
  [333, 333, 333] = 3, // alloc
};

_ = withIndexer[111]; // alloc
_ = withIndexer[222, 222]; // alloc
_ = withIndexer[333, 333, 333]; // alloc

public class WithIndexer
{
  public int this[int x, params int[] xs]
  {
    get => 0;
    set { }
  }

  public int this[params string[] xs]
  {
    get => 0;
    set { }
  }
}