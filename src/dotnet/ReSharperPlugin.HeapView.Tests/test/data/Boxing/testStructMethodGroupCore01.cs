using System;

var s = new S();

checked(s.Method)();
var naturalType = s.Method;
var naturalFunc = s.Func;
Action action = s.Method;
object obj = s.Method;
Delegate del = s.Method;
var customDelegate = s.Custom;
var toString = s.ToString;
Func<int> ghc = s.GetHashCode;
var equals = s.Equals;
var name = nameof(s.Method);

struct S
{
  public void Method() { }
  public string Func(int x) => x.ToString();
  public int Custom(ref int x) => x;
}