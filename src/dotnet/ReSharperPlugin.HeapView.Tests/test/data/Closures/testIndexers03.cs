public class Indexers {
  public int this[int x] {
    get {
      var f = () => x;
      return f();
    }
    private set {
      var f = () => x + this[value];
      f();
    }
  }
}