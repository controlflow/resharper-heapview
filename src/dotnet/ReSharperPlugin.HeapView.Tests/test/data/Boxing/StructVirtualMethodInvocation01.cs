// ReSharper disable BaseObjectGetHashCodeCallInGetHashCode
// ReSharper disable RedundantOverriddenMember

var s1 = new NoGetHashCodeOverride();
_ = checked((s1).GetHashCode)();

var s2 = new WithGetHashCodeOverride();
_ = s2.GetHashCode();

var rs1 = new RecordStruct1();
_ = rs1.GetHashCode(); // implicit override

var rs2 = new RecordStruct2(42);
_ = rs2.GetHashCode(); // implicit override

struct NoGetHashCodeOverride { }

struct WithGetHashCodeOverride
{
  public override int GetHashCode() => 1;
}

struct WithBaseGetHashCodeInvocation
{
  public override int GetHashCode() => base.GetHashCode();
}

record struct RecordStruct1;
record struct RecordStruct2(int X);