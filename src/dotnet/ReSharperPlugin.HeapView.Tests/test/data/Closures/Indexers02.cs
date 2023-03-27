public class Indexers {
  public object this[int x] {
    get => () => x;
    set { } // no capture
  }
}