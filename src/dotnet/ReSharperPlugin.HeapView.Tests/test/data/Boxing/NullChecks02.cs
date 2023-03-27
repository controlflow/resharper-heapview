// ReSharper disable ReferenceEqualsWithValueType
// ReSharper disable ConditionIsAlwaysTrueOrFalse

struct NullChecks
{
  public void Method<TStruct, TUnconstrained>(TStruct tStruct, TUnconstrained tUnconstrained)
    where TStruct : struct
  {
    _ = object.Equals(this, null); // yes

    // yes in DEBUG
    _ = object.ReferenceEquals(this, null);
    _ = object.ReferenceEquals(tStruct, null);
    _ = object.ReferenceEquals(tStruct, null);
    _ = tUnconstrained == null;
    _ = tUnconstrained != null;
    _ = tUnconstrained is null;
    _ = tUnconstrained is not null;

    TStruct? tn = tStruct;
    _ = tn == null;
    _ = tn != null;
  }
}