using System;
// ReSharper disable UnusedVariable
#pragma warning disable CS0219

if (args.Length == 0)
{
  var error = 42.ToString; // ambiguous
  var boxingAndDelegate = 42.GetHashCode;
  var nameTake = nameof(int.GetHashCode);
  var explicitFunc = new Func<int>(args[0].GetHashCode);
}
else
{
  var action = LocalFunc;

  throw null!;
  void LocalFunc() { }
}

struct BoxingInside
{
  public void InstanceMethod()
  {
    var f = InstanceMethod;
  }
}