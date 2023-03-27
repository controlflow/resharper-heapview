#nullable disable
using System;

Func<object> func1 = Variance.Static<string>;
Func<object> func2 = Variance.Static<object>;
Func<object> func3 = new(Variance.Static<string>); // alloc
Func<object> func3 = new Variance().Instance<string>; // alloc

public class Variance
{
  public static T Static<T>() => default;
  public T Instance<T>() => default;
}