public class Indexers {
  public int this[int x] {
    get => 1;
    set {
      var f = () => value;
      f();
    }
  }
    
  public int Property {
    get => 2;
    set {
      var f = () => value;
      f();
    }
  }
}