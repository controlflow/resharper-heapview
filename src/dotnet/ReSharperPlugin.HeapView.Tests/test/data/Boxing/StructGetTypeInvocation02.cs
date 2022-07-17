void Method<TClass, TValue, TUnconstrained>(
  TClass tc, TValue tv, TUnconstrained tu, TValue? tn)
  where TClass : class
  where TValue : struct
{
  tc.GetType();
  tv.GetType();
  tu.GetType(); // possible
  tn.GetType();
}