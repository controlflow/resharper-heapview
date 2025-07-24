using System;
// ReSharper disable UnusedVariable

var fromStatic = Static.Method;
var fromLocal = LocalFunc;
Delegate delegateType1 = Static.Method;
Delegate delegateType2 = LocalFunc;
object objDelegate1 = Static.Method;
object objDelegate2 = LocalFunc;
MyAction myAction1 = Static.Method;
MyAction myAction2 = LocalFunc;

void LocalFunc() { }

public class Static
{
  public static void Method() { }
}

public delegate void MyAction();