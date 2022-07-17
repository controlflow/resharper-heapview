void Method<TClass, TValue, TUnconstrained>(
  TClass tc, TValue tv, TUnconstrained tu, TValue? tn)
  where TClass : class
  where TValue : struct
{
  tc.GetHashCode();
  tv.GetHashCode();
  tu.GetHashCode();
  tn.GetHashCode();

  tc.Equals(null);
  tv.Equals(null);
  tu.Equals(null);
  tn.Equals(null);

  tc.ToString();
  tv.ToString();
  tu.ToString();
  tn.ToString();
}