NoGetHashCodeOverride? s1 = new NoGetHashCodeOverride();
_ = s1.GetHashCode();
_ = s1.Equals(null);

WithGetHashCodeOverride? s2 = new WithGetHashCodeOverride();
_ = s2.GetHashCode();
_ = s2.Equals(null);

RecordStruct1? rs1 = new RecordStruct1();
_ = rs1.GetHashCode();
_ = rs1.Equals(null);

RecordStruct2? rs2 = new RecordStruct2(42);
_ = rs2.GetHashCode();
_ = rs2.Equals(null);

struct NoGetHashCodeOverride { }

struct WithGetHashCodeOverride
{
  public override int GetHashCode() => 1;
  public override bool Equals(object o) => false;
}

record struct RecordStruct1;
record struct RecordStruct2(int X);