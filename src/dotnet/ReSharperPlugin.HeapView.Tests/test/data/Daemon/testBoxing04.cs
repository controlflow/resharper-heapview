// ReSharper disable RedundantOverridenMember

struct S {
  public override bool Equals(object obj) {
    return base.Equals(obj);
  }

  public override int GetHashCode() {
    return base.GetHashCode();
  }

  public override string ToString() {
    return base.ToString();
  }
}