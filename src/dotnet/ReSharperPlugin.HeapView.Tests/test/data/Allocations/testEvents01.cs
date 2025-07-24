#nullable enable
using System;
using System.Runtime.InteropServices;

// ReSharper disable ConvertToLocalFunction
// ReSharper disable ValueParameterNotUsed

class Events
{
  public event Action FieldLike = delegate { };

  public event Action Custom
  {
    add { } // it's VERY likely we do some kind of allocations here!
    remove { }
  }

  public void Usage()
  {
    FieldLike();
    FieldLike.Invoke();
    Action? action = delegate { };
    FieldLike += action;
    FieldLike -= action;
    Custom += action;
    Custom -= action;
    _ = action + action;
    _ = action - action;
    action += action;
    action -= action;
    action?.Invoke();
  }
}

class Derived : Events
{
  public void Usage2()
  {
    Action action = delegate { };
    FieldLike += action;
    FieldLike -= action;
    Custom += action;
    Custom -= action;
  }

  public void Usage3()
  {
    FieldLike += delegate { };
    throw null!;
  }
}