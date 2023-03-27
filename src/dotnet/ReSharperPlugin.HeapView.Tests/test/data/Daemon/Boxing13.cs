using System;
// ReSharper disable ReturnValueOfPureMethodIsNotUsed

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

    // type checks are boxings in .net fw
    if (t is object) { }
    if (t is IComparable) { }
    if (t is IEquatable<T> _) { }
    if (t is ValueType { }) { }
    if (t is int) { }
    if (t is byte b) { }
  }

  public void M2<T>(T t) where T : struct {
    // type checks are boxings in .net fw
    if (t is IComparable) { }
    if (t is IEquatable<T>) { }
    if (t is ValueType _) { } // except me
    if (t is int) { }
    if (t is byte b) { }
  }
}