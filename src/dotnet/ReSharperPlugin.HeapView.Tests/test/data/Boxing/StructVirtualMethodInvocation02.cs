struct NoGetHashCodeOverride
{
  public int Method1() => GetHashCode();
  public bool Method2() => checked(this).Equals(null);
}

struct WithGetHashCodeOverride
{
  public int Method1() => GetHashCode();
  public bool Method2() => this.Equals(null);
  public override int GetHashCode() => 1;
  public override bool Equals(object obj) => false;
}

record struct RecordStruct1
{
  public int Method1() => GetHashCode();
  public bool Method2() => this.Equals(null);
}

record struct RecordStruct2
{
  public int Method() => base.GetHashCode();
}