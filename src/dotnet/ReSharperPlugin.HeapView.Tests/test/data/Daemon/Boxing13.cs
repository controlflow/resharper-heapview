class C {
  public void M1<T>(T t) {
    // optimized in fw/core
    if (t != null) { }
    if ((object) t != null) { }
    if (null == (t)) { }
    if (t is null) { }
    if ((object) t is null) { }
    if (t is { }) { }
    if (ReferenceEquals(t, null)) { }
    t?.ToString(); // optimized ?, possible callvirt

    if (t is int) { }
    if (t is IEquatable<T>) { }
  }

  public void M2<T>(T t) where T : struct {
    if (t is int) { }
    if (t is IEquatable<T>) { }
  }
}