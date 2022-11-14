public class Indexers {
  public object this[int x] => () => x;
  public object this[long x] { get => () => x; }
  public object this[ulong x] { get { return () => x; } }
}