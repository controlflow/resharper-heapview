struct Enumerable : System.IDisposable {
  public Enumerable GetEnumerator() => this;
  public bool MoveNext() => true;
  public (int, (byte, int)) Current => (1, (2, 3));
  public void Dispose() { }
  
  public void M() {
    foreach (object o in this) { } // boxing
    foreach (var t in this) { }
    foreach ((var a, var b) in this) { }
    foreach ((object a, object b) in this) { } // boxing x2
    foreach (var (a, b) in this) { }
    foreach ((var a, (object b, var c)) in this) { } // boxing
    foreach (object o in new[] { 1, 2, 3 }) { } // boxing
  }
}