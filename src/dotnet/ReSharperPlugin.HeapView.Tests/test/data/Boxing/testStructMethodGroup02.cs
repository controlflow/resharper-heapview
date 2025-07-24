void Method<TClass, TValue, TUnconstrained>(
  TClass tc, TValue tv, TUnconstrained tu)
  where TClass : class, I
  where TValue : struct, I
  where TUnconstrained : I
{
  var tcm = tc.Method;
  var tvm = tv.Method;
  var tum = tu.Method;

  var tcg = tc.GetHashCode;
  var tvg = tv.GetHashCode;
  var tug = tu.GetHashCode;
}

interface I
{
  void Method();
}